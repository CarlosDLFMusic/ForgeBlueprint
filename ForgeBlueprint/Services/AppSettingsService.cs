using ForgeBlueprint.Models;
using System;
using System.IO;
using System.Text.Json;

namespace ForgeBlueprint.Services
{
    public sealed class AppSettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public string SettingsFolderPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ForgeVault");

        public string SettingsFilePath =>
            Path.Combine(SettingsFolderPath, "settings.json");

        public AppSettings Load()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolderPath);

                if (!File.Exists(SettingsFilePath))
                {
                    AppSettings defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
                }

                string json = File.ReadAllText(SettingsFilePath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (settings == null)
                {
                    AppSettings defaults = CreateDefaultSettings();
                    Save(defaults);
                    return defaults;
                }

                Normalize(settings);
                return settings;
            }
            catch
            {
                return CreateDefaultSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Normalize(settings);
            Directory.CreateDirectory(SettingsFolderPath);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }

        private static AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                DefaultProjectsFolder = "",
                DefaultExportFolder = "",

                IncludePackageJsonByDefault = true,
                IncludeAssetsCsvByDefault = true,
                IncludeExportReportByDefault = true,

                PreserveProjectFoldersByDefault = true,
                SanitizeNamesByDefault = true,
                SkipMissingFilesByDefault = true,
                ResolveDuplicateNamesByDefault = true,

                Theme = "Dark"
            };
        }

        private static void Normalize(AppSettings settings)
        {
            settings.DefaultProjectsFolder ??= "";
            settings.DefaultExportFolder ??= "";

            if (string.IsNullOrWhiteSpace(settings.Theme))
                settings.Theme = "Dark";

            if (!string.Equals(settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(settings.Theme, "Light", StringComparison.OrdinalIgnoreCase))
            {
                settings.Theme = "Dark";
            }
        }
    }
}
