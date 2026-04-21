using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgeBlueprint.Models
{
    public sealed class Ui2dBlueprintOptions
    {
        public bool IncludeHover { get; set; } = true;
        public bool IncludePress { get; set; } = true;
        public bool IncludeBack { get; set; } = true;
        public bool IncludeCancel { get; set; } = true;
        public bool IncludeConfirm { get; set; } = true;
        public bool IncludeSelect { get; set; } = true;

        public string AdditionalEventsText { get; set; } = "";

        public List<string> GetBaseEventNames()
        {
            List<string> names = new();

            if (IncludeHover)
                names.Add("Hover");
            if (IncludePress)
                names.Add("Press");
            if (IncludeBack)
                names.Add("Back");
            if (IncludeCancel)
                names.Add("Cancel");
            if (IncludeConfirm)
                names.Add("Confirm");
            if (IncludeSelect)
                names.Add("Select");

            return names;
        }

        public List<string> GetAdditionalEventNames()
        {
            string raw = AdditionalEventsText ?? string.Empty;

            return raw
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<string> GetAllEventNames()
        {
            List<string> names = new();

            foreach (string item in GetBaseEventNames())
            {
                if (!names.Contains(item, StringComparer.OrdinalIgnoreCase))
                    names.Add(item);
            }

            foreach (string item in GetAdditionalEventNames())
            {
                if (!names.Contains(item, StringComparer.OrdinalIgnoreCase))
                    names.Add(item);
            }

            return names;
        }
    }
}
