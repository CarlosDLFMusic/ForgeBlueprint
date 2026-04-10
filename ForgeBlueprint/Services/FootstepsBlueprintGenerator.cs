using ForgeBlueprint.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeBlueprint.Services
{
    public sealed class FootstepsBlueprintGenerator
    {
        public GenerationRecipe Generate(BlueprintDefinition blueprint, FootstepsBlueprintOptions options)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            List<string> surfaceNames = options.GetSurfaceNames();
            string prefix = options.NamingPrefix;

            return new GenerationRecipe
            {
                BlueprintKey = blueprint.Key,
                BlueprintName = blueprint.Name,
                BlueprintType = blueprint.BlueprintType,
                Middleware = blueprint.Middleware,
                Category = blueprint.Category,
                NamingPrefix = prefix,

                OptionSummary = BuildOptionSummary(options, surfaceNames),
                Folders = BuildFolders(prefix, surfaceNames, options),
                Events = BuildEvents(prefix, options),
                Buses = BuildBuses(options),
                Parameters = BuildParameters(options, surfaceNames),
                RoutingNotes = BuildRoutingNotes(options),
                Warnings = BuildWarnings(options),
                NextSteps = BuildNextSteps(prefix, options)
            };
        }

        private static List<string> BuildOptionSummary(FootstepsBlueprintOptions options, List<string> surfaceNames)
        {
            return new List<string>
            {
                $"Spatial mode: {options.SpatialMode}",
                $"Event structure: {options.EventStructure}",
                $"Naming prefix: {options.NamingPrefix}",
                $"Surfaces: {string.Join(", ", surfaceNames)}",
                options.IncludeWater ? "Water surface: included" : "Water surface: disabled",
                options.IncludeSprint ? "Sprint layer: included" : "Sprint layer: disabled",
                options.IncludeLanding ? "Landing layer: included" : "Landing layer: disabled",
                options.IncludeGear ? "Gear / cloth companion: included" : "Gear / cloth companion: disabled"
            };
        }

        private static List<GeneratedFolder> BuildFolders(
            string prefix,
            List<string> surfaceNames,
            FootstepsBlueprintOptions options)
        {
            List<GeneratedFolder> folders = new()
            {
                new GeneratedFolder
                {
                    Path = $"Events/Characters/{prefix}/Footsteps",
                    Purpose = "Main FMOD event container for the footsteps system."
                },
                new GeneratedFolder
                {
                    Path = $"Assets/Footsteps/{prefix}",
                    Purpose = "Root asset folder for this character or movement family."
                },
                new GeneratedFolder
                {
                    Path = $"Assets/Footsteps/{prefix}/Shared",
                    Purpose = "Shared source layers, tails and common supporting assets."
                }
            };

            foreach (string surface in surfaceNames)
            {
                folders.Add(new GeneratedFolder
                {
                    Path = $"Assets/Footsteps/{prefix}/{surface}",
                    Purpose = $"{surface} surface assets and variations."
                });
            }

            if (options.IncludeGear)
            {
                folders.Add(new GeneratedFolder
                {
                    Path = $"Events/Characters/{prefix}/Gear",
                    Purpose = "Companion gear or cloth movement events."
                });
            }

            return folders;
        }

        private static List<GeneratedEvent> BuildEvents(string prefix, FootstepsBlueprintOptions options)
        {
            List<GeneratedEvent> events = new();

            if (string.Equals(options.EventStructure, "Single Master Event", StringComparison.OrdinalIgnoreCase))
            {
                events.Add(new GeneratedEvent
                {
                    Path = $"event:/characters/{prefix}/footsteps/ev_{prefix}_footsteps_master",
                    SpatialMode = options.SpatialMode,
                    Purpose = "Single master footsteps event that switches behavior by parameters.",
                    TriggerSuggestion = "Drive with animation notifies or gameplay movement callbacks."
                });
            }
            else
            {
                events.Add(new GeneratedEvent
                {
                    Path = $"event:/characters/{prefix}/footsteps/ev_{prefix}_footsteps_walk",
                    SpatialMode = options.SpatialMode,
                    Purpose = "Walk footsteps event.",
                    TriggerSuggestion = "Call from standard locomotion footsteps."
                });

                if (options.IncludeSprint)
                {
                    events.Add(new GeneratedEvent
                    {
                        Path = $"event:/characters/{prefix}/footsteps/ev_{prefix}_footsteps_sprint",
                        SpatialMode = options.SpatialMode,
                        Purpose = "Sprint footsteps event.",
                        TriggerSuggestion = "Call from sprint movement state."
                    });
                }

                if (options.IncludeLanding)
                {
                    events.Add(new GeneratedEvent
                    {
                        Path = $"event:/characters/{prefix}/footsteps/ev_{prefix}_footsteps_landing",
                        SpatialMode = options.SpatialMode,
                        Purpose = "Landing event for heavier foot contacts.",
                        TriggerSuggestion = "Call on jump landing or drop impact."
                    });
                }
            }

            if (options.IncludeGear)
            {
                events.Add(new GeneratedEvent
                {
                    Path = $"event:/characters/{prefix}/gear/ev_{prefix}_gear_movement",
                    SpatialMode = options.SpatialMode,
                    Purpose = "Companion gear or cloth movement layer.",
                    TriggerSuggestion = "Trigger alongside footsteps or from movement state changes."
                });
            }

            return events;
        }

        private static List<GeneratedBus> BuildBuses(FootstepsBlueprintOptions options)
        {
            List<GeneratedBus> buses = new()
            {
                new GeneratedBus
                {
                    Path = "bus:/SFX/Characters/Footsteps",
                    Purpose = "Main footsteps routing bus."
                }
            };

            if (options.IncludeGear)
            {
                buses.Add(new GeneratedBus
                {
                    Path = "bus:/SFX/Characters/Gear",
                    Purpose = "Routing bus for cloth, armor or gear movement layers."
                });
            }

            return buses;
        }

        private static List<GeneratedParameter> BuildParameters(
            FootstepsBlueprintOptions options,
            List<string> surfaceNames)
        {
            List<GeneratedParameter> parameters = new()
            {
                new GeneratedParameter
                {
                    Name = "surface",
                    Scope = "Event",
                    Description = "Selects the active material or terrain family.",
                    Values = surfaceNames.Select(x => x.ToLowerInvariant()).ToList()
                }
            };

            if (string.Equals(options.EventStructure, "Single Master Event", StringComparison.OrdinalIgnoreCase))
            {
                List<string> movementValues = new() { "walk" };

                if (options.IncludeSprint)
                    movementValues.Add("sprint");

                if (options.IncludeLanding)
                    movementValues.Add("landing");

                if (movementValues.Count > 1)
                {
                    parameters.Add(new GeneratedParameter
                    {
                        Name = "movement_layer",
                        Scope = "Event",
                        Description = "Chooses the movement family inside the master event.",
                        Values = movementValues
                    });
                }
            }

            return parameters;
        }

        private static List<string> BuildRoutingNotes(FootstepsBlueprintOptions options)
        {
            List<string> notes = new()
            {
                "Route the main footsteps system to bus:/SFX/Characters/Footsteps."
            };

            if (options.IncludeGear)
            {
                notes.Add("Route the gear companion layer to bus:/SFX/Characters/Gear.");
            }

            if (string.Equals(options.SpatialMode, "3D", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("Use 3D spatialization and distance attenuation appropriate for character movement.");
            }
            else
            {
                notes.Add("Use 2D routing when the project does not need world-space spatialization.");
            }

            return notes;
        }

        private static List<string> BuildWarnings(FootstepsBlueprintOptions options)
        {
            List<string> warnings = new();

            if (string.Equals(options.EventStructure, "Single Master Event", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("Single master event requires careful parameter routing for surface and movement states.");
            }
            else
            {
                warnings.Add("Split event mode requires separate gameplay or animation routing for each event family.");
            }

            if (!options.IncludeWater)
            {
                warnings.Add("Water is disabled, so wet traversal cases will need to be added later if the project needs them.");
            }

            if (options.IncludeGear && string.Equals(options.SpatialMode, "2D", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("2D + gear companion may need extra mix decisions to avoid clutter.");
            }

            return warnings;
        }

        private static List<string> BuildNextSteps(string prefix, FootstepsBlueprintOptions options)
        {
            List<string> steps = new()
            {
                $"Create the FMOD event shell using the naming prefix '{prefix}'.",
                "Add random or multi-instrument variation structure per surface.",
                "Connect the gameplay side through animation notifies, traces or movement callbacks.",
                "Validate the routing and mix level against the rest of the character SFX."
            };

            if (string.Equals(options.EventStructure, "Single Master Event", StringComparison.OrdinalIgnoreCase))
            {
                steps.Add("Implement parameter-driven logic for surface and movement_layer.");
            }
            else
            {
                steps.Add("Wire walk, sprint and landing as separate gameplay triggers.");
            }

            return steps;
        }
    }
}
