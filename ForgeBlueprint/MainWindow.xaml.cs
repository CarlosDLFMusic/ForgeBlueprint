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
        private readonly FmodStudioScriptExportService _fmodStudioScriptExportService = new();
        private readonly PresetService _presetService = new();
        private readonly FootstepsBlueprintOptions _footstepsOptions = new();
        private readonly Ui2dBlueprintOptions _ui2dOptions = new();

        private List<BlueprintDefinition> _allBlueprints = new();
        private string _activeFilter = "All";
        private bool _isUpdatingFootstepsUi;
        private bool _isUpdatingUi2dUi;

        public MainWindow()
        {
            InitializeComponent();

            _allBlueprints = _blueprintLibraryService.GetBlueprints();

            InitializeFootstepsControls();
            InitializeUi2dControls();
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

        private void InitializeUi2dControls()
        {
            LoadUi2dControlsIntoUi();
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

        private void LoadUi2dControlsIntoUi()
        {
            _isUpdatingUi2dUi = true;

            try
            {
                IncludeHoverCheckBox.IsChecked = _ui2dOptions.IncludeHover;
                IncludePressCheckBox.IsChecked = _ui2dOptions.IncludePress;
                IncludeBackCheckBox.IsChecked = _ui2dOptions.IncludeBack;
                IncludeCancelCheckBox.IsChecked = _ui2dOptions.IncludeCancel;
                IncludeConfirmCheckBox.IsChecked = _ui2dOptions.IncludeConfirm;
                IncludeSelectCheckBox.IsChecked = _ui2dOptions.IncludeSelect;
                AdditionalUiEventsTextBox.Text = _ui2dOptions.AdditionalEventsText;
            }
            finally
            {
                _isUpdatingUi2dUi = false;
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

        private void LoadPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedKey = (BlueprintListBox.SelectedItem as BlueprintDefinition)?.Key ?? string.Empty;
                string initialFolder = string.Equals(selectedKey, "ui2d", StringComparison.OrdinalIgnoreCase)
                    ? _presetService.GetBlueprintPresetsFolderPath("ui2d")
                    : _presetService.GetBlueprintPresetsFolderPath("footsteps");

                Directory.CreateDirectory(initialFolder);

                OpenFileDialog dialog = new OpenFileDialog
                {
                    Title = "Load preset",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    CheckFileExists = true,
                    InitialDirectory = initialFolder
                };

                bool? result = dialog.ShowDialog(this);
                if (result != true)
                {
                    StatusTitleTextBlock.Text = "Preset loading cancelled";
                    StatusSubtitleTextBlock.Text = "No preset file was selected.";
                    return;
                }

                BlueprintPreset preset = _presetService.LoadPreset(dialog.FileName);
                ApplyPreset(preset);

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

            if (string.Equals(selected.Key, "ui2d", StringComparison.OrdinalIgnoreCase))
            {
                return _presetService.CreateUi2dPreset(selected, _ui2dOptions, GetSuggestedUi2dPresetName());
            }

            throw new NotSupportedException(
                $"The blueprint '{selected.Name}' does not support presets yet.");
        }

        private string GetSuggestedFootstepsPresetName()
        {
            string prefix = string.IsNullOrWhiteSpace(_footstepsOptions.NamingPrefix)
                ? "char"
                : _footstepsOptions.NamingPrefix;

            return $"footsteps_{prefix}";
        }

        private string GetSuggestedUi2dPresetName()
        {
            return "ui2d_default";
        }

        private void ApplyPreset(BlueprintPreset preset)
        {
            if (string.Equals(preset.BlueprintKey, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                if (preset.FootstepsOptions == null)
                    throw new InvalidOperationException("The preset file does not contain footsteps options.");

                EnsureBlueprintSelected("footsteps");
                ApplyFootstepsPreset(preset.FootstepsOptions);
                return;
            }

            if (string.Equals(preset.BlueprintKey, "ui2d", StringComparison.OrdinalIgnoreCase))
            {
                if (preset.Ui2dOptions == null)
                    throw new InvalidOperationException("The preset file does not contain UI 2D options.");

                EnsureBlueprintSelected("ui2d");
                ApplyUi2dPreset(preset.Ui2dOptions);
                return;
            }

            throw new InvalidOperationException($"This preset does not belong to a supported blueprint: {preset.BlueprintKey}.");
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

        private void ApplyUi2dPreset(Ui2dBlueprintOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _ui2dOptions.IncludeHover = options.IncludeHover;
            _ui2dOptions.IncludePress = options.IncludePress;
            _ui2dOptions.IncludeBack = options.IncludeBack;
            _ui2dOptions.IncludeCancel = options.IncludeCancel;
            _ui2dOptions.IncludeConfirm = options.IncludeConfirm;
            _ui2dOptions.IncludeSelect = options.IncludeSelect;
            _ui2dOptions.AdditionalEventsText = options.AdditionalEventsText ?? string.Empty;

            LoadUi2dControlsIntoUi();
            UpdateUi2dPreview();
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
                SaveFileDialog dialog = new SaveFileDialog
                {
                    Title = "Export FMOD Studio script",
                    Filter = "JavaScript files (*.js)|*.js|All files (*.*)|*.*",
                    DefaultExt = ".js",
                    AddExtension = true,
                    FileName = GetSuggestedFmodScriptFileName(selected)
                };

                string defaultFolder = App.CurrentSettings.DefaultExportFolder;
                if (!string.IsNullOrWhiteSpace(defaultFolder) && Directory.Exists(defaultFolder))
                {
                    dialog.InitialDirectory = defaultFolder;
                }

                bool? result = dialog.ShowDialog(this);
                if (result != true)
                {
                    StatusTitleTextBlock.Text = "Export cancelled";
                    StatusSubtitleTextBlock.Text = "No FMOD script file was written.";
                    return;
                }

                _fmodStudioScriptExportService.ExportScript(selected, _footstepsOptions, _ui2dOptions, dialog.FileName);

                string exportFolder = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(exportFolder))
                {
                    App.CurrentSettings.DefaultExportFolder = exportFolder;
                    _appSettingsService.Save(App.CurrentSettings);
                }

                StatusTitleTextBlock.Text = "FMOD script exported";
                StatusSubtitleTextBlock.Text = Path.GetFileName(dialog.FileName);

                MessageBox.Show(
                    "FMOD Studio script exported successfully.\n\n" +
                    "1. Copy the .js file into your FMOD project's Scripts folder or the global FMOD Scripts folder.\n" +
                    "2. In FMOD Studio, use Scripts > Reload.\n" +
                    "3. Run the new Scripts menu item created by the script.\n\n" +
                    dialog.FileName,
                    "ForgeBlueprint",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (NotSupportedException ex)
            {
                StatusTitleTextBlock.Text = "Export not available";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(ex.Message, "Generate", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusTitleTextBlock.Text = "Export failed";
                StatusSubtitleTextBlock.Text = ex.Message;

                MessageBox.Show(
                    $"An error occurred while exporting the FMOD Studio script.\n\n{ex.Message}",
                    "Generate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string GetSuggestedFmodScriptFileName(BlueprintDefinition selected)
        {
            if (string.Equals(selected.Key, "footsteps", StringComparison.OrdinalIgnoreCase))
            {
                return $"footsteps_{_footstepsOptions.NamingPrefix}_fmod.js";
            }

            if (string.Equals(selected.Key, "ui2d", StringComparison.OrdinalIgnoreCase))
            {
                return "ui_2d_fmod.js";
            }

            return $"{selected.Key}_fmod.js";
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
                SetUi2dConfiguratorEnabled(false);

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
                SetUi2dConfiguratorEnabled(false);
                LoadFootstepsControlsIntoUi();
                UpdateFootstepsPreview();

                StatusTitleTextBlock.Text = blueprint.Name;
                StatusSubtitleTextBlock.Text = "Footsteps blueprint is now configurable.";
            }
            else if (string.Equals(blueprint.Key, "ui2d", StringComparison.OrdinalIgnoreCase))
            {
                SetFootstepsConfiguratorEnabled(false);
                SetUi2dConfiguratorEnabled(true);
                LoadUi2dControlsIntoUi();
                UpdateUi2dPreview();

                StatusTitleTextBlock.Text = blueprint.Name;
                StatusSubtitleTextBlock.Text = "UI 2D blueprint is now configurable.";
            }
            else
            {
                SetFootstepsConfiguratorEnabled(false);
                SetUi2dConfiguratorEnabled(false);

                OptionsListBox.ItemsSource = blueprint.StaticOptions;
                GeneratedItemsListBox.ItemsSource = blueprint.StaticGeneratedItems;
                NotesListBox.ItemsSource = blueprint.StaticNotes;

                StatusTitleTextBlock.Text = blueprint.Name;
                StatusSubtitleTextBlock.Text = "Static blueprint preview loaded.";
            }
        }

        private void SetFootstepsConfiguratorEnabled(bool isEnabled)
        {
            FootstepsConfiguratorCard.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
            FootstepsConfiguratorCard.IsEnabled = isEnabled;
            FootstepsConfiguratorCard.Opacity = 1.0;

            FootstepsConfiguratorHintTextBlock.Text = isEnabled
                ? "These controls update the Footsteps Starter preview in real time."
                : "Select Footsteps Starter to enable this configuration panel.";
        }

        private void SetUi2dConfiguratorEnabled(bool isEnabled)
        {
            Ui2dConfiguratorCard.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
            Ui2dConfiguratorCard.IsEnabled = isEnabled;
            Ui2dConfiguratorCard.Opacity = 1.0;

            Ui2dConfiguratorHintTextBlock.Text = isEnabled
                ? "These controls update the UI 2D Starter preview in real time."
                : "Select UI 2D Starter to enable this configuration panel.";
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
            List<string> items = new()
            {
                $"Event: ev_{prefix}_footsteps",
                $"Spatial setup: {_footstepsOptions.SpatialMode}",
                "Parameter: Surfaces",
                $"Logic tracks: {string.Join(", ", _footstepsOptions.GetSurfaceNames())}",
            };

            if (_footstepsOptions.IncludeGear)
            {
                items.Add("Companion track: Gear / Cloth");
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

        private void Ui2dControl_Changed(object sender, RoutedEventArgs e)
        {
            RefreshUi2dStateFromUi();
        }

        private void AdditionalUiEventsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshUi2dStateFromUi();
        }

        private void RefreshUi2dStateFromUi()
        {
            if (_isUpdatingUi2dUi)
                return;

            BlueprintDefinition? selected = BlueprintListBox.SelectedItem as BlueprintDefinition;
            if (selected == null || !string.Equals(selected.Key, "ui2d", StringComparison.OrdinalIgnoreCase))
                return;

            _ui2dOptions.IncludeHover = IncludeHoverCheckBox.IsChecked == true;
            _ui2dOptions.IncludePress = IncludePressCheckBox.IsChecked == true;
            _ui2dOptions.IncludeBack = IncludeBackCheckBox.IsChecked == true;
            _ui2dOptions.IncludeCancel = IncludeCancelCheckBox.IsChecked == true;
            _ui2dOptions.IncludeConfirm = IncludeConfirmCheckBox.IsChecked == true;
            _ui2dOptions.IncludeSelect = IncludeSelectCheckBox.IsChecked == true;
            _ui2dOptions.AdditionalEventsText = AdditionalUiEventsTextBox.Text ?? string.Empty;

            UpdateUi2dPreview();

            StatusTitleTextBlock.Text = "UI 2D Starter";
            StatusSubtitleTextBlock.Text = "Preview updated from the current UI 2D settings.";
        }

        private void UpdateUi2dPreview()
        {
            OptionsListBox.ItemsSource = BuildUi2dOptionSummary();
            GeneratedItemsListBox.ItemsSource = BuildUi2dGeneratedItems();
            NotesListBox.ItemsSource = BuildUi2dNotes();
        }

        private List<string> BuildUi2dOptionSummary()
        {
            List<string> baseEvents = _ui2dOptions.GetBaseEventNames();
            List<string> additionalEvents = _ui2dOptions.GetAdditionalEventNames();

            return new List<string>
            {
                "Spatial model: 2D only",
                "Event folder: UI",
                $"Base events: {(baseEvents.Count > 0 ? string.Join(", ", baseEvents) : "none")}",
                $"Additional events: {(additionalEvents.Count > 0 ? string.Join(", ", additionalEvents) : "none")}",
                $"Total events to create: {_ui2dOptions.GetAllEventNames().Count}"
            };
        }

        private List<string> BuildUi2dGeneratedItems()
        {
            List<string> items = new() { "Folder: UI" };

            foreach (string eventName in _ui2dOptions.GetAllEventNames())
            {
                items.Add($"Event: event:/UI/{eventName}");
            }

            return items;
        }

        private List<string> BuildUi2dNotes()
        {
            List<string> notes = new()
            {
                "This blueprint exports a 2D UI folder in FMOD with one event per selected UI action.",
                "Hover, Press, Back, Cancel, Confirm and Select are available as the default base set.",
                "Any additional UI events are exported exactly as typed, after trimming empty entries and removing duplicates."
            };

            if (_ui2dOptions.GetAllEventNames().Count == 0)
            {
                notes.Add("Select at least one base event or add a custom UI event before exporting.");
            }
            else
            {
                notes.Add("Use the additional events field to add project-specific UI actions without editing the blueprint code.");
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
