using ForgeBlueprint.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ForgeBlueprint
{
    public partial class MainWindow : Window
    {
        private readonly AppSettingsService _appSettingsService = new();
        private readonly List<BlueprintListItem> _allBlueprints;
        private readonly FootstepsBlueprintOptions _footstepsOptions = new();

        private string _activeFilter = "All";
        private bool _isUpdatingFootstepsUi;

        public MainWindow()
        {
            InitializeComponent();

            _allBlueprints = CreateBlueprints();

            InitializeFootstepsControls();
            UpdateThemeUi();
            ApplyFilterButtonStates();
            RefreshBlueprintLibrary();
        }

        private void InitializeFootstepsControls()
        {
            SpatialModeComboBox.ItemsSource = new List<string> { "3D", "2D" };
            EventStructureComboBox.ItemsSource = new List<string>
            {
                "Single Master Event",
                "Split by movement layer"
            };

            LoadFootstepsControlsIntoUi();
        }

        private void LoadFootstepsControlsIntoUi()
        {
            _isUpdatingFootstepsUi = true;

            SpatialModeComboBox.SelectedItem = _footstepsOptions.SpatialMode;
            EventStructureComboBox.SelectedItem = _footstepsOptions.EventStructure;
            NamingPrefixTextBox.Text = _footstepsOptions.NamingPrefix;

            IncludeWaterCheckBox.IsChecked = _footstepsOptions.IncludeWater;
            IncludeSprintCheckBox.IsChecked = _footstepsOptions.IncludeSprint;
            IncludeLandingCheckBox.IsChecked = _footstepsOptions.IncludeLanding;
            IncludeGearCheckBox.IsChecked = _footstepsOptions.IncludeGear;

            _isUpdatingFootstepsUi = false;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshBlueprintLibrary();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filter)
            {
                _activeFilter = filter;
                ApplyFilterButtonStates();
                RefreshBlueprintLibrary();
            }
        }

        private void BlueprintListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplaySelectedBlueprint(BlueprintListBox.SelectedItem as BlueprintListItem);
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            bool isCurrentlyLight = string.Equals(App.CurrentSettings.Theme, "Light", StringComparison.OrdinalIgnoreCase);
            string nextTheme = isCurrentlyLight ? "Dark" : "Light";

            App.CurrentSettings.Theme = nextTheme;
            ThemeService.ApplyTheme(Application.Current.Resources, nextTheme);
            _appSettingsService.Save(App.CurrentSettings);

            UpdateThemeUi();
            ApplyFilterButtonStates();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Settings panel will come next.\n\nFor now, the theme toggle is already working.",
                "ForgeBlueprint",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            BlueprintListItem? selected = BlueprintListBox.SelectedItem as BlueprintListItem;

            if (selected == null)
            {
                MessageBox.Show("Select a blueprint first.", "Save Preset", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(
                $"Preset saving will come next.\n\nCurrent selection: {selected.Name}",
                "Save Preset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            BlueprintListItem? selected = BlueprintListBox.SelectedItem as BlueprintListItem;

            if (selected == null)
            {
                MessageBox.Show("Select a blueprint first.", "Generate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(
                $"Generation logic will come next.\n\nCurrent blueprint: {selected.Name}",
                "Generate",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RefreshBlueprintLibrary()
        {
            string previousSelectionKey = (BlueprintListBox.SelectedItem as BlueprintListItem)?.Key ?? string.Empty;
            string search = (SearchTextBox.Text ?? string.Empty).Trim();

            IEnumerable<BlueprintListItem> query = _allBlueprints;

            if (!string.Equals(_activeFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item =>
                    string.Equals(item.BlueprintType, _activeFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item =>
                    item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.BlueprintType.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Middleware.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Summary.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            List<BlueprintListItem> filtered = query.ToList();

            BlueprintListBox.ItemsSource = filtered;
            BlueprintCountTextBlock.Text = filtered.Count == 1
                ? "1 blueprint available"
                : $"{filtered.Count} blueprints available";

            BlueprintListItem? toSelect = filtered.FirstOrDefault(item =>
                string.Equals(item.Key, previousSelectionKey, StringComparison.OrdinalIgnoreCase));

            if (toSelect == null && filtered.Count > 0)
            {
                toSelect = filtered[0];
            }

            BlueprintListBox.SelectedItem = toSelect;

            if (toSelect == null)
            {
                DisplaySelectedBlueprint(null);
            }
        }

        private void DisplaySelectedBlueprint(BlueprintListItem? blueprint)
        {
            if (blueprint == null)
            {
                SelectedBlueprintNameTextBlock.Text = "No blueprint selected";
                SelectedBlueprintSummaryTextBlock.Text = "Choose a blueprint from the left library to see its configuration shell.";
                SelectedTypeTextBlock.Text = "—";
                SelectedMiddlewareTextBlock.Text = "—";
                SelectedCategoryTextBlock.Text = "—";
                ImplementationGoalTextBlock.Text = "—";
                PreviewLeadTextBlock.Text = "The selected blueprint will populate this preview.";

                OptionsListBox.ItemsSource = null;
                GeneratedItemsListBox.ItemsSource = null;
                NotesListBox.ItemsSource = null;

                SetFootstepsConfiguratorEnabled(false);

                StatusTitleTextBlock.Text = "Ready";
                StatusSubtitleTextBlock.Text = "Select a blueprint to continue.";
                return;
            }

            SelectedBlueprintNameTextBlock.Text = blueprint.Name;
            SelectedBlueprintSummaryTextBlock.Text = blueprint.Summary;
            SelectedTypeTextBlock.Text = blueprint.BlueprintType;
            SelectedMiddlewareTextBlock.Text = blueprint.Middleware;
            SelectedCategoryTextBlock.Text = blueprint.Category;
            ImplementationGoalTextBlock.Text = blueprint.ImplementationGoal;
            PreviewLeadTextBlock.Text = blueprint.PreviewLead;

            if (string.Equals(blueprint.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                SetFootstepsConfiguratorEnabled(true);
                LoadFootstepsControlsIntoUi();

                OptionsListBox.ItemsSource = BuildFootstepsOptionSummary();
                GeneratedItemsListBox.ItemsSource = BuildFootstepsGeneratedItems();
                NotesListBox.ItemsSource = BuildFootstepsNotes();

                StatusTitleTextBlock.Text = blueprint.Name;
                StatusSubtitleTextBlock.Text = "Footsteps blueprint is now configurable.";
            }
            else
            {
                SetFootstepsConfiguratorEnabled(false);

                OptionsListBox.ItemsSource = blueprint.Options;
                GeneratedItemsListBox.ItemsSource = blueprint.GeneratedItems;
                NotesListBox.ItemsSource = blueprint.Notes;

                StatusTitleTextBlock.Text = blueprint.Name;
                StatusSubtitleTextBlock.Text = "Static blueprint preview loaded.";
            }
        }

        private void SetFootstepsConfiguratorEnabled(bool isEnabled)
        {
            FootstepsConfiguratorCard.IsEnabled = isEnabled;
            FootstepsConfiguratorCard.Opacity = isEnabled ? 1.0 : 0.55;
            FootstepsConfiguratorHintTextBlock.Text = isEnabled
                ? "These controls update the Footsteps Starter preview in real time."
                : "Select Footsteps Starter to enable this configuration panel.";
        }

        private void FootstepsControl_Changed(object sender, RoutedEventArgs e)
        {
            RefreshFootstepsStateFromUi();
        }

        private void NamingPrefixTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFootstepsStateFromUi();
        }

        private void RefreshFootstepsStateFromUi()
        {
            if (_isUpdatingFootstepsUi)
                return;

            BlueprintListItem? selected = BlueprintListBox.SelectedItem as BlueprintListItem;
            if (selected == null || !string.Equals(selected.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
                return;

            _footstepsOptions.SpatialMode = SpatialModeComboBox.SelectedItem as string ?? "3D";
            _footstepsOptions.EventStructure = EventStructureComboBox.SelectedItem as string ?? "Single Master Event";
            _footstepsOptions.NamingPrefix = SanitizeToken(NamingPrefixTextBox.Text, "char");

            _footstepsOptions.IncludeWater = IncludeWaterCheckBox.IsChecked == true;
            _footstepsOptions.IncludeSprint = IncludeSprintCheckBox.IsChecked == true;
            _footstepsOptions.IncludeLanding = IncludeLandingCheckBox.IsChecked == true;
            _footstepsOptions.IncludeGear = IncludeGearCheckBox.IsChecked == true;

            OptionsListBox.ItemsSource = BuildFootstepsOptionSummary();
            GeneratedItemsListBox.ItemsSource = BuildFootstepsGeneratedItems();
            NotesListBox.ItemsSource = BuildFootstepsNotes();

            StatusTitleTextBlock.Text = "Footsteps Starter";
            StatusSubtitleTextBlock.Text = "Preview updated from the current Footsteps settings.";
        }

        private List<string> BuildFootstepsOptionSummary()
        {
            string surfaces = string.Join(", ", _footstepsOptions.GetSurfaceNames());

            return new List<string>
            {
                $"Spatial mode: {_footstepsOptions.SpatialMode}",
                $"Event structure: {_footstepsOptions.EventStructure}",
                $"Naming prefix: {_footstepsOptions.NamingPrefix}",
                $"Surfaces: {surfaces}",
                _footstepsOptions.IncludeSprint ? "Sprint layer: included" : "Sprint layer: disabled",
                _footstepsOptions.IncludeLanding ? "Landing layer: included" : "Landing layer: disabled",
                _footstepsOptions.IncludeGear ? "Gear / cloth companion: included" : "Gear / cloth companion: disabled"
            };
        }

        private List<string> BuildFootstepsGeneratedItems()
        {
            string prefix = _footstepsOptions.NamingPrefix;
            List<string> items = new();

            if (string.Equals(_footstepsOptions.EventStructure, "Single Master Event", StringComparison.OrdinalIgnoreCase))
            {
                items.Add($"Event: ev_{prefix}_footsteps_master");
            }
            else
            {
                items.Add($"Event: ev_{prefix}_footsteps_walk");
                if (_footstepsOptions.IncludeSprint)
                    items.Add($"Event: ev_{prefix}_footsteps_sprint");
                if (_footstepsOptions.IncludeLanding)
                    items.Add($"Event: ev_{prefix}_footsteps_landing");
            }

            items.Add($"Spatial setup: {_footstepsOptions.SpatialMode}");
            items.Add($"Folders: {string.Join(", ", _footstepsOptions.GetSurfaceNames())}");
            items.Add($"Naming guide: foot_{prefix}_surface_var##");

            if (_footstepsOptions.IncludeGear)
            {
                items.Add($"Companion event: ev_{prefix}_gear_movement");
            }

            if (_footstepsOptions.IncludeWater)
            {
                items.Add("Water surface extension included");
            }

            return items;
        }

        private List<string> BuildFootstepsNotes()
        {
            List<string> notes = new()
            {
                "This blueprint should be the first fully implemented generator in ForgeBlueprint.",
                "It is representative because it mixes naming, routing, surfaces and optional layers."
            };

            if (string.Equals(_footstepsOptions.SpatialMode, "2D", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("2D mode fits menu-driven, side-view or simplified implementation needs.");
            }
            else
            {
                notes.Add("3D mode is better for world-space traversal and character-driven gameplay.");
            }

            if (string.Equals(_footstepsOptions.EventStructure, "Split by movement layer", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("Split mode makes it easier to separate walk, sprint and landing logic.");
            }
            else
            {
                notes.Add("Single master mode is faster for initial setup and easier to maintain.");
            }

            if (_footstepsOptions.IncludeGear)
            {
                notes.Add("Gear / cloth is enabled, so the system can scale into fuller character movement design.");
            }

            return notes;
        }

        private void ApplyFilterButtonStates()
        {
            ApplyFilterButtonState(AllFilterButton, "All");
            ApplyFilterButtonState(SystemFilterButton, "System");
            ApplyFilterButtonState(ProjectFilterButton, "Project");
            ApplyFilterButtonState(EventFilterButton, "Event");
        }

        private void ApplyFilterButtonState(Button button, string filterName)
        {
            bool isActive = string.Equals(_activeFilter, filterName, StringComparison.OrdinalIgnoreCase);

            if (isActive)
            {
                button.SetResourceReference(Button.BackgroundProperty, "AccentBrush");
                button.SetResourceReference(Button.ForegroundProperty, "AccentTextBrush");
                button.SetResourceReference(Button.BorderBrushProperty, "AccentBrushStrong");
            }
            else
            {
                button.SetResourceReference(Button.BackgroundProperty, "SurfaceBrush");
                button.SetResourceReference(Button.ForegroundProperty, "TextPrimaryBrush");
                button.SetResourceReference(Button.BorderBrushProperty, "BorderBrushCustom");
            }
        }

        private void UpdateThemeUi()
        {
            bool isLight = string.Equals(App.CurrentSettings.Theme, "Light", StringComparison.OrdinalIgnoreCase);

            ThemeModeTextBlock.Text = isLight ? "Light" : "Dark";
            ThemeToggleButton.Content = isLight ? "Switch to Dark" : "Switch to Light";

            HeaderLogoImage.Source = LoadPackBitmap(isLight
                ? "Assets/forgevault_logo_inv.png"
                : "Assets/forgevault_logo.png");

            HeaderWordmarkImage.Source = LoadPackBitmap(isLight
                ? "Assets/forgevault_tipo_inv.png"
                : "Assets/forgevault_tipo.png");
        }

        private static BitmapImage LoadPackBitmap(string relativePath)
        {
            return new BitmapImage(new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute));
        }

        private static string SanitizeToken(string? value, string fallback)
        {
            string text = (value ?? string.Empty).Trim().ToLowerInvariant();
            text = Regex.Replace(text, @"[^a-z0-9_]+", "_");
            text = Regex.Replace(text, @"_+", "_").Trim('_');

            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        private static List<BlueprintListItem> CreateBlueprints()
        {
            return new List<BlueprintListItem>
            {
                new BlueprintListItem
                {
                    Key = "footsteps",
                    Name = "Footsteps Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Traversal",
                    Summary = "Creates a strong starting shell for character footsteps with common surfaces and room for sprint and landing expansion.",
                    ImplementationGoal = "Set up a reusable footstep implementation structure that can evolve from simple one-shots into a full surface-driven system.",
                    PreviewLead = "This blueprint is ideal as the first real ForgeBlueprint generator because it mixes structure, routing, naming and configuration decisions."
                },
                new BlueprintListItem
                {
                    Key = "ui2d",
                    Name = "UI 2D Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Interface",
                    Summary = "Builds a clean 2D UI structure with common feedback categories for navigation, confirmations and warnings.",
                    ImplementationGoal = "Give menus and HUD feedback a clean and reusable implementation shell from minute one.",
                    PreviewLead = "This blueprint focuses on fast setup, consistency and naming order rather than complex audio logic.",
                    Options = new List<string>
                    {
                        "2D event setup by default",
                        "Menu / HUD / notification split",
                        "Optional hover / click / back / confirm families",
                        "Error and reward feedback placeholders"
                    },
                    GeneratedItems = new List<string>
                    {
                        "Event group for buttons and menu interactions",
                        "Notification category shell",
                        "Errors / rewards / transitions structure",
                        "UI naming convention starter"
                    },
                    Notes = new List<string>
                    {
                        "Great quick-win blueprint.",
                        "Very useful for almost every game genre."
                    }
                },
                new BlueprintListItem
                {
                    Key = "weaponbasic",
                    Name = "Weapon Basic Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Combat",
                    Summary = "Sets up a compact weapon implementation structure for swings, impacts and optional sweeteners.",
                    ImplementationGoal = "Avoid rebuilding the same combat skeleton each time a new project starts.",
                    PreviewLead = "This is a strong second-step blueprint after footsteps.",
                    Options = new List<string>
                    {
                        "2D or 3D choice depending on game camera",
                        "Swing and impact families",
                        "Optional cloth / body / armor splits",
                        "Optional layered sweetener track"
                    },
                    GeneratedItems = new List<string>
                    {
                        "Weapon master category",
                        "Swing event shell",
                        "Impact family structure",
                        "Variation-ready folders"
                    },
                    Notes = new List<string>
                    {
                        "Very useful for action games.",
                        "Can later expand into melee / ranged / explosive presets."
                    }
                },
                new BlueprintListItem
                {
                    Key = "3doneshot",
                    Name = "3D One-Shot Starter",
                    BlueprintType = "Event",
                    Middleware = "FMOD",
                    Category = "Generic",
                    Summary = "Creates a generic 3D one-shot event template for fast implementation of world-based sounds.",
                    ImplementationGoal = "Speed up repetitive authoring when a project needs many simple spatialized sounds.",
                    PreviewLead = "This is one of the smallest useful building blocks and later can become a macro generator.",
                    Options = new List<string>
                    {
                        "3D spatialization base",
                        "Optional max distance preset",
                        "Optional random multi-variation shell"
                    },
                    GeneratedItems = new List<string>
                    {
                        "Single event shell",
                        "3D ready routing",
                        "Distance / variation placeholders"
                    },
                    Notes = new List<string>
                    {
                        "Good micro-blueprint.",
                        "Useful inside larger project templates."
                    }
                },
                new BlueprintListItem
                {
                    Key = "metroidvania",
                    Name = "Metroidvania Audio Starter",
                    BlueprintType = "Project",
                    Middleware = "FMOD",
                    Category = "Genre Template",
                    Summary = "A higher-level project scaffold for character traversal, ambience, enemies, UI and adaptive exploration cues.",
                    ImplementationGoal = "Kick off a full middleware architecture tailored to a side-scrolling exploration game.",
                    PreviewLead = "Project blueprints come after system blueprints, but this preview helps define the long-term vision.",
                    Options = new List<string>
                    {
                        "Traversal and movement categories",
                        "Enemy and boss separation",
                        "Exploration ambience shell",
                        "UI and map feedback layer",
                        "Music ready grouping"
                    },
                    GeneratedItems = new List<string>
                    {
                        "Top-level category layout",
                        "Bus and routing starter",
                        "System blueprint insertion points"
                    },
                    Notes = new List<string>
                    {
                        "Not for the first implementation pass.",
                        "Useful later when the core generator is stable."
                    }
                },
                new BlueprintListItem
                {
                    Key = "towerdefense",
                    Name = "Tower Defense Audio Starter",
                    BlueprintType = "Project",
                    Middleware = "FMOD",
                    Category = "Genre Template",
                    Summary = "A project scaffold focused on waves, tower actions, enemy reads, notifications and top-down clarity.",
                    ImplementationGoal = "Standardize the first middleware pass for strategy-oriented games with repeated gameplay loops.",
                    PreviewLead = "Another future-facing project blueprint once the system generators are proven.",
                    Options = new List<string>
                    {
                        "Towers, enemies and UI categories",
                        "Wave progression feedback shell",
                        "Notification and alert structure",
                        "Music escalation entry points"
                    },
                    GeneratedItems = new List<string>
                    {
                        "Genre-based top-level layout",
                        "Gameplay feedback families",
                        "System blueprint docking points"
                    },
                    Notes = new List<string>
                    {
                        "Good long-term template target.",
                        "Not needed for the first coding milestone."
                    }
                }
            };
        }
    }

    public sealed class BlueprintListItem
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string BlueprintType { get; set; } = "";
        public string Middleware { get; set; } = "";
        public string Category { get; set; } = "";
        public string Summary { get; set; } = "";
        public string ImplementationGoal { get; set; } = "";
        public string PreviewLead { get; set; } = "";
        public List<string> Options { get; set; } = new();
        public List<string> GeneratedItems { get; set; } = new();
        public List<string> Notes { get; set; } = new();
    }

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