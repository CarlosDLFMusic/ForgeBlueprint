using ForgeBlueprint.Models;
using System;
using System.IO;

namespace ForgeBlueprint.Services
{
    public sealed class FmodStudioScriptExportService
    {
        private readonly FootstepsFmodStudioScriptBuilder _footstepsBuilder = new();

        public void ExportScript(BlueprintDefinition blueprint, FootstepsBlueprintOptions options, string filePath)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Export path cannot be empty.", nameof(filePath));

            string script = BuildScript(blueprint, options);

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, script);
        }

        private string BuildScript(BlueprintDefinition blueprint, FootstepsBlueprintOptions options)
        {
            if (string.Equals(blueprint.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                return _footstepsBuilder.Build(blueprint, options);
            }

            throw new NotSupportedException($"The blueprint '{blueprint.Name}' does not support FMOD Studio script export yet.");
        }
    }
}
