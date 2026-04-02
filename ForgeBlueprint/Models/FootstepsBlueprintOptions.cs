using System.Collections.Generic;

namespace ForgeBlueprint.Models
{
    public sealed class FootstepsBlueprintOptions
    {
        public string SpatialMode { get; set; } = "3D";
        public string EventStructure { get; set; } = "Single Master Event";
        public string NamingPrefix { get; set; } = "char";

        public bool IncludeWater { get; set; } = true;
        public bool IncludeSprint { get; set; } = true;
        public bool IncludeLanding { get; set; } = true;
        public bool IncludeGear { get; set; } = false;

        public List<string> GetSurfaceNames()
        {
            List<string> surfaces = new()
            {
                "Stone",
                "Dirt",
                "Grass",
                "Wood"
            };

            if (IncludeWater)
            {
                surfaces.Add("Water");
            }

            return surfaces;
        }
    }
}
