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
            RowCount = 3,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label { Name = "AssetStepTitle", Text = _localizationService.GetString("AssetStepTitleGeneric", "If you wish to select the model to be replaced..."), Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
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
        layout.Controls.Add(filters, 0, 1);
        layout.Controls.Add(gallery, 0, 2);
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
            title.Text = GetReplacementTitleForType(sourceType);
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
        state.Caption.BackColor = isSelected ? state.SelectedColor : state.NormalCaptionColor;
        state.Preview.BackColor = isSelected ? state.SelectedColor : state.NormalPreviewColor;

        if (state.OccupiedUseLabel != null)
        {
            state.OccupiedUseLabel.BackColor = isSelected ? state.SelectedColor : state.NormalOccupiedLabelColor;
        }

        if (state.IdBadge != null)
        {
            state.IdBadge.BackColor = isSelected ? state.SelectedColor : state.NormalIdBadgeColor;
        }

        if (state.UnavailableImageLabel != null)
        {
            state.UnavailableImageLabel.BackColor = isSelected ? state.SelectedColor : state.NormalUnavailableImageColor;
        }
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
        var occupiedCaption = isOccupied ? occupiedFolders[0] : asset.NameFile;
        var card = new Panel
        {
            Width = cardWidth,
            Height = cardHeight,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 12),
            BackColor = palette.Card,
            ForeColor = palette.TextPrimary,
            Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        Label? occupiedUseLabel = null;
        if (isOccupied)
        {
            card.Paint += (_, e) => ControlPaint.DrawBorder(
                e.Graphics,
                card.ClientRectangle,
                palette.Warning,
                ButtonBorderStyle.Solid);
        }

        var preview = new PictureBox { Width = innerWidth, Height = previewHeight, Location = new Point(8, 8), SizeMode = PictureBoxSizeMode.Zoom, BackColor = palette.SurfaceSecondary, BorderStyle = BorderStyle.None, Cursor = Cursors.Hand };
        var caption = new AssetCardCaption(
            occupiedCaption,
            isOccupied ? occupiedCaption : asset.Name,
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

        if (isOccupied)
        {
            var isBadgeOnLeft = idBadge != null && !isRtl;
            var isBadgeOnRight = idBadge != null && isRtl;
            var headerLeft = isBadgeOnLeft ? 48 : 8;
            var headerRight = isBadgeOnRight ? 48 : 8;
            var folderText = occupiedFolders.Count == 1
                ? occupiedFolders[0]
                : occupiedFolders[0] + " +" + (occupiedFolders.Count - 1);
            occupiedUseLabel = new Label
            {
                Text = string.Format(_localizationService.GetString("AssetUsedBy", "Used by: {0}"), folderText),
                AutoEllipsis = true,
                AutoSize = false,
                Width = Math.Max(40, cardWidth - headerLeft - headerRight),
                Height = 18,
                Location = new Point(headerLeft, 8),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = palette.Warning,
                BackColor = palette.Card,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(occupiedUseLabel);
            occupiedUseLabel.BringToFront();
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
        occupiedUseLabel?.BringToFront();

        card.Tag = new AssetCardVisualState(
            caption,
            preview,
            occupiedUseLabel,
            idBadge,
            unavailableImageLabel,
            palette.AccentSoft,
            palette.Card,
            caption.BackColor,
            preview.BackColor,
            occupiedUseLabel?.BackColor ?? palette.Card,
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
        var assetTooltip = isOccupied
            ? asset.NameFile + Environment.NewLine + asset.Name
            : string.IsNullOrWhiteSpace(asset.Id) ? asset.NameFile : $"{asset.NameFile}\nID: {asset.Id}";
        tooltip.SetToolTip(preview, assetTooltip);
        if (isOccupied)
        {
            tooltip.SetToolTip(card, assetTooltip);
            tooltip.SetToolTip(caption, assetTooltip);
            if (occupiedUseLabel != null)
            {
                tooltip.SetToolTip(occupiedUseLabel, assetTooltip);
            }
        }
        if (idBadge != null)
        {
            tooltip.SetToolTip(idBadge, isOccupied
                ? $"{asset.NameFile}\n{asset.Name}\nID: {asset.Id}"
                : $"{asset.NameFile}\nID: {asset.Id}");
        }

        void ToggleSelection(object? _, EventArgs __)
        {
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
        if (occupiedUseLabel != null)
        {
            occupiedUseLabel.Click += ToggleSelection;
        }

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
        Label? OccupiedUseLabel,
        Panel? IdBadge,
        Label? UnavailableImageLabel,
        Color SelectedColor,
        Color NormalCardColor,
        Color NormalCaptionColor,
        Color NormalPreviewColor,
        Color NormalOccupiedLabelColor,
        Color NormalIdBadgeColor,
        Color NormalUnavailableImageColor);

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
