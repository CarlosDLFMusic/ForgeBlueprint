using ForgeBlueprint.Models;
using ForgeBlueprint.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ForgeBlueprint
{
    public partial class MainWindow : Window
    {
        private readonly AppSettingsService _appSettingsService = new();
        private readonly BlueprintLibraryService _blueprintLibraryService = new();
        private readonly FootstepsBlueprintOptions _footstepsOptions = new();

        private List<BlueprintDefinition> _allBlueprints = new();
        private string _activeFilter = "All";
        private bool _isUpdatingFootstepsUi;

        public MainWindow()
        {
            InitializeComponent();

            _allBlueprints = _blueprintLibraryService.GetBlueprints();

            InitializeFootstepsControls();
            UpdateThemeUi();
            ApplyFilterButtonStates();
            RefreshBlueprintLibrary();

            PreviewMouseDown += MainWindow_PreviewMouseDown;
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject dependencyObject)
                return;

            DependencyObject? current = dependencyObject;

            while (current != null)
            {
                if (current is TextBox || current is ComboBox || current is ComboBoxItem)
                {
                    return;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            Keyboard.ClearFocus();
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

            try
            {
                SpatialModeComboBox.SelectedItem = _footstepsOptions.SpatialMode;
                EventStructureComboBox.SelectedItem = _footstepsOptions.EventStructure;
                NamingPrefixTextBox.Text = _footstepsOptions.NamingPrefix;

                IncludeWaterCheckBox.IsChecked = _footstepsOptions.IncludeWater;
                IncludeSprintCheckBox.IsChecked = _footstepsOptions.IncludeSprint;
                IncludeLandingCheckBox.IsChecked = _footstepsOptions.IncludeLanding;
                IncludeGearCheckBox.IsChecked = _footstepsOptions.IncludeGear;
            }
            finally
            {
                _isUpdatingFootstepsUi = false;
            }
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
            DisplaySelectedBlueprint(BlueprintListBox.SelectedItem as BlueprintDefinition);
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
            BlueprintDefinition? selected = BlueprintListBox.SelectedItem as BlueprintDefinition;

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
            BlueprintDefinition? selected = BlueprintListBox.SelectedItem as BlueprintDefinition;

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
            string previousSelectionKey = (BlueprintListBox.SelectedItem as BlueprintDefinition)?.Key ?? string.Empty;
            string search = (SearchTextBox.Text ?? string.Empty).Trim();

            IEnumerable<BlueprintDefinition> query = _allBlueprints;

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

            List<BlueprintDefinition> filtered = query.ToList();

            BlueprintListBox.ItemsSource = filtered;
            BlueprintCountTextBlock.Text = filtered.Count == 1
                ? "1 blueprint available"
                : $"{filtered.Count} blueprints available";

            BlueprintDefinition? toSelect = filtered.FirstOrDefault(item =>
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

        private void DisplaySelectedBlueprint(BlueprintDefinition? blueprint)
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
                UpdateFootstepsPreview();

                StatusTitleTextBlock.Text = blueprint.Name;
                StatusSubtitleTextBlock.Text = "Footsteps blueprint is now configurable.";
            }
            else
            {
                SetFootstepsConfiguratorEnabled(false);

                OptionsListBox.ItemsSource = blueprint.StaticOptions;
                GeneratedItemsListBox.ItemsSource = blueprint.StaticGeneratedItems;
                NotesListBox.ItemsSource = blueprint.StaticNotes;

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

            BlueprintDefinition? selected = BlueprintListBox.SelectedItem as BlueprintDefinition;
            if (selected == null || !string.Equals(selected.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
                return;

            _footstepsOptions.SpatialMode = SpatialModeComboBox.SelectedItem as string ?? "3D";
            _footstepsOptions.EventStructure = EventStructureComboBox.SelectedItem as string ?? "Single Master Event";
            _footstepsOptions.NamingPrefix = SanitizeToken(NamingPrefixTextBox.Text, "char");

            _footstepsOptions.IncludeWater = IncludeWaterCheckBox.IsChecked == true;
            _footstepsOptions.IncludeSprint = IncludeSprintCheckBox.IsChecked == true;
            _footstepsOptions.IncludeLanding = IncludeLandingCheckBox.IsChecked == true;
            _footstepsOptions.IncludeGear = IncludeGearCheckBox.IsChecked == true;

            UpdateFootstepsPreview();

            StatusTitleTextBlock.Text = "Footsteps Starter";
            StatusSubtitleTextBlock.Text = "Preview updated from the current Footsteps settings.";
        }

        private void UpdateFootstepsPreview()
        {
            OptionsListBox.ItemsSource = BuildFootstepsOptionSummary();
            GeneratedItemsListBox.ItemsSource = BuildFootstepsGeneratedItems();
            NotesListBox.ItemsSource = BuildFootstepsNotes();
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
                _footstepsOptions.IncludeWater ? "Water surface: included" : "Water surface: disabled",
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
    }
}