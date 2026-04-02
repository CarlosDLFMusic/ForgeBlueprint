using ForgeBlueprint.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ForgeBlueprint
{
    public partial class MainWindow : Window
    {
        private readonly AppSettingsService _appSettingsService = new();
        private readonly List<BlueprintListItem> _allBlueprints;
        private string _activeFilter = "All";

        public MainWindow()
        {
            InitializeComponent();

            _allBlueprints = CreateBlueprints();

            UpdateThemeUi();
            ApplyFilterButtonStates();
            RefreshBlueprintLibrary();
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
                MessageBox.Show(
                    "Select a blueprint first.",
                    "Save Preset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(
                $"Preset saving will come in the next step.\n\nCurrent selection: {selected.Name}",
                "Save Preset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            BlueprintListItem? selected = BlueprintListBox.SelectedItem as BlueprintListItem;

            if (selected == null)
            {
                MessageBox.Show(
                    "Select a blueprint first.",
                    "Generate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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
            string previousSelectionName = (BlueprintListBox.SelectedItem as BlueprintListItem)?.Name ?? string.Empty;
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
                string.Equals(item.Name, previousSelectionName, StringComparison.OrdinalIgnoreCase));

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

            OptionsListBox.ItemsSource = blueprint.Options;
            GeneratedItemsListBox.ItemsSource = blueprint.GeneratedItems;
            NotesListBox.ItemsSource = blueprint.Notes;

            StatusTitleTextBlock.Text = blueprint.Name;
            StatusSubtitleTextBlock.Text = "Shell loaded correctly. Ready for the next implementation step.";
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

        private static List<BlueprintListItem> CreateBlueprints()
        {
            return new List<BlueprintListItem>
            {
                new BlueprintListItem
                {
                    Name = "Footsteps Starter",
                    BlueprintType = "System",
                    Middleware = "FMOD",
                    Category = "Traversal",
                    Summary = "Creates a strong starting shell for character footsteps with common surfaces and room for sprint and landing expansion.",
                    ImplementationGoal = "Set up a reusable footstep implementation structure that can evolve from simple one-shots into a full surface-driven system.",
                    PreviewLead = "This blueprint is ideal as the first real ForgeBlueprint generator because it mixes structure, routing, naming and configuration decisions.",
                    Options = new List<string>
                    {
                        "3D event setup by default",
                        "Choose one master event or split per movement layer",
                        "Surfaces: Stone, Dirt, Grass, Wood, Water",
                        "Optional sprint layer",
                        "Optional landing layer",
                        "Optional gear/cloth companion events"
                    },
                    GeneratedItems = new List<string>
                    {
                        "Event: ev_char_footsteps_master",
                        "Folders for each surface family",
                        "Base naming guide for variations",
                        "Footstep routing group",
                        "Surface-ready implementation shell",
                        "Optional sprint and landing extension points"
                    },
                    Notes = new List<string>
                    {
                        "Best first blueprint to implement in the app.",
                        "Later this can branch into FMOD event generation and Wwise equivalents.",
                        "It represents a real daily production need."
                    }
                },
                new BlueprintListItem
                {
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
                        "Distance/variation placeholders"
                    },
                    Notes = new List<string>
                    {
                        "Good micro-blueprint.",
                        "Useful inside larger project templates."
                    }
                },
                new BlueprintListItem
                {
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
}