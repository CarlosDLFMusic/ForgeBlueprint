using System.Collections.Generic;

namespace ForgeBlueprint.Models
{
    public sealed class GeneratedParameter
    {
        public string Name { get; set; } = "";
        public string Scope { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Values { get; set; } = new();
    }
}
