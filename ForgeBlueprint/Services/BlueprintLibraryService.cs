using System.Collections.Generic;
using ForgeBlueprint.Models;

namespace ForgeBlueprint.Services
{
    public sealed class BlueprintLibraryService
    {
        public List<BlueprintDefinition> GetBlueprints()
        {
            return new List<BlueprintDefinition>
            {
                new BlueprintDefinition
                {
                    Key = "footsteps",
                    Name = "Footsteps Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Traversal",
                    Summary = "Creates a strong FMOD footsteps shell built around one master event, a Surfaces parameter and dedicated logic tracks for the core terrain set.",
                    ImplementationGoal = "Set up a reusable surface-driven footsteps implementation with one master event, a discrete Surfaces parameter and optional gear / cloth layering.",
                    PreviewLead = "This blueprint now mirrors a more production-ready FMOD setup: one Footsteps event, one Surfaces parameter and one logic track per surface.",
                    HasConfigurableOptions = true
                },
                new BlueprintDefinition
                {
                    Key = "ui2d",
                    Name = "UI 2D Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Interface",
                    Summary = "Creates a 2D UI event shell inside one UI folder, with toggles for the core navigation events and support for additional custom events.",
                    ImplementationGoal = "Give menus and HUD feedback a fast FMOD starting point by exporting a ready-made UI folder with the selected 2D events.",
                    PreviewLead = "This blueprint is focused on speed: select the standard UI events you need, add any extra ones, and export a clean FMOD UI template.",
                    HasConfigurableOptions = true
                },
                new BlueprintDefinition
                {
                    Key = "weaponbasic",
                    Name = "Weapon Basic Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Combat",
                    Summary = "Sets up a compact weapon implementation structure for swings, impacts and optional sweeteners.",
                    ImplementationGoal = "Avoid rebuilding the same combat skeleton each time a new project starts.",
                    PreviewLead = "This is a strong second-step blueprint after footsteps.",
                    StaticOptions = new List<string>
                    {
                        "2D or 3D choice depending on game camera",
                        "Swing and impact families",
                        "Optional cloth / body / armor splits",
                        "Optional layered sweetener track"
                    },
                    StaticGeneratedItems = new List<string>
                    {
                        "Weapon master category",
                        "Swing event shell",
                        "Impact family structure",
                        "Variation-ready folders"
                    },
                    StaticNotes = new List<string>
                    {
                        "Very useful for action games.",
                        "Can later expand into melee / ranged / explosive presets."
                    }
                },
                new BlueprintDefinition
                {
                    Key = "3doneshot",
                    Name = "3D One-Shot Starter",
                    BlueprintType = "Event",
                    Middleware = "FMOD",
                    Category = "Generic",
                    Summary = "Creates a generic 3D one-shot event template for fast implementation of world-based sounds.",
                    ImplementationGoal = "Speed up repetitive authoring when a project needs many simple spatialized sounds.",
                    PreviewLead = "This is one of the smallest useful building blocks and later can become a macro generator.",
                    StaticOptions = new List<string>
                    {
                        "3D spatialization base",
                        "Optional max distance preset",
                        "Optional random multi-variation shell"
                    },
                    StaticGeneratedItems = new List<string>
                    {
                        "Single event shell",
                        "3D ready routing",
                        "Distance / variation placeholders"
                    },
                    StaticNotes = new List<string>
                    {
                        "Good micro-blueprint.",
                        "Useful inside larger project templates."
                    }
                },
                new BlueprintDefinition
                {
                    Key = "metroidvania",
                    Name = "Metroidvania Audio Starter",
                    BlueprintType = "Project",
                    Middleware = "FMOD",
                    Category = "Genre Template",
                    Summary = "A higher-level project scaffold for character traversal, ambience, enemies, UI and adaptive exploration cues.",
                    ImplementationGoal = "Kick off a full middleware architecture tailored to a side-scrolling exploration game.",
                    PreviewLead = "Project blueprints come after system blueprints, but this preview helps define the long-term vision.",
                    StaticOptions = new List<string>
                    {
                        "Traversal and movement categories",
                        "Enemy and boss separation",
                        "Exploration ambience shell",
                        "UI and map feedback layer",
                        "Music ready grouping"
                    },
                    StaticGeneratedItems = new List<string>
                    {
                        "Top-level category layout",
                        "Bus and routing starter",
                        "System blueprint insertion points"
                    },
                    StaticNotes = new List<string>
                    {
                        "Not for the first implementation pass.",
                        "Useful later when the core generator is stable."
                    }
                },
                new BlueprintDefinition
                {
                    Key = "towerdefense",
                    Name = "Tower Defense Audio Starter",
                    BlueprintType = "Project",
                    Middleware = "FMOD",
                    Category = "Genre Template",
                    Summary = "A project scaffold focused on waves, tower actions, enemy reads, notifications and top-down clarity.",
                    ImplementationGoal = "Standardize the first middleware pass for strategy-oriented games with repeated gameplay loops.",
                    PreviewLead = "Another future-facing project blueprint once the system generators are proven.",
                    StaticOptions = new List<string>
                    {
                        "Towers, enemies and UI categories",
                        "Wave progression feedback shell",
                        "Notification and alert structure",
                        "Music escalation entry points"
                    },
                    StaticGeneratedItems = new List<string>
                    {
                        "Genre-based top-level layout",
                        "Gameplay feedback families",
                        "System blueprint docking points"
                    },
                    StaticNotes = new List<string>
                    {
                        "Good long-term template target.",
                        "Not needed for the first coding milestone."
                    }
                }
            };
        }
    }
}
