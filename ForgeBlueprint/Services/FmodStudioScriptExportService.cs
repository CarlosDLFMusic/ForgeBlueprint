using ForgeBlueprint.Models;
using System;
using System.IO;

namespace ForgeBlueprint.Services
{
    public sealed class FmodStudioScriptExportService
    {
        private readonly FootstepsFmodStudioScriptBuilder _footstepsBuilder = new();
        private readonly Ui2dFmodStudioScriptBuilder _ui2dBuilder = new();

        public void ExportScript(
            BlueprintDefinition blueprint,
            FootstepsBlueprintOptions footstepsOptions,
            Ui2dBlueprintOptions ui2dOptions,
            string filePath)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));
            if (footstepsOptions == null)
                throw new ArgumentNullException(nameof(footstepsOptions));
            if (ui2dOptions == null)
                throw new ArgumentNullException(nameof(ui2dOptions));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Export path cannot be empty.", nameof(filePath));

            string script = BuildScript(blueprint, footstepsOptions, ui2dOptions);

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, script);
        }

        private string BuildScript(BlueprintDefinition blueprint, FootstepsBlueprintOptions footstepsOptions, Ui2dBlueprintOptions ui2dOptions)
        {
            if (string.Equals(blueprint.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                return _footstepsBuilder.Build(blueprint, footstepsOptions);
            }

            if (string.Equals(blueprint.Key, "ui2d", StringComparison.OrdinalIgnoreCase))
            {
                return _ui2dBuilder.Build(blueprint, ui2dOptions);
            }

            throw new NotSupportedException($"The blueprint '{blueprint.Name}' does not support FMOD Studio script export yet.");
        }
    }
}
