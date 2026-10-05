using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GtaSaModManager.Models;
using GtaSaModManager.Services;
using GtaSaModManager.UI;

namespace GtaSaModManager.Forms;

public partial class MainForm : Form
{
    // -------------------------------------------------------------------------
    // از اینجا
    // -------------------------------------------------------------------------
    private Panel CreateWizardStep5()
    {
        var panel = new Panel { Name = "AssetStepPanel", BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(18) };
        var layout = new TableLayoutPanel
        {
            Name = "AssetStepLayout",
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label { Name = "AssetStepTitle", Text = _localizationService.GetString("AssetStepTitleGeneric", "If you wish to select the model to be replaced..."), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 12, 8) };
        var reviewModelsButton = new Button
        {
            Name = "MultiAssetReviewButton",
            Text = _localizationService.GetString("MultiAssetReviewButton", "Mod Status"),
            AutoSize = true,
            Height = 32,
            Visible = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        reviewModelsButton.Click += (_, _) => ShowMultiAssetReview();
        var header = new FlowLayoutPanel
        {
            Name = "AssetStepHeader",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        header.Controls.Add(title);
        header.Controls.Add(reviewModelsButton);
        var filters = new FlowLayoutPanel { Name = "AssetFilters", Dock = DockStyle.Fill, Height = 42, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 4), Margin = new Padding(0, 0, 0, 8) };
        var categoryLabel = new Label { Name = "AssetCategoryLabel", Text = _localizationService.GetString("AssetCategory", "Category"), AutoSize = true, Margin = new Padding(0, 7, 8, 0), Visible = false };
        var categoryFilter = new ComboBox { Name = "AssetCategoryFilter", Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, Visible = false };
        var columns = new ComboBox { Name = "AssetColumns", Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        var sorting = new ComboBox { Name = "AssetSortMode", Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        var columnsLabel = new Label { Name = "AssetColumnsLabel", Text = _localizationService.GetString("AssetColumns", "Columns"), AutoSize = true, Margin = new Padding(18, 7, 8, 0) };
        var sortLabel = new Label { Name = "AssetSortLabel", Text = _localizationService.GetString("AssetSort", "Sort"), AutoSize = true, Margin = new Padding(18, 7, 8, 0) };
        var allText = _localizationService.GetString("AssetAll", "All");

        columns.Items.AddRange(new object[] { "2", "3", "4" });
        columns.SelectedItem = "3";
        sorting.Items.AddRange(new object[]
        {
            _localizationService.GetString("SortByFileName", "Sort by file name"),
            _localizationService.GetString("SortById", "Sort by ID")
        });
        sorting.SelectedItem = _localizationService.GetString("SortByFileName", "Sort by file name");
        _step5SortMode = Step5SortByName;

        var multiAssetActions = new FlowLayoutPanel
        {
            Name = "MultiAssetActions",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 8),
            Visible = false
        };
        var multiAssetTabs = new TabControl
        {
            Name = "MultiAssetTypeTabs",
            Dock = DockStyle.Top,
            Height = 34,
            Visible = false,
            RightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian
                ? RightToLeft.Yes
                : RightToLeft.No,
            RightToLeftLayout = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian
        };
        foreach (var assetType in new[] { "Vehicle", "Skin", "Weapon" })
        {
            multiAssetTabs.TabPages.Add(new TabPage
            {
                Name = "MultiAssetType" + assetType,
                Text = _localizationService.GetString("AssetType" + assetType, assetType),
                Tag = assetType
            });
        }
        multiAssetTabs.SelectedIndexChanged += MultiAssetTypeTabs_SelectedIndexChanged;
        var unknownModelNotice = new Label
        {
            Name = "MultiAssetUnknownNotice",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6),
            Visible = false
        };
        var keepOriginalButton = new Button
        {
            Name = "MultiAssetKeepOriginalButton",
            Text = _localizationService.GetString("KeepOriginalName", "Keep original name"),
            AutoSize = true,
            Height = 34,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 4)
        };
        var skipModelButton = new Button
        {
            Name = "MultiAssetSkipModelButton",
            Text = _localizationService.GetString("SkipThisModel", "Skip this model"),
            AutoSize = true,
            Height = 34,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 4)
        };
        var onlyUnknownModelsButton = new Button
        {
            Name = "MultiAssetOnlyUnknownModelsButton",
            Text = _localizationService.GetString("OnlyUnknownModelsFromHere", "Only review unknown models from here"),
            AutoSize = true,
            Height = 34,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 4)
        };
        var installRemainingButton = new Button
        {
            Name = "MultiAssetInstallRemainingButton",
            Text = _localizationService.GetString("InstallRemainingOriginalNames", "Install the remaining {0} with original names"),
            AutoSize = true,
            Height = 34,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 8, 4),
            Visible = false
        };
        keepOriginalButton.Click += async (_, _) => await KeepCurrentMultiAssetModelAsync();
        skipModelButton.Click += async (_, _) => await SkipCurrentMultiAssetModelAsync();
        onlyUnknownModelsButton.Click += async (_, _) => await OnlyReviewUnknownModelsFromHereAsync();
        installRemainingButton.Click += async (_, _) => await InstallRemainingMultiModelsWithOriginalNamesAsync();
        multiAssetActions.Controls.Add(keepOriginalButton);
        multiAssetActions.Controls.Add(skipModelButton);
        multiAssetActions.Controls.Add(onlyUnknownModelsButton);
        multiAssetActions.Controls.Add(installRemainingButton);

        filters.Controls.Add(categoryLabel);
        filters.Controls.Add(categoryFilter);
        filters.Controls.Add(columnsLabel);
        filters.Controls.Add(columns);
        filters.Controls.Add(sortLabel);
        filters.Controls.Add(sorting);
        var gallery = new FlowLayoutPanel { Name = "AssetGallery", Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 8, 0, 0), Margin = new Padding(0) };
        gallery.SizeChanged += (_, _) => RefreshAssetStep();

        categoryFilter.SelectedIndexChanged += (_, _) =>
        {
            if (categoryFilter.SelectedItem is string selectedCategory)
            {
                _step5CategoryFilter = selectedCategory;
            }

            RefreshAssetStep();
        };

        columns.SelectedIndexChanged += (_, _) =>
        {
            ApplyStep5ColumnCount(columns);
            RefreshAssetStep();
        };

        sorting.SelectedIndexChanged += (_, _) =>
        {
            _step5SortMode = sorting.SelectedItem?.ToString() == _localizationService.GetString("SortById", "Sort by ID") ? Step5SortById : Step5SortByName;
            RefreshAssetStep();
        };

        layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(filters, 0, 1);
        layout.Controls.Add(multiAssetTabs, 0, 2);
        layout.Controls.Add(unknownModelNotice, 0, 3);
        layout.Controls.Add(multiAssetActions, 0, 4);
        layout.Controls.Add(gallery, 0, 5);
        panel.Controls.Add(layout);
        return panel;
    }

    private string DetectSourceModelName(string payloadPath)
    {
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return string.Empty;
        }

        var modelFiles = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(IsModelFile)
            .OrderBy(path => string.Equals(Path.GetExtension(path), ".dff", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var catalogAssets = _assetCatalogService.LoadAssets();
        return modelFiles.FirstOrDefault(modelName => catalogAssets.Any(asset =>
                   string.Equals(asset.NameFile, modelName, StringComparison.OrdinalIgnoreCase)))
            ?? modelFiles.FirstOrDefault()
            ?? string.Empty;
    }

    private static bool IsModelFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".dff", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txd", StringComparison.OrdinalIgnoreCase);
    }

    private List<SourceModel> BuildSourceModels(string payloadPath)
    {
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return new List<SourceModel>();
        }

        var catalogAssets = _assetCatalogService.LoadAssets();
        return Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => IsModelFile(path) && !ModPackageService.IsMetadataOrNonInstallableFile(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .GroupBy(path => Path.GetFileNameWithoutExtension(path) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group =>
            {
                var dffPath = group.FirstOrDefault(path => Path.GetExtension(path).Equals(".dff", StringComparison.OrdinalIgnoreCase));
                var txdPath = group.FirstOrDefault(path => Path.GetExtension(path).Equals(".txd", StringComparison.OrdinalIgnoreCase));
                var matchingAsset = catalogAssets.FirstOrDefault(asset =>
                    string.Equals(asset.NameFile, group.Key, StringComparison.OrdinalIgnoreCase));
                var detectedAssetType = matchingAsset?.AssetType.ToLowerInvariant() switch
                {
                    "vehicle" => "Vehicle",
                    "skin" => "Skin",
                    "weapon" => "Weapon",
                    _ => "Unknown"
                };

                return new SourceModel
                {
                    BaseName = group.Key,
                    DffPath = dffPath,
                    TxdPath = txdPath,
                    DetectedAssetType = detectedAssetType
                };
            })
            .OrderBy(model => model.BaseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> GetSourceModelFilePaths(string payloadPath, string sourceModelName)
    {
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath) || string.IsNullOrWhiteSpace(sourceModelName))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var modelFiles = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(IsModelFile)
            .ToList();
        var sourceFiles = modelFiles
            .Where(path => string.Equals(Path.GetFileNameWithoutExtension(path), sourceModelName, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sourceDff = sourceFiles.FirstOrDefault(path => Path.GetExtension(path).Equals(".dff", StringComparison.OrdinalIgnoreCase));
        var sourceTxd = sourceFiles.FirstOrDefault(path => Path.GetExtension(path).Equals(".txd", StringComparison.OrdinalIgnoreCase));
        if (sourceDff != null && sourceTxd == null)
        {
            var dffFiles = modelFiles.Where(path => Path.GetExtension(path).Equals(".dff", StringComparison.OrdinalIgnoreCase)).ToList();
            var txdFiles = modelFiles.Where(path => Path.GetExtension(path).Equals(".txd", StringComparison.OrdinalIgnoreCase)).ToList();
            if (dffFiles.Count == 1 && txdFiles.Count == 1)
            {
                sourceFiles.Add(txdFiles[0]);
                Debug.WriteLine($"[Assets] paired DFF '{Path.GetFileName(sourceDff)}' with differently named TXD '{Path.GetFileName(txdFiles[0])}'");
            }
        }

        return sourceFiles;
    }

    private string DetectAssetTypeByName(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return string.Empty;
        }

        return _assetCatalogService.LoadAssets()
            .FirstOrDefault(asset => string.Equals(asset.NameFile, modelName, StringComparison.OrdinalIgnoreCase))?
            .AssetType ?? string.Empty;
    }

    private string GetReplacementTitleForType(string assetType)
    {
        return assetType switch
        {
            "Vehicle" => _localizationService.GetString("AssetStepTitleVehicle", "If you wish to select the Vehicle model to be replaced..."),
            "Weapon" => _localizationService.GetString("AssetStepTitleWeapon", "If you wish to select the Weapon model to be replaced..."),
            "Skin" => _localizationService.GetString("AssetStepTitleSkin", "If you wish to select the Skin model to be replaced..."),
            _ => _localizationService.GetString("AssetStepTitleGeneric", "If you wish to select the model to be replaced...")
        };
    }

    private string GetCurrentStep5AssetType()
    {
        if (!string.IsNullOrWhiteSpace(_step5DetectedAssetType))
        {
            return _step5DetectedAssetType;
        }

        if (_step5DetectedAssets.Count > 0)
        {
            return _step5DetectedAssets[0].AssetType;
        }

        var sourceModel = DetectSourceModelName(_selectedModPayloadPath);
        var detectedType = DetectAssetTypeByName(sourceModel);
        if (!string.IsNullOrWhiteSpace(detectedType))
        {
            return detectedType;
        }

        return string.Empty;
    }

    private void PrepareDetectedAssetStep(string payloadPath, GtaSaModManager.Models.ModManifest? manifest)
    {
        if (manifest?.IsMultiAssetPackage == true && _multiSourceModels.Count > 0)
        {
            PrepareMultiSourceModelStep(payloadPath);
            return;
        }

        if (string.Equals(_step5PreparedPayloadPath, payloadPath, StringComparison.OrdinalIgnoreCase)
            && _step5PreparationAttempted)
        {
            return;
        }

        _step5DetectedAssets.Clear();
        _step5SelectedAssetKeys.Clear();
        _selectedAssetForInstall = null;
        _step5PreparedPayloadPath = payloadPath;
        _step5DetectedAssetType = string.Empty;
        _step5UnknownAssetTypeCancelled = false;
        _step5PreparationAttempted = true;

        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return;
        }

        var sourceModelName = DetectSourceModelName(payloadPath);
        if (string.IsNullOrWhiteSpace(sourceModelName))
        {
            return;
        }

        var detectedType = DetectAssetTypeByName(sourceModelName);
        var typeWasUnknown = string.IsNullOrWhiteSpace(detectedType);
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            detectedType = PromptForUnknownAssetType(sourceModelName);
            _step5UnknownAssetTypeCancelled = string.IsNullOrWhiteSpace(detectedType);
        }

        _step5DetectedAssetType = detectedType;
        if (string.IsNullOrWhiteSpace(detectedType))
        {
            if (typeWasUnknown)
            {
                return;
            }

            var fallbackAssets = _assetCatalogService.LoadAssets()
                .OrderBy(asset => asset.AssetType, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Debug.WriteLine($"[Step5] detectedType empty for '{sourceModelName}'; fallbackAssets={fallbackAssets.Count}");
            if (fallbackAssets.Count == 0)
            {
                return;
            }

            foreach (var asset in fallbackAssets)
            {
                _step5DetectedAssets.Add(asset);
            }

            _selectedAssetForInstall = _step5DetectedAssets.FirstOrDefault();
            _step5CategoryFilter = _localizationService.GetString("AssetAll", "All");
            return;
        }

        var catalogAssets = _assetCatalogService.LoadAssets()
            .Where(asset => string.Equals(asset.AssetType, detectedType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var asset in catalogAssets)
        {
            _step5DetectedAssets.Add(asset);
        }

        if (_step5DetectedAssets.Count == 0)
        {
            return;
        }

        var currentlySelected = _selectedAssetForInstall;
        var matchingSelectedAsset = currentlySelected is null
            ? null
            : _step5DetectedAssets.FirstOrDefault(asset =>
                string.Equals(GetAssetSelectionKey(asset), GetAssetSelectionKey(currentlySelected), StringComparison.OrdinalIgnoreCase));

        var sourceAsset = matchingSelectedAsset
            ?? _step5DetectedAssets.FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase))
            ?? _step5DetectedAssets.First();

        _step5SelectedAssetKeys.Clear();
        _step5SelectedAssetKeys.Add(GetAssetSelectionKey(sourceAsset));
        _selectedAssetForInstall = sourceAsset;
        _step5CategoryFilter = GetDefaultStep5Category(detectedType, sourceModelName);
    }

    private void PrepareMultiSourceModelStep(string payloadPath)
    {
        if (_multiSourceModels.Count == 0)
        {
            return;
        }

        _multiIndex = Math.Clamp(_multiIndex, 0, _multiSourceModels.Count - 1);
        _step5PreparedPayloadPath = payloadPath;
        _step5UnknownAssetTypeCancelled = false;
        _step5PreparationAttempted = true;
        var model = _multiSourceModels[_multiIndex];
        if (model.DetectedAssetType == "Unknown"
            && model.Status != SourceModelStatus.Mapped
            && string.IsNullOrWhiteSpace(model.SelectedAssetType))
        {
            if (!model.AssetTypePrompted)
            {
                model.AssetTypePrompted = true;
                model.SelectedAssetType = PromptForUnknownAssetType(model.BaseName);
            }

            if (string.IsNullOrWhiteSpace(model.SelectedAssetType))
            {
                _step5UnknownAssetTypeCancelled = model.AssetTypePrompted;
                _step5DetectedAssetType = string.Empty;
                _step5DetectedAssets.Clear();
                _step5SelectedAssetKeys.Clear();
                _selectedAssetForInstall = null;
                return;
            }
        }

        var initialType = model.TargetAsset != null
            ? model.TargetAsset.AssetType
            : model.DetectedAssetType == "Unknown"
                ? model.SelectedAssetType
                : model.DetectedAssetType;
        SetMultiAssetTypeTab(initialType, refresh: false);

        var notice = _wizardPanels[WizardStep.Step5].Controls.Find("MultiAssetUnknownNotice", true)
            .FirstOrDefault() as Label;
        if (notice != null)
        {
            notice.Text = string.Format(
                _localizationService.GetString("UnknownMultiAssetModel", "'{0}' is not in the asset catalog."),
                model.BaseName);
            notice.Visible = model.DetectedAssetType == "Unknown";
        }
    }

    private void MultiAssetTypeTabs_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingMultiAssetTabs || !IsMultiAssetModelMode() || sender is not TabControl tabs
            || tabs.SelectedTab?.Tag is not string assetType)
        {
            return;
        }

        SetMultiAssetTypeTab(assetType, refresh: true);
    }

    private void SetMultiAssetTypeTab(string assetType, bool refresh)
    {
        if (!new[] { "Vehicle", "Skin", "Weapon" }.Contains(assetType, StringComparer.OrdinalIgnoreCase))
        {
            assetType = "Vehicle";
        }

        var model = _multiSourceModels[_multiIndex];
        var requiredType = model.TargetAsset != null
            ? model.TargetAsset.AssetType
            : model.DetectedAssetType == "Unknown"
                ? model.SelectedAssetType
                : model.DetectedAssetType;
        if (!string.IsNullOrWhiteSpace(requiredType))
        {
            assetType = requiredType;
        }
        else if (model.DetectedAssetType == "Unknown")
        {
            model.SelectedAssetType = assetType;
            _step5UnknownAssetTypeCancelled = false;
        }

        _step5DetectedAssetType = assetType;
        _step5DetectedAssets.Clear();
        _step5DetectedAssets.AddRange(_assetCatalogService.LoadAssets()
            .Where(asset => string.Equals(asset.AssetType, assetType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase));
        _step5SelectedAssetKeys.Clear();
        _selectedAssetForInstall = null;

        var selectedAsset = model.TargetAsset != null
            ? _step5DetectedAssets.FirstOrDefault(asset =>
                string.Equals(GetAssetSelectionKey(asset), GetAssetSelectionKey(model.TargetAsset), StringComparison.OrdinalIgnoreCase))
            : model.Status == SourceModelStatus.Pending
                && string.Equals(model.DetectedAssetType, assetType, StringComparison.OrdinalIgnoreCase)
                    ? _step5DetectedAssets.FirstOrDefault(asset =>
                        string.Equals(asset.NameFile, model.BaseName, StringComparison.OrdinalIgnoreCase))
                    : null;
        if (selectedAsset == null
            && model.Status == SourceModelStatus.Pending
            && model.DetectedAssetType == "Unknown")
        {
            var availableAssets = _step5DetectedAssets
                .Where(asset => !IsMultiAssetTargetUsedByAnotherModel(asset))
                .ToList();
            if (availableAssets.Count > 0)
            {
                selectedAsset = availableAssets[Random.Shared.Next(availableAssets.Count)];
            }
        }

        if (selectedAsset != null && !IsMultiAssetTargetUsedByAnotherModel(selectedAsset))
        {
            _selectedAssetForInstall = selectedAsset;
            _step5SelectedAssetKeys.Add(GetAssetSelectionKey(selectedAsset));
            _step5CategoryFilter = selectedAsset.Category;
        }
        else
        {
            _step5CategoryFilter = _localizationService.GetString("AssetAll", "All");
        }

        if (_wizardPanels.TryGetValue(WizardStep.Step5, out var panel)
            && panel.Controls.Find("MultiAssetTypeTabs", true).FirstOrDefault() is TabControl tabs)
        {
            _isUpdatingMultiAssetTabs = true;
            try
            {
                tabs.SelectedTab = tabs.TabPages.Cast<TabPage>()
                    .FirstOrDefault(page => string.Equals(page.Tag?.ToString(), assetType, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _isUpdatingMultiAssetTabs = false;
            }
        }

        if (refresh)
        {
            RefreshAssetStep();
            UpdateSidebarState();
        }
    }

    private bool IsMultiAssetModelMode()
    {
        return _selectedModManifest?.IsMultiAssetPackage == true
            && _multiSourceModels.Count > 0
            && _multiIndex >= 0
            && _multiIndex < _multiSourceModels.Count;
    }

    private bool IsMultiAssetTargetUsedByAnotherModel(GameAsset asset)
    {
        var selectionKey = GetAssetSelectionKey(asset);
        return _multiSourceModels
            .Where((model, index) => index != _multiIndex)
            .Any(model => model.Status == SourceModelStatus.Mapped
                && model.TargetAsset != null
                && string.Equals(GetAssetSelectionKey(model.TargetAsset), selectionKey, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryStoreCurrentMultiSourceModelChoice()
    {
        if (!IsMultiAssetModelMode())
        {
            return false;
        }

        var model = _multiSourceModels[_multiIndex];
        if (_selectedAssetForInstall == null)
        {
            return model.Status is SourceModelStatus.KeepOriginal or SourceModelStatus.Skipped;
        }

        if (IsMultiAssetTargetUsedByAnotherModel(_selectedAssetForInstall))
        {
            MessageBox.Show(
                _localizationService.GetString(
                    "MultiAssetTargetAlreadyMapped",
                    "This asset is already mapped to another model in this package. Choose a different asset."),
                _appName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        model.Status = SourceModelStatus.Mapped;
        model.TargetAsset = _selectedAssetForInstall;
        return true;
    }

    private async Task AdvanceMultiAssetModelAsync()
    {
        if (!TryStoreCurrentMultiSourceModelChoice())
        {
            return;
        }

        var nextIndex = _multiIndex + 1;
        while (nextIndex < _multiSourceModels.Count
            && _multiSourceModels[nextIndex].Status == SourceModelStatus.Skipped)
        {
            nextIndex++;
        }

        if (nextIndex < _multiSourceModels.Count)
        {
            _multiIndex = nextIndex;
            PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
            RefreshAssetStep();
            UpdateSidebarState();
        }
        else
        {
            await InstallSelectedModAsync();
        }
    }

    private async Task OnlyReviewUnknownModelsFromHereAsync()
    {
        if (!IsMultiAssetModelMode())
        {
            return;
        }

        var startIndex = _multiIndex;
        if (_selectedAssetForInstall != null
            && _multiSourceModels[startIndex].DetectedAssetType == "Unknown")
        {
            _multiSourceModels[startIndex].TargetAsset = _selectedAssetForInstall;
        }

        for (var index = startIndex; index < _multiSourceModels.Count; index++)
        {
            var model = _multiSourceModels[index];
            if (model.DetectedAssetType != "Unknown")
            {
                model.Status = SourceModelStatus.Skipped;
                model.TargetAsset = null;
            }
        }

        var nextUnknownIndex = Enumerable.Range(startIndex, _multiSourceModels.Count - startIndex)
            .FirstOrDefault(index => _multiSourceModels[index].DetectedAssetType == "Unknown"
                && _multiSourceModels[index].Status == SourceModelStatus.Pending, -1);
        if (nextUnknownIndex < 0)
        {
            await InstallSelectedModAsync();
            return;
        }

        _multiIndex = nextUnknownIndex;
        PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
        RefreshAssetStep();
        UpdateSidebarState();
    }

    private async Task KeepCurrentMultiAssetModelAsync()
    {
        if (!IsMultiAssetModelMode())
        {
            return;
        }

        _step5UnknownAssetTypeCancelled = false;
        var model = _multiSourceModels[_multiIndex];
        model.Status = SourceModelStatus.KeepOriginal;
        model.TargetAsset = null;
        _selectedAssetForInstall = null;
        _step5SelectedAssetKeys.Clear();
        await AdvanceMultiAssetModelAsync();
    }

    private async Task SkipCurrentMultiAssetModelAsync()
    {
        if (!IsMultiAssetModelMode())
        {
            return;
        }

        _step5UnknownAssetTypeCancelled = false;
        var model = _multiSourceModels[_multiIndex];
        model.Status = SourceModelStatus.Skipped;
        model.TargetAsset = null;
        _selectedAssetForInstall = null;
        _step5SelectedAssetKeys.Clear();
        await AdvanceMultiAssetModelAsync();
    }

    private async Task InstallRemainingMultiModelsWithOriginalNamesAsync()
    {
        if (!IsMultiAssetModelMode())
        {
            return;
        }

        _step5UnknownAssetTypeCancelled = false;
        for (var index = _multiIndex; index < _multiSourceModels.Count; index++)
        {
            var model = _multiSourceModels[index];
            if (model.Status is SourceModelStatus.Pending or SourceModelStatus.Skipped)
            {
                model.Status = SourceModelStatus.KeepOriginal;
                model.TargetAsset = null;
            }
        }

        await InstallSelectedModAsync();
    }

    private void ShowMultiAssetReview()
    {
        if (_selectedModManifest?.IsMultiAssetPackage != true || _multiSourceModels.Count == 0)
        {
            return;
        }

        if (IsMultiAssetModelMode() && _selectedAssetForInstall != null)
        {
            _multiSourceModels[_multiIndex].TargetAsset = _selectedAssetForInstall;
        }

        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        using var dialog = new Form
        {
            Text = _localizationService.GetString("MultiAssetSummaryTitle", "Review model mappings"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftLayout = isRtl,
            ClientSize = new Size(460, 390)
        };
        var listHost = new FlowLayoutPanel
        {
            Name = "MultiAssetStatusRows",
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = palette.Surface,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No
        };
        var selectedModelIndex = -1;
        for (var index = 0; index < _multiSourceModels.Count; index++)
        {
            var modelIndex = index;
            var model = _multiSourceModels[index];
            var targetName = model.TargetAsset?.NameFile ?? model.TargetAsset?.Name ?? model.BaseName;
            var isSkipped = model.Status == SourceModelStatus.Skipped;
            var isConfirmed = model.Status is SourceModelStatus.Mapped or SourceModelStatus.KeepOriginal;
            var row = new Panel
            {
                Name = "MultiAssetStatusRow" + modelIndex,
                Height = 58,
                Width = 420,
                Margin = new Padding(0),
                Padding = new Padding(24, 6, 20, 4),
                Cursor = Cursors.Hand,
                BackColor = palette.Surface,
                Tag = modelIndex
            };
            var sourceLabel = new Label
            {
                Text = model.BaseName,
                AutoSize = true,
                Location = new Point(0, 3),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = palette.TextPrimary,
                Cursor = Cursors.Hand
            };
            row.Controls.Add(sourceLabel);

            if (!isSkipped)
            {
                var targetText = string.Equals(model.BaseName, targetName, StringComparison.OrdinalIgnoreCase)
                    ? "=> " + targetName
                    : "=> " + string.Format(
                        _localizationService.GetString("ChangedNameTo", "Changed Name To {0}"),
                        targetName);
                var targetLabel = new Label
                {
                    Text = targetText,
                    AutoSize = true,
                    Location = new Point(8, 27),
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = palette.TextSecondary,
                    Tag = "SecondaryText",
                    Cursor = Cursors.Hand
                };
                row.Controls.Add(targetLabel);
            }

            var statusLabel = new Label
            {
                Text = isConfirmed ? "✓" : "×",
                AutoSize = false,
                Width = 25,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(row.Width - 48, 10),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = isConfirmed ? palette.Success : palette.Error,
                Cursor = Cursors.Hand
            };
            row.Controls.Add(statusLabel);

            void SelectModel(object? _, EventArgs __)
            {
                selectedModelIndex = modelIndex;
                dialog.Close();
            }

            void SetRowHover(object? _, EventArgs __)
            {
                row.BackColor = palette.AccentSoft;
            }

            void ClearRowHover(object? _, EventArgs __)
            {
                if (!row.ClientRectangle.Contains(row.PointToClient(Cursor.Position)))
                {
                    row.BackColor = palette.Surface;
                }
            }

            row.Click += SelectModel;
            row.MouseEnter += SetRowHover;
            row.MouseLeave += ClearRowHover;
            foreach (Control child in row.Controls)
            {
                child.Click += SelectModel;
                child.MouseEnter += SetRowHover;
                child.MouseLeave += ClearRowHover;
            }

            listHost.Controls.Add(row);
            if (modelIndex < _multiSourceModels.Count - 1)
            {
                listHost.Controls.Add(new Panel
                {
                    Height = 1,
                    Width = 420,
                    Margin = new Padding(14, 0, 14, 0),
                    BackColor = palette.BorderSoft
                });
            }
        }

        listHost.SizeChanged += (_, _) =>
        {
            var rowWidth = Math.Max(260, listHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
            foreach (var row in listHost.Controls.OfType<Panel>().Where(control => control.Name.StartsWith("MultiAssetStatusRow", StringComparison.Ordinal)))
            {
                row.Width = rowWidth;
                if (row.Controls.OfType<Label>().LastOrDefault() is { Text: "✓" or "×" } statusLabel)
                {
                    statusLabel.Location = new Point(row.Width - 48, 10);
                }
            }
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 6, 8, 6),
            FlowDirection = isRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        var closeButton = new Button
        {
            Text = _localizationService.GetString("Close", "Close"),
            AutoSize = true,
            Height = 28,
            Margin = new Padding(0),
            DialogResult = DialogResult.Cancel
        };
        buttons.Controls.Add(closeButton);
        var dialogLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        dialogLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        dialogLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        dialogLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        dialogLayout.Controls.Add(listHost, 0, 0);
        dialogLayout.Controls.Add(buttons, 0, 1);
        dialog.Controls.Add(dialogLayout);
        dialog.CancelButton = closeButton;
        ThemeManager.ApplyTheme(dialog, ThemeManager.ParseTheme(_settings.Theme));

        dialog.ShowDialog(this);
        if ((uint)selectedModelIndex < (uint)_multiSourceModels.Count)
        {
            _multiIndex = selectedModelIndex;
            GoToStep(WizardStep.Step5);
        }
    }

    private string GetMultiSourceModelTitle()
    {
        var model = _multiSourceModels[_multiIndex];
        return string.Format(
            _localizationService.GetString("MultiModelStepTitle", "Model {0} of {1}: {2}"),
            _multiIndex + 1,
            _multiSourceModels.Count,
            model.BaseName);
    }

    private string GetDefaultStep5Category(string assetType, string sourceModelName)
    {
        var allText = _localizationService.GetString("AssetAll", "All");
        if (string.Equals(assetType, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            return allText;
        }

        return _step5DetectedAssets
            .FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase))?
            .Category ?? allText;
    }

    private static string GetAssetSelectionKey(GameAsset asset)
    {
        return (string.IsNullOrWhiteSpace(asset.AssetType) ? "asset" : asset.AssetType.Trim()) + "|" + (string.IsNullOrWhiteSpace(asset.NameFile) ? asset.Name : asset.NameFile.Trim());
    }

    private Image? LoadCachedImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return null;
        }

        if (_assetImageCache.TryGetValue(imagePath, out var cachedImage))
        {
            return cachedImage;
        }

        try
        {
            var bitmap = TryLoadBitmap(imagePath);
            if (bitmap == null)
            {
                return null;
            }

            _assetImageCache[imagePath] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private List<GameAsset> GetSelectedAssetListForInstall(GtaSaModManager.Models.ModManifest manifest, string payloadPath)
    {
        if (string.IsNullOrWhiteSpace(payloadPath) || !Directory.Exists(payloadPath))
        {
            return new List<GameAsset>();
        }

        var sourceModelName = DetectSourceModelName(payloadPath);
        var sourceType = DetectAssetTypeByName(sourceModelName);
        if (string.IsNullOrWhiteSpace(sourceType)
            && string.Equals(_step5PreparedPayloadPath, payloadPath, StringComparison.OrdinalIgnoreCase))
        {
            sourceType = _step5DetectedAssetType;
        }

        var availableAssets = _assetCatalogService.LoadAssets()
            .Where(asset => string.Equals(asset.AssetType, sourceType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (availableAssets.Count == 0)
        {
            return new List<GameAsset>();
        }

        if (_selectedAssetForInstall != null)
        {
            return availableAssets
                .Where(asset => string.Equals(GetAssetSelectionKey(asset), GetAssetSelectionKey(_selectedAssetForInstall), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var defaultSourceAsset = availableAssets.FirstOrDefault(asset => string.Equals(asset.NameFile, sourceModelName, StringComparison.OrdinalIgnoreCase)) ?? availableAssets.First();
        return defaultSourceAsset is null ? new List<GameAsset>() : new List<GameAsset> { defaultSourceAsset };
    }

    private void ApplyStep5ColumnCount(ComboBox? columns)
    {
        if (columns == null)
        {
            return;
        }

        var selectedValue = columns.SelectedItem?.ToString();
        if (int.TryParse(selectedValue, out var parsedColumns))
        {
            _step5ColumnCount = Math.Clamp(parsedColumns, 2, 6);
        }
        else
        {
            _step5ColumnCount = 3;
        }

        Debug.WriteLine($"[Step5] columns changed => {_step5ColumnCount}");
    }

    private async void RefreshAssetStep()
    {
        try
        {
            await RefreshAssetStepAsync();
        }
        catch (Exception ex)
        {
            HideGlobalLoadingOverlay();
            MessageBox.Show(ex.Message, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task RefreshAssetStepAsync()
    {
        if (_isRefreshingAssetStep)
        {
            return;
        }

        if (!_wizardPanels.TryGetValue(WizardStep.Step5, out var panel))
        {
            return;
        }

        var gallery = panel.Controls.Find("AssetGallery", true).FirstOrDefault() as FlowLayoutPanel;
        var categoryFilter = panel.Controls.Find("AssetCategoryFilter", true).FirstOrDefault() as ComboBox;
        var categoryLabel = panel.Controls.Find("AssetCategoryLabel", true).FirstOrDefault() as Label;
        var title = panel.Controls.Find("AssetStepTitle", true).FirstOrDefault() as Label;
        var columns = panel.Controls.Find("AssetColumns", true).FirstOrDefault() as ComboBox;
        var sortFilter = panel.Controls.Find("AssetSortMode", true).FirstOrDefault() as ComboBox;
        if (gallery == null)
        {
            return;
        }

        _isRefreshingAssetStep = true;
        var galleryLayoutSuspended = false;
        try
        {
        ShowGlobalLoadingOverlay(_localizationService.GetString("LoadingAssets", "Loading..."));
        await Task.Yield();

        if (_step5DetectedAssets.Count == 0 && !string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath))
        {
            PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
        }

        await EnsureStep5OccupiedAssetFoldersAsync();

        _assetCatalogService.LoadAssets();
        if (!string.IsNullOrWhiteSpace(_assetCatalogService.ValidationError)
            && !string.Equals(_lastShownAssetCatalogError, _assetCatalogService.ValidationError, StringComparison.Ordinal))
        {
            _lastShownAssetCatalogError = _assetCatalogService.ValidationError;
            MessageBox.Show(_assetCatalogService.ValidationError, _appName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        var sourceType = GetCurrentStep5AssetType();
        if (title != null)
        {
            title.Text = IsMultiAssetModelMode()
                ? GetMultiSourceModelTitle()
                : GetReplacementTitleForType(sourceType);
        }

        if (columns != null && columns.SelectedItem == null)
        {
            columns.SelectedItem = "3";
        }

        if (columns != null)
        {
            ApplyStep5ColumnCount(columns);
        }

        if (sortFilter != null)
        {
            if (string.IsNullOrWhiteSpace(sortFilter.SelectedItem?.ToString()))
            {
                sortFilter.SelectedItem = _localizationService.GetString("SortByFileName", "Sort by file name");
                _step5SortMode = Step5SortByName;
            }
            else if (sortFilter.SelectedItem?.ToString() == _localizationService.GetString("SortById", "Sort by ID"))
            {
                _step5SortMode = Step5SortById;
            }
            else
            {
                _step5SortMode = Step5SortByName;
            }
        }

        gallery.SuspendLayout();
        galleryLayoutSuspended = true;
        DisposeAssetGalleryControls(gallery);

        var assets = _step5DetectedAssets
            .Where(asset => string.IsNullOrWhiteSpace(sourceType) || string.Equals(asset.AssetType, sourceType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var categories = assets
            .Select(asset => NormalizeCategoryValue(asset.Category))
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasCategories = categories.Count > 0;
        var hasMeaningfulCategories = categories.Count > 1;
        Debug.WriteLine($"[Step5] detectedAssets={assets.Count}; categories={categories.Count}; categoryList={string.Join(" | ", categories)}; selectedFilter={_step5CategoryFilter}; sourceType={sourceType}; meaningful={hasMeaningfulCategories}");

        if (categoryFilter != null)
        {
            categoryFilter.Visible = hasCategories;
            categoryFilter.Enabled = hasCategories;
        }
        if (categoryLabel != null)
        {
            categoryLabel.Visible = hasCategories;
        }

        var allText = _localizationService.GetString("AssetAll", "All");
        if (categoryFilter != null)
        {
            categoryFilter.Items.Clear();
            categoryFilter.Items.Add(allText);
            foreach (var category in categories)
            {
                if (string.Equals(NormalizeCategoryValue(category), NormalizeCategoryValue(allText), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                categoryFilter.Items.Add(category);
            }

            if (string.IsNullOrWhiteSpace(_step5CategoryFilter))
            {
                _step5CategoryFilter = allText;
            }

            var normalizedSelected = NormalizeCategoryValue(_step5CategoryFilter);
            var canonicalCategory = categories.FirstOrDefault(category =>
                string.Equals(NormalizeCategoryValue(category), normalizedSelected, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(normalizedSelected, NormalizeCategoryValue(allText), StringComparison.OrdinalIgnoreCase))
            {
                canonicalCategory = allText;
            }

            if (canonicalCategory == null)
            {
                Debug.WriteLine($"[Step5] category selection reset from '{_step5CategoryFilter}' to '{allText}'");
                canonicalCategory = allText;
            }

            _step5CategoryFilter = canonicalCategory;
            categoryFilter.SelectedItem = canonicalCategory;
            if (categoryFilter.SelectedItem == null)
            {
                categoryFilter.SelectedIndex = 0;
                _step5CategoryFilter = allText;
            }
            else
            {
                _step5CategoryFilter = categoryFilter.SelectedItem.ToString() ?? allText;
            }
        }

        var visibleAssets = assets;
        if (hasMeaningfulCategories && !string.Equals(_step5CategoryFilter, allText, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedSelectedCategory = NormalizeCategoryValue(_step5CategoryFilter);
            visibleAssets = assets
                .Where(asset => string.Equals(NormalizeCategoryValue(asset.Category), normalizedSelectedCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (_step5SortMode == Step5SortById)
        {
            visibleAssets = visibleAssets
                .OrderBy(asset => string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            visibleAssets = visibleAssets
                .OrderBy(asset => asset.NameFile, StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        Debug.WriteLine($"[Step5] finalVisibleAssets={visibleAssets.Count}; filter={_step5CategoryFilter}; sort={_step5SortMode}");

        var availableGalleryWidth = gallery.ClientSize.Width > 0
            ? gallery.ClientSize.Width
            : Math.Max(320, panel.ClientSize.Width - 32);

        var usableGalleryWidth = Math.Max(
            180,
            availableGalleryWidth - gallery.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth);
        var columnCount = Math.Max(1, _step5ColumnCount);
        const int cardRightMargin = 12;
        var rawCardWidth = (usableGalleryWidth - columnCount * cardRightMargin) / (double)columnCount;
        var baseCardWidth = Math.Max(120, (int)Math.Floor(rawCardWidth));
        var cardHeight = Math.Max(95, (int)Math.Round(baseCardWidth * 0.50d));

        for (var index = 0; index < visibleAssets.Count; index++)
        {
            var asset = visibleAssets[index];
            var (cardWidth, cardBodyHeight) = GetStep5CardSize(asset.AssetType, baseCardWidth, cardHeight);
            gallery.Controls.Add(CreateAssetCard(asset, cardWidth, cardBodyHeight));

            if ((index + 1) % 8 == 0 && index + 1 < visibleAssets.Count)
            {
                gallery.ResumeLayout(true);
                galleryLayoutSuspended = false;
                await Task.Delay(16);
                gallery.SuspendLayout();
                galleryLayoutSuspended = true;
            }
        }

        if (visibleAssets.Count == 0)
        {
            gallery.Controls.Add(new Label { Text = _localizationService.GetString("AssetNoMatching", "No matching assets were found in this Mod."), AutoSize = true, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 12, 0, 0) });
        }
        }
        finally
        {
            if (galleryLayoutSuspended && !gallery.IsDisposed)
            {
                gallery.ResumeLayout(true);
            }

            _isRefreshingAssetStep = false;
            HideGlobalLoadingOverlay();
        }
    }

    private async Task EnsureStep5OccupiedAssetFoldersAsync()
    {
        var modLoaderFolder = GameService.GetModLoaderFolder(_selectedGamePath);
        var candidateFolder = Path.Combine(modLoaderFolder, _selectedModName);
        var excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(candidateFolder))
        {
            excludedFolders.Add(Path.GetFullPath(candidateFolder));
        }

        if (!string.IsNullOrWhiteSpace(_step5ExistingInstallationDestination)
            && Directory.Exists(_step5ExistingInstallationDestination))
        {
            excludedFolders.Add(Path.GetFullPath(_step5ExistingInstallationDestination));
        }

        var cacheKey = string.Join("|", new[]
        {
            Path.GetFullPath(_selectedGamePath),
            Path.GetFullPath(_selectedModPackageRoot),
            Path.GetFullPath(candidateFolder),
            string.Join("|", excludedFolders.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        });
        if (_step5OccupiedScanComplete && string.Equals(_step5OccupiedScanKey, cacheKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _step5OccupiedAssetFolders = await Task.Run(() => ScanOccupiedAssetFolders(modLoaderFolder, excludedFolders));
        _step5OccupiedScanKey = cacheKey;
        _step5OccupiedScanComplete = true;
    }

    private static Dictionary<string, List<string>> ScanOccupiedAssetFolders(
        string modLoaderFolder,
        HashSet<string> excludedFolders)
    {
        var foldersByModel = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(modLoaderFolder))
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var filePath in Directory.GetFiles(modLoaderFolder, "*", SearchOption.AllDirectories).Where(IsModelFile))
        {
            var relativePath = Path.GetRelativePath(modLoaderFolder, filePath);
            var separatorIndex = relativePath.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            if (separatorIndex <= 0)
            {
                continue;
            }

            var folderName = relativePath[..separatorIndex];
            var topLevelFolderPath = Path.GetFullPath(Path.Combine(modLoaderFolder, folderName));
            if (excludedFolders.Contains(topLevelFolderPath))
            {
                continue;
            }

            var modelName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(modelName))
            {
                continue;
            }

            if (!foldersByModel.TryGetValue(modelName, out var folderNames))
            {
                folderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foldersByModel[modelName] = folderNames;
            }

            folderNames.Add(folderName);
        }

        return foldersByModel.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void DisposeAssetGalleryControls(Control gallery)
    {
        foreach (Control control in gallery.Controls.Cast<Control>().ToList())
        {
            control.Dispose();
        }

        gallery.Controls.Clear();
    }

    private string PromptForUnknownAssetType(string sourceModelName)
    {
        using var dialog = new Form
        {
            Text = _localizationService.GetStringForLanguage("AssetTypePromptTitle", _settings.Language, "Select asset type"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            RightToLeft = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian ? RightToLeft.Yes : RightToLeft.No,
            RightToLeftLayout = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian,
            ClientSize = new Size(360, 145)
        };

        var description = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 48,
            Text = string.Format(
                _localizationService.GetStringForLanguage("AssetTypePrompt", _settings.Language, "The model '{0}' was not found. Select its asset type."),
                sourceModelName),
            Padding = new Padding(12, 12, 12, 4)
        };
        var typeSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180,
            Location = new Point(12, 58)
        };
        var assetTypes = new[] { "Vehicle", "Skin", "Weapon" };
        typeSelector.Items.AddRange(assetTypes
            .Select(type => (object)_localizationService.GetStringForLanguage("AssetType" + type, _settings.Language, type))
            .ToArray());
        typeSelector.SelectedIndex = 0;

        var okButton = new Button
        {
            Text = _localizationService.GetStringForLanguage("Continue", _settings.Language, "Continue"),
            DialogResult = DialogResult.OK,
            Width = 90,
            Height = 30,
            Location = new Point(258, 100)
        };
        var cancelButton = new Button
        {
            Text = _localizationService.GetStringForLanguage("Cancel", _settings.Language, "Cancel"),
            DialogResult = DialogResult.Cancel,
            Width = 90,
            Height = 30,
            Location = new Point(160, 100)
        };

        typeSelector.SelectedIndexChanged += (_, _) => okButton.Enabled = typeSelector.SelectedIndex >= 0;

        dialog.Controls.Add(description);
        dialog.Controls.Add(typeSelector);
        dialog.Controls.Add(cancelButton);
        dialog.Controls.Add(okButton);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        return dialog.ShowDialog(this) == DialogResult.OK
            ? typeSelector.SelectedIndex >= 0 ? assetTypes[typeSelector.SelectedIndex] : string.Empty
            : string.Empty;
    }

    private static string NormalizeCategoryValue(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        return Regex.Replace(category.Trim(), @"\s+", " ");
    }

    private static (int Width, int Height) GetStep5CardSize(string? assetType, int baseWidth, int baseHeight)
    {
        var normalizedType = assetType?.Trim();
        var width = Math.Max(150, baseWidth);

        if (string.Equals(normalizedType, "Vehicle", StringComparison.OrdinalIgnoreCase))
        {
            var height = Math.Max(120, (int)Math.Round(width * 0.62d));
            return (width, height);
        }

        if (string.Equals(normalizedType, "Weapon", StringComparison.OrdinalIgnoreCase))
        {
            var height = Math.Max(110, (int)Math.Round(width * 0.88d));
            return (width, height);
        }

        if (string.Equals(normalizedType, "Skin", StringComparison.OrdinalIgnoreCase))
        {
            var height = Math.Max(150, (int)Math.Round(width * 1.35d));
            return (width, height);
        }

        return (width, baseHeight);
    }

    private void ApplyCardVisualState(Panel card, bool isSelected)
    {
        if (card.Tag is not AssetCardVisualState state)
        {
            return;
        }

        card.BackColor = isSelected ? state.SelectedColor : state.NormalCardColor;
        state.IsSelected = isSelected;
        state.Caption.BackColor = isSelected ? state.SelectedColor : state.NormalCaptionColor;
        state.Preview.BackColor = isSelected ? state.SelectedColor : state.NormalPreviewColor;

        if (state.IdBadge != null)
        {
            state.IdBadge.BackColor = isSelected ? state.SelectedColor : state.NormalIdBadgeColor;
        }

        if (state.UnavailableImageLabel != null)
        {
            state.UnavailableImageLabel.BackColor = isSelected ? state.SelectedColor : state.NormalUnavailableImageColor;
        }

        card.Invalidate();
    }

    private Control CreateAssetCard(GameAsset asset, int cardWidth, int cardHeight)
    {
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        var innerWidth = cardWidth - 18;
        var previewHeight = Math.Max(62, cardHeight - 38);
        var selectionKey = GetAssetSelectionKey(asset);
        var isSelected = _step5SelectedAssetKeys.Contains(selectionKey);
        var occupiedFolders = _step5OccupiedAssetFolders.TryGetValue(asset.NameFile, out var folders)
            ? folders
            : new List<string>();
        var isOccupied = occupiedFolders.Count > 0;
        var installedModName = isOccupied ? occupiedFolders[0] : asset.NameFile;
        var card = new Step5AssetCardPanel
        {
            Width = cardWidth,
            Height = cardHeight,
            BorderStyle = isOccupied ? BorderStyle.None : BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 12),
            BackColor = palette.Card,
            ForeColor = palette.TextPrimary,
            Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        var isCardHovered = false;
        var glowPhase = MathF.PI / 2F;
        System.Windows.Forms.Timer? glowTimer = null;
        if (isOccupied)
        {
            glowTimer = new System.Windows.Forms.Timer { Interval = 30 };
            glowTimer.Tick += (_, _) =>
            {
                glowPhase += 0.08F;
                if (glowPhase >= MathF.Tau)
                {
                    glowPhase -= MathF.Tau;
                }

                card.Invalidate();
            };
            card.Disposed += (_, _) => glowTimer.Dispose();
        }

        card.Paint += (_, e) =>
        {
            if (card.Tag is AssetCardVisualState { IsSelected: true })
            {
                DrawSelectedAssetCardShadow(e.Graphics, card.ClientRectangle, palette.Warning);
            }
            else if (isOccupied && isCardHovered)
            {
                DrawOccupiedAssetCardGlow(e.Graphics, card.ClientRectangle, palette.Warning, glowPhase);
            }

            if (isOccupied)
            {
                ControlPaint.DrawBorder(
                    e.Graphics,
                    card.ClientRectangle,
                    palette.Warning,
                    ButtonBorderStyle.Solid);
            }
        };

        var preview = new PictureBox { Width = innerWidth, Height = previewHeight, Location = new Point(8, 8), SizeMode = PictureBoxSizeMode.Zoom, BackColor = palette.SurfaceSecondary, BorderStyle = BorderStyle.None, Cursor = Cursors.Hand };
        var caption = new AssetCardCaption(
            asset.NameFile,
            installedModName,
            palette.TextPrimary,
            palette.TextSecondary)
        {
            Width = innerWidth,
            Height = 22,
            Location = new Point(8, cardHeight - 28),
            Cursor = Cursors.Hand
        };
        var idText = string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id;
        var isRtl = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        var idBadge = string.IsNullOrWhiteSpace(idText)
            ? null
            : new Panel
            {
                Width = 36,
                Height = 36,
                Location = new Point(isRtl ? cardWidth - 44 : 8, 8),
                BackColor = GetAssetTypeColor(asset.AssetType, palette),
                Cursor = Cursors.Hand,
                Tag = $"AssetId:{idText}"
            };

        if (idBadge != null)
        {
            SetAssetIdBadgeRegion(idBadge);
            idBadge.Resize += (_, _) =>
            {
                SetAssetIdBadgeRegion(idBadge);
            };
            idBadge.Controls.Add(new Label
            {
                Text = idText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            });
        }

        var catalogImagePath = _assetCatalogService.ResolveImagePath(asset);
        var catalogImage = LoadCachedImage(catalogImagePath)
            ?? LoadCachedImage(ResolveUnavailableAssetImagePath());
        Label? unavailableImageLabel = null;
        if (catalogImage == null)
        {
            preview.Image = null;
            unavailableImageLabel = new Label
            {
                Text = _localizationService.GetString("ImageUnavailableFriendly", "No image available"),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = palette.TextSecondary,
                BackColor = palette.SurfaceSecondary
            };
            preview.Controls.Add(unavailableImageLabel);
        }
        else
        {
            preview.Image = catalogImage;
            preview.SizeMode = PictureBoxSizeMode.Zoom;
        }

        card.Controls.Add(preview);
        card.Controls.Add(caption);
        if (idBadge != null)
        {
            card.Controls.Add(idBadge);
            idBadge.BringToFront();
        }
        card.Tag = new AssetCardVisualState(
            caption,
            preview,
            idBadge,
            unavailableImageLabel,
            palette.AccentSoft,
            palette.Card,
            caption.BackColor,
            preview.BackColor,
            idBadge?.BackColor ?? palette.Card,
            unavailableImageLabel?.BackColor ?? preview.BackColor);
        ApplyCardVisualState(card, isSelected);

        var tooltip = new ToolTip
        {
            AutoPopDelay = 2000,
            InitialDelay = 250,
            ReshowDelay = 100,
            ShowAlways = true
        };
        var installedModTooltip = string.Join(Environment.NewLine, occupiedFolders);
        var assetTooltip = isOccupied
            ? installedModTooltip
            : string.IsNullOrWhiteSpace(asset.Id) ? asset.NameFile : $"{asset.NameFile}\nID: {asset.Id}";
        tooltip.SetToolTip(preview, assetTooltip);
        if (isOccupied)
        {
            tooltip.SetToolTip(card, assetTooltip);
            tooltip.SetToolTip(caption, assetTooltip);
        }
        if (idBadge != null)
        {
            tooltip.SetToolTip(idBadge, isOccupied
                ? $"{installedModTooltip}\nID: {asset.Id}"
                : $"{asset.NameFile}\nID: {asset.Id}");
        }

        void ToggleSelection(object? _, EventArgs __)
        {
            if (IsMultiAssetModelMode() && IsMultiAssetTargetUsedByAnotherModel(asset))
            {
                MessageBox.Show(
                    _localizationService.GetString(
                        "MultiAssetTargetAlreadyMapped",
                        "This asset is already mapped to another model in this package. Choose a different asset."),
                    _appName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_selectedAssetForInstall == null
                || !string.Equals(GetAssetSelectionKey(_selectedAssetForInstall), selectionKey, StringComparison.OrdinalIgnoreCase))
            {
                _occupiedAssetInstallWarningAcknowledged = false;
            }

            _step5SelectedAssetKeys.Clear();
            _step5SelectedAssetKeys.Add(selectionKey);
            _selectedAssetForInstall = asset;

            foreach (var sibling in card.Parent?.Controls.OfType<Panel>() ?? Enumerable.Empty<Panel>())
            {
                if (sibling != card)
                {
                    ApplyCardVisualState(sibling, false);
                }
            }

            ApplyCardVisualState(card, true);

            UpdateSidebarState();
        }

        void UpdateCardHoverState(object? _, EventArgs __)
        {
            if (card.IsDisposed || !card.IsHandleCreated)
            {
                return;
            }

            var isMouseInsideCard = card.ClientRectangle.Contains(card.PointToClient(Cursor.Position));
            Debug.WriteLine($"[Step5Hover] isOccupied={isOccupied}; isMouseInsideCard={isMouseInsideCard}");
            card.BeginInvoke(new Action(() =>
            {
                if (card.IsDisposed)
                {
                    return;
                }

                var mouseIsInsideCard = card.ClientRectangle.Contains(card.PointToClient(Cursor.Position));
                caption.SetHovered(mouseIsInsideCard);

                if (isOccupied)
                {
                    isCardHovered = mouseIsInsideCard;
                    if (mouseIsInsideCard)
                    {
                        glowPhase = MathF.PI / 2F;
                        glowTimer?.Start();
                    }
                    else
                    {
                        glowTimer?.Stop();
                        glowPhase = MathF.PI / 2F;
                    }

                    card.Invalidate();
                }

                if (!isOccupied)
                {
                    return;
                }

                if (!mouseIsInsideCard)
                {
                    preview.Image = catalogImage;
                    if (unavailableImageLabel != null)
                    {
                        unavailableImageLabel.Visible = catalogImage == null;
                    }

                    return;
                }

                var occupiedImage = GetCachedOccupiedAssetImage(occupiedFolders[0], asset.NameFile);
                if (occupiedImage == null)
                {
                    return;
                }

                preview.Image = occupiedImage;
                if (unavailableImageLabel != null)
                {
                    unavailableImageLabel.Visible = false;
                }
            }));
        }

        void WireCardMouseTracking(Control control)
        {
            control.MouseEnter += UpdateCardHoverState;
            control.MouseLeave += UpdateCardHoverState;
            foreach (Control child in control.Controls)
            {
                WireCardMouseTracking(child);
            }
        }

        preview.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var selectedIndex = _step5DetectedAssets
                .Where(assetItem => string.Equals(assetItem.AssetType, asset.AssetType, StringComparison.OrdinalIgnoreCase))
                .Select(assetItem => assetItem)
                .ToList()
                .FindIndex(item => string.Equals(GetAssetSelectionKey(item), selectionKey, StringComparison.OrdinalIgnoreCase));

            var imageList = _step5DetectedAssets
                .Where(assetItem => string.Equals(assetItem.AssetType, asset.AssetType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.NameFile, StringComparer.OrdinalIgnoreCase)
                .Select(item => _assetCatalogService.ResolveImagePath(item))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .Where(path => File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (imageList.Count == 0)
            {
                return;
            }

            if (selectedIndex < 0 || selectedIndex >= imageList.Count)
            {
                selectedIndex = 0;
            }

            OpenFullImageViewer(imageList, selectedIndex, catalogImagePath);
        };

        card.Click += ToggleSelection;
        preview.Click += ToggleSelection;
        caption.Click += ToggleSelection;
        if (idBadge != null)
        {
            idBadge.Click += ToggleSelection;
            foreach (Control child in idBadge.Controls)
            {
                child.Click += ToggleSelection;
            }
        }

        WireCardMouseTracking(card);

        return card;
    }

    private static void DrawOccupiedAssetCardGlow(Graphics graphics, Rectangle bounds, Color color, float phase)
    {
        var pulse = (1F + MathF.Sin(phase)) / 2F;
        const int borderClearance = 4;
        const int glowSpread = 8;
        var previousSmoothingMode = graphics.SmoothingMode;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        for (var inset = glowSpread; inset >= borderClearance; inset--)
        {
            var rect = Rectangle.Inflate(bounds, -inset, -inset);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            var fade = 1F - (inset - borderClearance) / (float)(glowSpread - borderClearance + 1);
            var alpha = (int)MathF.Round(30F * pulse * fade);
            using var pen = new Pen(Color.FromArgb(alpha, color), 2.5F);
            graphics.DrawRectangle(pen, rect);
        }

        graphics.SmoothingMode = previousSmoothingMode;
    }

    private static void DrawSelectedAssetCardShadow(Graphics graphics, Rectangle bounds, Color color)
    {
        const int borderClearance = 2;
        const int shadowSpread = 9;
        var previousSmoothingMode = graphics.SmoothingMode;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        for (var inset = shadowSpread; inset >= borderClearance; inset--)
        {
            var rect = Rectangle.Inflate(bounds, -inset, -inset);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            var distance = Math.Abs(inset - 5.5F) / 4F;
            var alpha = (int)MathF.Round(24F * Math.Max(0F, 1F - distance));
            using var pen = new Pen(Color.FromArgb(alpha, color), 3F);
            graphics.DrawRectangle(pen, rect);
        }

        graphics.SmoothingMode = previousSmoothingMode;
    }

    private Image? GetCachedOccupiedAssetImage(string modFolderName, string nameFile)
    {
        if (string.IsNullOrWhiteSpace(modFolderName) || string.IsNullOrWhiteSpace(nameFile))
        {
            return null;
        }

        var modFolderPath = Path.Combine(GameService.GetModLoaderFolder(_selectedGamePath), modFolderName);
        var normalizedModFolderPath = Path.GetFullPath(modFolderPath);
        var folderCacheKey = "occupied|" + normalizedModFolderPath;
        var assetCacheKey = folderCacheKey + "|" + nameFile;
        var searchedFolders = new List<string> { normalizedModFolderPath };

        var installedImages = GetCachedOccupiedAssetImageFiles(normalizedModFolderPath);
        var imagePath = installedImages.FirstOrDefault(path => IsOccupiedAssetImage(path, nameFile));
        if (imagePath != null)
        {
            var image = LoadCachedOccupiedAssetImage(assetCacheKey, imagePath);
            WriteOccupiedAssetImageDiagnostic("1", nameFile, imagePath, image, searchedFolders);
            return image;
        }

        imagePath = GetPreferredOccupiedAssetImage(installedImages, normalizedModFolderPath);
        if (imagePath != null)
        {
            var image = LoadCachedOccupiedAssetImage(folderCacheKey, imagePath);
            WriteOccupiedAssetImageDiagnostic("2", nameFile, imagePath, image, searchedFolders);
            return image;
        }

        var sourcePackagePath = GetCachedOccupiedAssetSourceFolder(normalizedModFolderPath);
        if (!string.IsNullOrWhiteSpace(sourcePackagePath))
        {
            var normalizedSourcePath = Path.GetFullPath(sourcePackagePath);
            searchedFolders.Add(normalizedSourcePath);
            var sourceImages = GetCachedOccupiedAssetImageFiles(normalizedSourcePath);
            imagePath = sourceImages.FirstOrDefault(path => IsOccupiedAssetImage(path, nameFile))
                ?? GetPreferredOccupiedAssetImage(sourceImages, normalizedSourcePath);
            if (imagePath != null)
            {
                var image = LoadCachedOccupiedAssetImage(
                    IsOccupiedAssetImage(imagePath, nameFile) ? assetCacheKey : folderCacheKey,
                    imagePath);
                WriteOccupiedAssetImageDiagnostic("3", nameFile, imagePath, image, searchedFolders);
                return image;
            }
        }

        if (_assetImageCache.TryGetValue(folderCacheKey, out var cachedFolderImage))
        {
            WriteOccupiedAssetImageDiagnostic("none", nameFile, null, cachedFolderImage, searchedFolders);
            return cachedFolderImage;
        }

        _assetImageCache[folderCacheKey] = null;
        WriteOccupiedAssetImageDiagnostic("none", nameFile, null, null, searchedFolders);
        return null;
    }

    private List<string> GetCachedOccupiedAssetImageFiles(string folderPath)
    {
        var normalizedPath = Path.GetFullPath(folderPath);
        if (_occupiedAssetImageFiles.TryGetValue(normalizedPath, out var cachedFiles))
        {
            return cachedFiles;
        }

        List<string> imageFiles;
        try
        {
            imageFiles = FindImageFiles(normalizedPath)
                .Where(IsSupportedOccupiedAssetImage)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            imageFiles = new List<string>();
        }

        _occupiedAssetImageFiles[normalizedPath] = imageFiles;
        return imageFiles;
    }

    private string? GetCachedOccupiedAssetSourceFolder(string installedFolderPath)
    {
        var normalizedInstalledPath = Path.GetFullPath(installedFolderPath);
        if (_occupiedAssetSourceFolders.TryGetValue(normalizedInstalledPath, out var cachedSourcePath))
        {
            return cachedSourcePath;
        }

        string? sourcePath = null;
        try
        {
            var manifestPath = ModLoaderService.GetGameInstallationsManifestPath(_selectedGamePath);
            var manifest = ModLoaderService.LoadInstallationManifest(manifestPath);
            var entry = manifest.Entries.LastOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.InstalledDestination)
                && string.Equals(
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(item.InstalledDestination)),
                    Path.TrimEndingDirectorySeparator(normalizedInstalledPath),
                    StringComparison.OrdinalIgnoreCase));
            if (entry != null && Directory.Exists(entry.SourcePackagePath))
            {
                sourcePath = Path.GetFullPath(entry.SourcePackagePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {
        }

        _occupiedAssetSourceFolders[normalizedInstalledPath] = sourcePath;
        return sourcePath;
    }

    private Image? LoadCachedOccupiedAssetImage(string cacheKey, string imagePath)
    {
        if (_assetImageCache.TryGetValue(cacheKey, out var cachedImage))
        {
            return cachedImage;
        }

        var image = LoadCachedImage(imagePath);
        _assetImageCache[cacheKey] = image;
        return image;
    }

    private void WriteOccupiedAssetImageDiagnostic(
        string step,
        string nameFile,
        string? imagePath,
        Image? image,
        IReadOnlyCollection<string>? searchedFolders = null)
    {
        var folders = searchedFolders ?? Array.Empty<string>();
        Debug.WriteLine($"[Step5Hover] foldersSearched='{string.Join(" | ", folders)}'; NameFile='{nameFile}'; step={step}; imagePath='{imagePath ?? "not found"}'; LoadCachedImage returned null={image is null}");
    }

    private static string? GetPreferredOccupiedAssetImage(IReadOnlyList<string> imageFiles, string folderPath)
    {
        return imageFiles.FirstOrDefault(path =>
        {
            var relativeParts = Path.GetRelativePath(folderPath, path);
            var fileName = Path.GetFileName(path);
            return fileName.StartsWith("preview", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("screen", StringComparison.OrdinalIgnoreCase)
                || relativeParts.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                    .SkipLast(1)
                    .Any(part => part.Equals("preview", StringComparison.OrdinalIgnoreCase)
                        || part.Equals("screen", StringComparison.OrdinalIgnoreCase)
                        || part.Equals("screens", StringComparison.OrdinalIgnoreCase));
        }) ?? imageFiles.FirstOrDefault();
    }

    private static bool IsSupportedOccupiedAssetImage(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOccupiedAssetImage(string path, string nameFile)
    {
        return IsSupportedOccupiedAssetImage(path)
            && string.Equals(Path.GetFileNameWithoutExtension(path), nameFile, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetAssetIdBadgeRegion(Control badge)
    {
        badge.Region?.Dispose();
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddEllipse(0, 0, badge.Width, badge.Height);
        badge.Region = new Region(path);
    }

    private string? ResolveUnavailableAssetImagePath()
    {
        var isPersian = _localizationService.ParseLanguage(_settings.Language) == SupportedLanguage.Persian;
        var imageNames = isPersian
            ? new[] { "Image_not_available_Persion.png", "Image_not_available_English.png" }
            : new[] { "Image_not_available_English.png", "Image_not_available_Persion.png" };

        return imageNames
            .Select(name => Path.Combine(AppContext.BaseDirectory, "Assets", name))
            .FirstOrDefault(File.Exists);
    }

    private sealed record AssetCardVisualState(
        AssetCardCaption Caption,
        PictureBox Preview,
        Panel? IdBadge,
        Label? UnavailableImageLabel,
        Color SelectedColor,
        Color NormalCardColor,
        Color NormalCaptionColor,
        Color NormalPreviewColor,
        Color NormalIdBadgeColor,
        Color NormalUnavailableImageColor)
    {
        public bool IsSelected { get; set; }
    }

    private sealed class Step5AssetCardPanel : Panel
    {
        public Step5AssetCardPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    private sealed class AssetCardCaption : Control
    {
        private readonly string _fileName;
        private readonly string _displayName;
        private readonly Color _fileNameColor;
        private readonly Color _displayNameColor;
        private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 16 };
        private float _transition;
        private bool _hovered;

        private bool HasDisplayName => !string.IsNullOrWhiteSpace(_displayName)
            && !string.Equals(_displayName, _fileName, StringComparison.OrdinalIgnoreCase);

        public AssetCardCaption(string? fileName, string? displayName, Color fileNameColor, Color displayNameColor)
        {
            _fileName = fileName ?? string.Empty;
            _displayName = displayName ?? string.Empty;
            _fileNameColor = fileNameColor;
            _displayNameColor = displayNameColor;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            BackColor = Color.Transparent;
            _animationTimer.Tick += (_, _) => AdvanceTransition();
            MouseEnter += (_, _) => SetHovered(true);
            MouseLeave += (_, _) => SetHovered(false);
        }

        public void SetHovered(bool hovered)
        {
            if (!HasDisplayName || IsDisposed)
            {
                return;
            }

            _hovered = hovered;
            if (!_animationTimer.Enabled)
            {
                _animationTimer.Start();
            }
        }

        private void AdvanceTransition()
        {
            var target = _hovered ? 1f : 0f;
            _transition = Math.Clamp(_transition + Math.Sign(target - _transition) * 0.16f, 0f, 1f);
            Invalidate();
            if (Math.Abs(_transition - target) < 0.001f)
            {
                _transition = target;
                _animationTimer.Stop();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            using var fileBrush = new SolidBrush(Color.FromArgb((int)Math.Round(255 * (1f - _transition)), _fileNameColor));
            e.Graphics.DrawString(_fileName, Font, fileBrush, ClientRectangle, format);
            if (HasDisplayName)
            {
                using var nameBrush = new SolidBrush(Color.FromArgb((int)Math.Round(255 * _transition), _displayNameColor));
                e.Graphics.DrawString(_displayName, Font, nameBrush, ClientRectangle, format);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Step5UI.cs
    // -------------------------------------------------------------------------

}
