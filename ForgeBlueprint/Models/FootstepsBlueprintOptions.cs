using System.Collections.Generic;

namespace ForgeBlueprint.Models
{
    public sealed class FootstepsBlueprintOptions
    {
        public string SpatialMode { get; set; } = "3D";
        public string NamingPrefix { get; set; } = "char";
        public bool IncludeGear { get; set; } = false;

        public List<string> GetSurfaceNames()
        {
            return new List<string>
            {
                "Concrete",
                "Dirt",
                "Grass",
                "Rock",
                "Wood",
                "Snow",
                "Sand",
                "Mud",
                "Water"
            };
        }
    }
}
