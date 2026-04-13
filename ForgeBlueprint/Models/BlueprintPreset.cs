using System;

namespace ForgeBlueprint.Models
{
    public sealed class BlueprintPreset
    {
        public string PresetName { get; set; } = "";
        public string BlueprintKey { get; set; } = "";
        public string BlueprintName { get; set; } = "";
        public string BlueprintType { get; set; } = "";
        public string Middleware { get; set; } = "";
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
        public FootstepsBlueprintOptions? FootstepsOptions { get; set; }
    }
}
