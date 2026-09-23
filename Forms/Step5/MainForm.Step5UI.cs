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

        columns.Items.AddRange(new object[] { "2", "3", "4", "5" });
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

        return Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".dff" or ".txd")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? string.Empty;
    }

    private string DetectAssetTypeByName(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName))
        {
            return string.Empty;
        }

        var normalizedModelName = NormalizeAssetIdentifier(modelName);

        return _assetCatalogService.LoadAssets()
            .FirstOrDefault(asset =>
                string.Equals(asset.NameFile, modelName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(asset.Name, modelName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeAssetIdentifier(asset.NameFile), normalizedModelName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeAssetIdentifier(asset.Name), normalizedModelName, StringComparison.OrdinalIgnoreCase))?
            .AssetType ?? string.Empty;
    }

    private static string NormalizeAssetIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        trimmed = trimmed.Replace('\\', '/');
        trimmed = trimmed.TrimEnd('/');
        trimmed = trimmed.Replace(".dff", string.Empty, StringComparison.OrdinalIgnoreCase);
        trimmed = trimmed.Replace(".txd", string.Empty, StringComparison.OrdinalIgnoreCase);
        trimmed = Regex.Replace(trimmed, "[_-]+", "");
        return trimmed.Trim();
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

        var fallbackAsset = _assetCatalogService.LoadAssets().FirstOrDefault();
        return fallbackAsset?.AssetType ?? string.Empty;
    }

    private void PrepareDetectedAssetStep(string payloadPath, GtaSaModManager.Models.ModManifest? manifest)
    {
        _step5DetectedAssets.Clear();
        _step5SelectedAssetKeys.Clear();
        _selectedAssetForInstall = null;

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
        if (string.IsNullOrWhiteSpace(detectedType))
        {
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
        _step5CategoryFilter = !string.IsNullOrWhiteSpace(sourceAsset.Category)
            ? sourceAsset.Category
            : _localizationService.GetString("AssetAll", "All");
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

    private void RefreshAssetStep()
    {
        if (_isRefreshingAssetStep)
        {
            return;
        }

        if (!_wizardPanels.TryGetValue(WizardStep.Step5, out var panel))
        {
            return;
        }

        var gallery = panel.Controls.Find("AssetGallery", false).FirstOrDefault() as FlowLayoutPanel;
        var categoryFilter = panel.Controls.Find("AssetCategoryFilter", false).FirstOrDefault() as ComboBox;
        var categoryLabel = panel.Controls.Find("AssetCategoryLabel", false).FirstOrDefault() as Label;
        var title = panel.Controls.Find("AssetStepTitle", false).FirstOrDefault() as Label;
        var columns = panel.Controls.Find("AssetColumns", false).FirstOrDefault() as ComboBox;
        var sortFilter = panel.Controls.Find("AssetSortMode", false).FirstOrDefault() as ComboBox;
        if (gallery == null)
        {
            return;
        }

        if (_step5DetectedAssets.Count == 0 && !string.IsNullOrWhiteSpace(_selectedModPayloadPath) && Directory.Exists(_selectedModPayloadPath))
        {
            PrepareDetectedAssetStep(_selectedModPayloadPath, _selectedModManifest);
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

        _isRefreshingAssetStep = true;
        gallery.SuspendLayout();
        gallery.Controls.Clear();

        var assets = _step5DetectedAssets
            .Where(asset => string.IsNullOrWhiteSpace(sourceType) || string.Equals(asset.AssetType, sourceType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var categories = assets
            .Select(asset => NormalizeCategoryValue(asset.Category))
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var hasMeaningfulCategories = categories.Count > 1;
        Debug.WriteLine($"[Step5] detectedAssets={assets.Count}; categories={categories.Count}; categoryList={string.Join(" | ", categories)}; selectedFilter={_step5CategoryFilter}; sourceType={sourceType}; meaningful={hasMeaningfulCategories}");

        if (categoryFilter != null)
        {
            categoryFilter.Visible = hasMeaningfulCategories;
            categoryFilter.Enabled = hasMeaningfulCategories;
        }
        if (categoryLabel != null)
        {
            categoryLabel.Visible = hasMeaningfulCategories;
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
            var validSelection = string.Equals(normalizedSelected, NormalizeCategoryValue(allText), StringComparison.OrdinalIgnoreCase)
                || categories.Any(category => string.Equals(NormalizeCategoryValue(category), normalizedSelected, StringComparison.OrdinalIgnoreCase));

            if (!validSelection)
            {
                Debug.WriteLine($"[Step5] category selection reset from '{_step5CategoryFilter}' to '{allText}'");
                _step5CategoryFilter = allText;
            }

            var selectedExists = categoryFilter.Items.Cast<object>().Any(item =>
                string.Equals(NormalizeCategoryValue(item?.ToString()), NormalizeCategoryValue(_step5CategoryFilter), StringComparison.OrdinalIgnoreCase));
            if (!selectedExists)
            {
                _step5CategoryFilter = allText;
            }

            categoryFilter.SelectedItem = _step5CategoryFilter;
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

        var usableGalleryWidth = Math.Max(180, availableGalleryWidth - gallery.Padding.Horizontal);
        var columnGap = 12;
        var columnCount = Math.Max(1, _step5ColumnCount);
        var rawCardWidth = (usableGalleryWidth - (columnCount - 1) * columnGap) / (double)columnCount;
        var baseCardWidth = Math.Max(120, (int)Math.Floor(rawCardWidth));
        var cardHeight = Math.Max(95, (int)Math.Round(baseCardWidth * 0.50d));

        foreach (var asset in visibleAssets)
        {
            var (cardWidth, cardBodyHeight) = GetStep5CardSize(asset.AssetType, baseCardWidth, cardHeight);
            gallery.Controls.Add(CreateAssetCard(asset, cardWidth, cardBodyHeight));
        }

        if (visibleAssets.Count == 0)
        {
            gallery.Controls.Add(new Label { Text = _localizationService.GetString("AssetNoMatching", "No matching assets were found in this Mod."), AutoSize = true, Font = new Font("Segoe UI", 10F), Margin = new Padding(0, 12, 0, 0) });
        }

        gallery.ResumeLayout(true);
        _isRefreshingAssetStep = false;
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

    private Control CreateAssetCard(GameAsset asset, int cardWidth, int cardHeight)
    {
        var palette = ThemeManager.ResolvePalette(ThemeManager.ParseTheme(_settings.Theme));
        var innerWidth = cardWidth - 18;
        var previewHeight = Math.Max(62, cardHeight - 38);
        var selectionKey = GetAssetSelectionKey(asset);
        var isSelected = _step5SelectedAssetKeys.Contains(selectionKey);
        var card = new Panel
        {
            Width = cardWidth,
            Height = cardHeight,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 12),
            BackColor = isSelected ? palette.AccentSoft : palette.Card,
            ForeColor = palette.TextPrimary,
            Cursor = Cursors.Hand,
            Padding = new Padding(0)
        };
        var preview = new PictureBox { Width = innerWidth, Height = previewHeight, Location = new Point(8, 8), SizeMode = PictureBoxSizeMode.Zoom, BackColor = palette.SurfaceSecondary, BorderStyle = BorderStyle.None, Cursor = Cursors.Hand };
        var fileName = new Label { Text = asset.NameFile, AutoSize = false, Width = innerWidth, Height = 22, Location = new Point(8, cardHeight - 28), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = palette.TextPrimary, Cursor = Cursors.Hand };
        var name = new Label { Text = asset.Name, AutoSize = false, Width = innerWidth, Height = 20, Location = new Point(8, cardHeight - 28), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = palette.TextSecondary, Visible = false, Cursor = Cursors.Hand };
        var idText = string.IsNullOrWhiteSpace(asset.Id) ? string.Empty : asset.Id;
        var id = string.IsNullOrWhiteSpace(idText)
            ? null
            : new Label
            {
                Text = idText,
                Width = 44,
                Height = 22,
                Location = new Point(8, previewHeight + 6),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = GetAssetTypeColor(asset.AssetType, palette),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                AutoSize = false,
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand,
                Padding = new Padding(0),
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Visible = true,
                Tag = $"AssetId:{idText}"
            };

        if (id != null)
        {
            id.TextAlign = ContentAlignment.MiddleCenter;
            id.Margin = new Padding(0);
        }

        var imagePath = _assetCatalogService.ResolveImagePath(asset);
        var cachedImage = LoadCachedImage(imagePath);
        if (cachedImage == null)
        {
            preview.Image = null;
            preview.BackColor = palette.SurfaceSecondary;
            preview.Controls.Add(new Label
            {
                Text = _localizationService.GetString("ImageUnavailableFriendly", "No image available"),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = palette.TextSecondary,
                BackColor = palette.SurfaceSecondary
            });
        }
        else
        {
            preview.Image = cachedImage;
            preview.SizeMode = PictureBoxSizeMode.Zoom;
        }

        card.Controls.Add(preview);
        card.Controls.Add(fileName);
        card.Controls.Add(name);
        if (id != null)
        {
            card.Controls.Add(id);
        }

        var tooltip = new ToolTip
        {
            AutoPopDelay = 2000,
            InitialDelay = 250,
            ReshowDelay = 100,
            ShowAlways = true
        };
        tooltip.SetToolTip(preview, $"{asset.NameFile}\nID: {asset.Id}");
        if (id != null)
        {
            tooltip.SetToolTip(id, $"{asset.NameFile}\nID: {asset.Id}");
        }

        void ToggleSelection(object? _, EventArgs __)
        {
            _step5SelectedAssetKeys.Clear();
            _step5SelectedAssetKeys.Add(selectionKey);
            _selectedAssetForInstall = asset;

            foreach (var sibling in card.Parent?.Controls.OfType<Panel>() ?? Enumerable.Empty<Panel>())
            {
                sibling.BackColor = sibling == card ? palette.AccentSoft : palette.Card;
                sibling.ForeColor = palette.TextPrimary;
            }

            card.BackColor = palette.AccentSoft;

            UpdateSidebarState();
            RefreshAssetStep();
        }

        void ShowAssetNameHover(object? _, EventArgs __)
        {
            fileName.Visible = false;
            name.Visible = true;
        }

        void ShowAssetFileNameHover(object? _, EventArgs __)
        {
            fileName.Visible = true;
            name.Visible = false;
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

            OpenFullImageViewer(imageList, selectedIndex, imagePath);
        };

        card.Click += ToggleSelection;
        preview.Click += ToggleSelection;
        fileName.Click += ToggleSelection;
        name.Click += ToggleSelection;
        if (id != null)
        {
            id.Click += ToggleSelection;
        }
        card.MouseEnter += ShowAssetNameHover;
        card.MouseLeave += ShowAssetFileNameHover;
        preview.MouseEnter += ShowAssetNameHover;
        preview.MouseLeave += ShowAssetFileNameHover;
        fileName.MouseEnter += ShowAssetNameHover;
        fileName.MouseLeave += ShowAssetFileNameHover;
        name.MouseEnter += ShowAssetNameHover;
        name.MouseLeave += ShowAssetFileNameHover;
        return card;
    }

    // -------------------------------------------------------------------------
    // تا اینجا برای فایل MainForm.Step5UI.cs
    // -------------------------------------------------------------------------

}
