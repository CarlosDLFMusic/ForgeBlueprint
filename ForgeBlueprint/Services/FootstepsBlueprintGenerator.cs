using ForgeBlueprint.Models;
using System;
using System.Collections.Generic;

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
                Folders = BuildFolders(prefix, surfaceNames),
                Events = BuildEvents(prefix, options),
                Buses = BuildBuses(),
                Parameters = BuildParameters(surfaceNames),
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
                $"Naming prefix: {options.NamingPrefix}",
                "Event model: single Footsteps master event",
                "Surface parameter: Surfaces",
                $"Surfaces: {string.Join(", ", surfaceNames)}",
                options.IncludeGear ? "Gear / cloth companion track: included" : "Gear / cloth companion track: disabled"
            };
        }

        private static List<GeneratedFolder> BuildFolders(string prefix, List<string> surfaceNames)
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

            return folders;
        }

        private static List<GeneratedEvent> BuildEvents(string prefix, FootstepsBlueprintOptions options)
        {
            string purpose = options.IncludeGear
                ? "Single footsteps event driven by the Surfaces parameter, with an additional gear / cloth companion logic track."
                : "Single footsteps event driven by the Surfaces parameter, with one dedicated logic track per surface.";

            return new List<GeneratedEvent>
            {
                new GeneratedEvent
                {
                    Path = $"event:/characters/{prefix}/footsteps/ev_{prefix}_footsteps",
                    SpatialMode = options.SpatialMode,
                    Purpose = purpose,
                    TriggerSuggestion = "Drive with animation notifies or gameplay movement callbacks."
                }
            };
        }

        private static List<GeneratedBus> BuildBuses()
        {
            return new List<GeneratedBus>
            {
                new GeneratedBus
                {
                    Path = "bus:/SFX/Characters/Footsteps",
                    Purpose = "Main footsteps routing bus."
                }
            };
        }

        private static List<GeneratedParameter> BuildParameters(List<string> surfaceNames)
        {
            return new List<GeneratedParameter>
            {
                new GeneratedParameter
                {
                    Name = "Surfaces",
                    Scope = "Event",
                    Description = "Selects the active terrain logic track inside the master footsteps event.",
                    Values = new List<string>(surfaceNames)
                }
            };
        }

        private static List<string> BuildRoutingNotes(FootstepsBlueprintOptions options)
        {
            List<string> notes = new()
            {
                "Route the footsteps event to bus:/SFX/Characters/Footsteps.",
                "Each surface should live on its own logic track and respond to the discrete Surfaces parameter."
            };

            if (options.IncludeGear)
            {
                notes.Add("Add a companion gear / cloth logic track inside the same event so it can layer with every surface.");
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
            List<string> warnings = new()
            {
                "Sprint and landing are intentionally left out of this blueprint and should be authored as separate events."
            };

            if (options.IncludeGear && string.Equals(options.SpatialMode, "2D", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add("2D + gear / cloth layering may need extra mix decisions to avoid clutter.");
            }

            return warnings;
        }

        private static List<string> BuildNextSteps(string prefix, FootstepsBlueprintOptions options)
        {
            List<string> steps = new()
            {
                $"Create the FMOD event shell using the naming prefix '{prefix}'.",
                "Add a discrete Surfaces parameter with values for Concrete, Dirt, Grass, Rock, Wood, Snow, Sand, Mud and Water.",
                "Create one logic track per surface and place the corresponding multi instrument and random variations on each track.",
                "Connect the gameplay side through animation notifies, traces or movement callbacks.",
                "Validate the routing and mix level against the rest of the character SFX."
            };

            if (options.IncludeGear)
            {
                steps.Add("Add a dedicated gear / cloth companion track that can layer on top of any surface trigger.");
            }

            return steps;
        }
    }
}
