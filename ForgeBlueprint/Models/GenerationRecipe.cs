using System;
using System.Collections.Generic;

namespace ForgeBlueprint.Models
{
    public sealed class GenerationRecipe
    {
        public string BlueprintKey { get; set; } = "";
        public string BlueprintName { get; set; } = "";
        public string BlueprintType { get; set; } = "";
        public string Middleware { get; set; } = "";
        public string Category { get; set; } = "";

        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public string NamingPrefix { get; set; } = "";

        public List<string> OptionSummary { get; set; } = new();
        public List<GeneratedFolder> Folders { get; set; } = new();
        public List<GeneratedEvent> Events { get; set; } = new();
        public List<GeneratedBus> Buses { get; set; } = new();
        public List<GeneratedParameter> Parameters { get; set; } = new();
        public List<string> RoutingNotes { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> NextSteps { get; set; } = new();
    }
}
