using ForgeBlueprint.Models;
using ForgeBlueprint.Services;
using System.Windows;

namespace ForgeBlueprint
{
    public partial class App : Application
    {
        public static AppSettings CurrentSettings { get; private set; } = new();
        private readonly AppSettingsService _appSettingsService = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            CurrentSettings = _appSettingsService.Load();
            ThemeService.ApplyTheme(Resources, CurrentSettings.Theme);
        }
    }
}
