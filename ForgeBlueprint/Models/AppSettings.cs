using System;

namespace ForgeBlueprint.Models
{
    public sealed class AppSettings
    {
        public string DefaultProjectsFolder { get; set; } = "";
        public string DefaultExportFolder { get; set; } = "";

        public bool IncludePackageJsonByDefault { get; set; } = true;
        public bool IncludeAssetsCsvByDefault { get; set; } = true;
        public bool IncludeExportReportByDefault { get; set; } = true;

        public bool PreserveProjectFoldersByDefault { get; set; } = true;
        public bool SanitizeNamesByDefault { get; set; } = true;
        public bool SkipMissingFilesByDefault { get; set; } = true;
        public bool ResolveDuplicateNamesByDefault { get; set; } = true;

        public string Theme { get; set; } = "Dark";

        public AppSettings Clone()
        {
            return new AppSettings
            {
                DefaultProjectsFolder = DefaultProjectsFolder,
                DefaultExportFolder = DefaultExportFolder,

                IncludePackageJsonByDefault = IncludePackageJsonByDefault,
                IncludeAssetsCsvByDefault = IncludeAssetsCsvByDefault,
                IncludeExportReportByDefault = IncludeExportReportByDefault,

                PreserveProjectFoldersByDefault = PreserveProjectFoldersByDefault,
                SanitizeNamesByDefault = SanitizeNamesByDefault,
                SkipMissingFilesByDefault = SkipMissingFilesByDefault,
                ResolveDuplicateNamesByDefault = ResolveDuplicateNamesByDefault,

                Theme = Theme
            };
        }
    }
}