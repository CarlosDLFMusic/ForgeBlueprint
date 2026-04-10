using ForgeBlueprint.Models;
using System;
using System.IO;
using System.Text.Json;

namespace ForgeBlueprint.Services
{
    public sealed class RecipeExportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public void ExportToJson(GenerationRecipe recipe, string filePath)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Export path cannot be empty.", nameof(filePath));

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(recipe, JsonOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
