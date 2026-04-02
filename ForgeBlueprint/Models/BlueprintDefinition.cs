using System.Collections.Generic;

namespace ForgeBlueprint.Models
{
    public sealed class BlueprintDefinition
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string BlueprintType { get; set; } = "";
        public string Middleware { get; set; } = "";
        public string Category { get; set; } = "";
        public string Summary { get; set; } = "";
        public string ImplementationGoal { get; set; } = "";
        public string PreviewLead { get; set; } = "";

        public bool HasConfigurableOptions { get; set; }

        public List<string> StaticOptions { get; set; } = new();
        public List<string> StaticGeneratedItems { get; set; } = new();
        public List<string> StaticNotes { get; set; } = new();
    }
}
