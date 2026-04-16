using ForgeBlueprint.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForgeBlueprint.Services
{
    public sealed class PresetService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly AppSettingsService _appSettingsService = new();

        public string PresetsRootFolderPath =>
            Path.Combine(_appSettingsService.SettingsFolderPath, "Presets");

        public string GetBlueprintPresetsFolderPath(string blueprintKey)
        {
            string safeBlueprintKey = SanitizeFileToken(blueprintKey, "blueprint");
            return Path.Combine(PresetsRootFolderPath, safeBlueprintKey);
        }

        public BlueprintPreset CreateFootstepsPreset(BlueprintDefinition blueprint, FootstepsBlueprintOptions options, string presetName)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new BlueprintPreset
            {
                PresetName = string.IsNullOrWhiteSpace(presetName) ? "footsteps_preset" : presetName.Trim(),
                BlueprintKey = blueprint.Key,
                BlueprintName = blueprint.Name,
                BlueprintType = blueprint.BlueprintType,
                Middleware = blueprint.Middleware,
                SavedAtUtc = DateTime.UtcNow,
                FootstepsOptions = CloneFootstepsOptions(options)
            };
        }

        public void SavePreset(BlueprintPreset preset, string filePath)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Preset path cannot be empty.", nameof(filePath));

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(preset, JsonOptions);
            File.WriteAllText(filePath, json);
        }

        public BlueprintPreset LoadPreset(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Preset path cannot be empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Preset file was not found.", filePath);

            string json = File.ReadAllText(filePath);
            BlueprintPreset? preset = JsonSerializer.Deserialize<BlueprintPreset>(json, JsonOptions);

            if (preset == null)
                throw new InvalidOperationException("Preset file could not be read.");

            return preset;
        }

        public string BuildSuggestedPresetFileName(string blueprintKey, string presetName)
        {
            string safeBlueprintKey = SanitizeFileToken(blueprintKey, "blueprint");
            string safePresetName = SanitizeFileToken(presetName, "preset");
            return $"{safeBlueprintKey}_{safePresetName}_preset.json";
        }

        private static FootstepsBlueprintOptions CloneFootstepsOptions(FootstepsBlueprintOptions options)
        {
            return new FootstepsBlueprintOptions
            {
                SpatialMode = options.SpatialMode,
                NamingPrefix = options.NamingPrefix,
                IncludeGear = options.IncludeGear
            };
        }

        private static string SanitizeFileToken(string? value, string fallback)
        {
            string text = (value ?? string.Empty).Trim().ToLowerInvariant();
            text = Regex.Replace(text, @"[^a-z0-9_\-]+", "_");
            text = Regex.Replace(text, @"_+", "_").Trim('_');

            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }
    }
}
