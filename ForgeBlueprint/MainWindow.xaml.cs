using ForgeBlueprint.Models;
using ForgeBlueprint.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly RecipeExportService _recipeExportService = new();
        private readonly PresetService _presetService = new();
        private readonly FootstepsBlueprintGenerator _footstepsBlueprintGenerator = new();
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
            LoadFootstepsControlsIntoUi();
        }

        private void LoadFootstepsControlsIntoUi()
        {
            _isUpdatingFootstepsUi = true;

            try
            {
                SpatialModeComboBox.SelectedItem = _footstepsOptions.SpatialMode;
                NamingPrefixTextBox.Text = _footstepsOptions.NamingPrefix;
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

        private void LoadPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string presetsFolder = _presetService.GetBlueprintPresetsFolderPath("footsteps");
                Directory.CreateDirectory(presetsFolder);

                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "Load preset",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    CheckFileExists = true,
                    InitialDirectory = presetsFolder
                };

                bool? result = dialog.ShowDialog(this);
                if (result != true)
                {
                    StatusTitleTextBlock.Text = "Preset loading cancelled";
                    StatusSubtitleTextBlock.Text = "No preset file was selected.";
                    return;
                }

                BlueprintDefinition? footstepsBlueprint = _allBlueprints.FirstOrDefault(item =>
                    string.Equals(item.Key, "footsteps", StringComparison.OrdinalIgnoreCase));

                if (footstepsBlueprint == null)
                    throw new InvalidOperationException("Footsteps Starter blueprint was not found in the library.");

                if (!string.Equals((BlueprintListBox.SelectedItem as BlueprintDefinition)?.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
                {
                    BlueprintListBox.SelectedItem = footstepsBlueprint;
                }

                bool loaded = TryLoadFootstepsPresetFromFile(dialog.FileName);
                if (!loaded)
                {
                    StatusTitleTextBlock.Text = "Preset not loaded";
                    StatusSubtitleTextBlock.Text = "The selected file is not a valid Footsteps preset.";

                    MessageBox.Show(
                        "The selected file is not a valid Footsteps preset.",
                        "Load Preset",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                StatusTitleTextBlock.Text = "Preset loaded";
                StatusSubtitleTextBlock.Text = Path.GetFileName(dialog.FileName);

                MessageBox.Show(
                    $"Preset loaded successfully.\n\n{dialog.FileName}",
                    "ForgeBlueprint",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusTitleTextBlock.Text = "Preset load failed";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(
                    $"An error occurred while loading the preset.\n\n{ex.Message}",
                    "Load Preset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            BlueprintDefinition? selected = BlueprintListBox.SelectedItem as BlueprintDefinition;

            if (selected == null)
            {
                MessageBox.Show("Select a blueprint first.", "Save Preset", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                BlueprintPreset preset = BuildPresetForSelectedBlueprint(selected);
                string suggestedFileName = _presetService.BuildSuggestedPresetFileName(selected.Key, preset.PresetName);
                string presetsFolder = _presetService.GetBlueprintPresetsFolderPath(selected.Key);
                Directory.CreateDirectory(presetsFolder);

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Save blueprint preset",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    AddExtension = true,
                    InitialDirectory = presetsFolder,
                    FileName = suggestedFileName
                };

                bool? result = dialog.ShowDialog(this);
                if (result != true)
                {
                    StatusTitleTextBlock.Text = "Preset save cancelled";
                    StatusSubtitleTextBlock.Text = "No preset file was written.";
                    return;
                }

                _presetService.SavePreset(preset, dialog.FileName);

                StatusTitleTextBlock.Text = "Preset saved";
                StatusSubtitleTextBlock.Text = Path.GetFileName(dialog.FileName);

                MessageBox.Show(
                    $"Preset saved successfully.\n\n{dialog.FileName}",
                    "ForgeBlueprint",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (NotSupportedException ex)
            {
                StatusTitleTextBlock.Text = "Preset not available";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(ex.Message, "Save Preset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusTitleTextBlock.Text = "Preset save failed";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(
                    $"An error occurred while saving the preset.\n\n{ex.Message}",
                    "Save Preset",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private BlueprintPreset BuildPresetForSelectedBlueprint(BlueprintDefinition selected)
        {
            if (string.Equals(selected.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                return _presetService.CreateFootstepsPreset(selected, _footstepsOptions, GetSuggestedFootstepsPresetName());
            }

            throw new NotSupportedException(
                $"The blueprint '{selected.Name}' does not support presets yet. Footsteps Starter is the first preset-enabled blueprint.");
        }

        private string GetSuggestedFootstepsPresetName()
        {
            string prefix = string.IsNullOrWhiteSpace(_footstepsOptions.NamingPrefix)
                ? "char"
                : _footstepsOptions.NamingPrefix;

            return $"footsteps_{prefix}";
        }

        private bool TryLoadFootstepsPresetFromFile(string filePath)
        {
            BlueprintPreset preset = _presetService.LoadPreset(filePath);

            if (!string.Equals(preset.BlueprintKey, "footsteps", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This preset does not belong to Footsteps Starter.");

            if (preset.FootstepsOptions == null)
                throw new InvalidOperationException("The preset file does not contain footsteps options.");

            EnsureBlueprintSelected("footsteps");
            ApplyFootstepsPreset(preset.FootstepsOptions);
            return true;
        }

        private void EnsureBlueprintSelected(string blueprintKey)
        {
            if (!string.Equals(_activeFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                _activeFilter = "All";
                ApplyFilterButtonStates();
                RefreshBlueprintLibrary();
            }

            BlueprintDefinition? blueprint = _allBlueprints.FirstOrDefault(item =>
                string.Equals(item.Key, blueprintKey, StringComparison.OrdinalIgnoreCase));

            if (blueprint == null)
                throw new InvalidOperationException($"Blueprint '{blueprintKey}' was not found in the library.");

            BlueprintListBox.SelectedItem = blueprint;
            DisplaySelectedBlueprint(blueprint);
        }

        private void ApplyFootstepsPreset(FootstepsBlueprintOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _footstepsOptions.SpatialMode = string.IsNullOrWhiteSpace(options.SpatialMode) ? "3D" : options.SpatialMode;
            _footstepsOptions.NamingPrefix = SanitizeToken(options.NamingPrefix, "char");
            _footstepsOptions.IncludeGear = options.IncludeGear;

            LoadFootstepsControlsIntoUi();
            UpdateFootstepsPreview();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            BlueprintDefinition? selected = BlueprintListBox.SelectedItem as BlueprintDefinition;

            if (selected == null)
            {
                MessageBox.Show("Select a blueprint first.", "Generate", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                GenerationRecipe recipe = BuildRecipeForSelectedBlueprint(selected);

                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Export generation recipe",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    AddExtension = true,
                    FileName = GetSuggestedRecipeFileName(selected)
                };

                string defaultFolder = App.CurrentSettings.DefaultExportFolder;
                if (!string.IsNullOrWhiteSpace(defaultFolder) && Directory.Exists(defaultFolder))
                {
                    dialog.InitialDirectory = defaultFolder;
                }

                bool? result = dialog.ShowDialog(this);
                if (result != true)
                {
                    StatusTitleTextBlock.Text = "Generation cancelled";
                    StatusSubtitleTextBlock.Text = "No recipe file was exported.";
                    return;
                }

                _recipeExportService.ExportToJson(recipe, dialog.FileName);

                string exportFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(exportFolder))
                {
                    App.CurrentSettings.DefaultExportFolder = exportFolder;
                    _appSettingsService.Save(App.CurrentSettings);
                }

                StatusTitleTextBlock.Text = "Recipe exported";
                StatusSubtitleTextBlock.Text = Path.GetFileName(dialog.FileName);

                MessageBox.Show(
                    $"Recipe exported successfully.\n\n{dialog.FileName}",
                    "ForgeBlueprint",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (NotSupportedException ex)
            {
                StatusTitleTextBlock.Text = "Generation not available";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(ex.Message, "Generate", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusTitleTextBlock.Text = "Generation failed";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(
                    $"An error occurred while generating the recipe.\n\n{ex.Message}",
                    "Generate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private GenerationRecipe BuildRecipeForSelectedBlueprint(BlueprintDefinition selected)
        {
            if (string.Equals(selected.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                return _footstepsBlueprintGenerator.Generate(selected, _footstepsOptions);
            }

            throw new NotSupportedException(
                $"The blueprint '{selected.Name}' is not implemented yet. Footsteps Starter is the first real generator.");
        }

        private string GetSuggestedRecipeFileName(BlueprintDefinition selected)
        {
            if (string.Equals(selected.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                return $"{selected.Key}_{_footstepsOptions.NamingPrefix}_recipe.json";
            }

            return $"{selected.Key}_recipe.json";
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
            _footstepsOptions.NamingPrefix = SanitizeToken(NamingPrefixTextBox.Text, "char");
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
                $"Naming prefix: {_footstepsOptions.NamingPrefix}",
                "Event model: single Footsteps master event",
                "Surface parameter: Surfaces",
                $"Surfaces: {surfaces}",
                _footstepsOptions.IncludeGear
                    ? "Gear / cloth companion track: included"
                    : "Gear / cloth companion track: disabled"
            };
        }

        private List<string> BuildFootstepsGeneratedItems()
        {
            string prefix = _footstepsOptions.NamingPrefix;
            string displayPrefix = string.IsNullOrWhiteSpace(prefix)
                ? "Character"
                : char.ToUpperInvariant(prefix[0]) + prefix.Substring(1);

                List<string> items = new()
            {
                $"Event: Footsteps_{displayPrefix}",
                $"Spatial setup: {_footstepsOptions.SpatialMode}",
                "Surface parameter: Surfaces",
                $"Surface logic tracks: {string.Join(", ", _footstepsOptions.GetSurfaceNames())}",
                $"Naming guide: foot_{prefix}_{{surface}}_var##"
            };

                    if (_footstepsOptions.IncludeGear)
                    {
                        items.Add("Companion logic track: Gear / Cloth");
                    }

                return items;
        }

        private List<string> BuildFootstepsNotes()
        {
            List<string> notes = new()
            {
                "This blueprint now reflects a single FMOD footsteps event driven by a Surfaces parameter.",
                "Each surface should live on its own logic track with a dedicated multi instrument and variation set.",
                "Sprint and landing are intentionally left out of this blueprint so they can be authored as separate events."
            };

            if (string.Equals(_footstepsOptions.SpatialMode, "2D", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add("2D mode fits menu-driven, side-view or simplified implementation needs.");
            }
            else
            {
                notes.Add("3D mode is better for world-space traversal and character-driven gameplay.");
            }

            if (_footstepsOptions.IncludeGear)
            {
                notes.Add("Gear / cloth can be layered as an extra companion logic track inside the same event.");
            }
            else
            {
                notes.Add("Gear / cloth remains disabled, so only the surface logic tracks are represented.");
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
                ? "Assets/ForgeBlueprint_tipo_inv.png"
                : "Assets/ForgeBlueprint_tipo.png");
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