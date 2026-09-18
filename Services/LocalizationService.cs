using System.Globalization;

namespace GtaSaModManager.Services;

public enum SupportedLanguage
{
    English,
    Persian
}

public class LocalizationService
{
    private static readonly Dictionary<string, string> EnglishStrings = new()
    {
        ["AppTitle"] = "GTA San Andreas Mod Manager",
        ["GameStatus"] = "Game Status",
        ["GamePath"] = "Game Path",
        ["GameFolderNotConfigured"] = "Game folder not configured",
        ["OpenGameFolder"] = "Open Game Folder",
        ["RunGame"] = "Run Game",
        ["InstallMod"] = "Install Mod",
        ["Install"] = "Install",
        ["ModLoaderMods"] = "ModLoader Mods",
        ["ModLibraryTitle"] = "Mod Library",
        ["ModLibraryPath"] = "Base Mods Folder",
        ["Settings"] = "Settings",
        ["ModLibrary"] = "Mod Library",
        ["AvailableMods"] = "Available Mods",
        ["InstalledMods"] = "Installed Mods",
        ["Change"] = "Change",
        ["OpenFolder"] = "Open Folder",
        ["Browse"] = "Browse",
        ["Configured"] = "Configured",
        ["NotConfigured"] = "Not configured",
        ["NoModsInstalled"] = "No ModLoader mods installed yet.",
        ["NoModPackagesInLibrary"] = "No mod packages found in this library yet.",
        ["ViewReadme"] = "View README",
        ["GameFolderNotConfiguredMessage"] = "Please select a valid GTA San Andreas folder first.",
        ["SelectMod"] = "Select a mod folder or archive",
        ["ExtractionFailed"] = "The mod archive could not be extracted.",
        ["DuplicateModPrompt"] = "A mod named '{0}' already exists. Replace it?",
        ["Installing"] = "Installing",
        ["InstallationCompleted"] = "Installation completed successfully.",
        ["InstallationFailed"] = "The mod could not be installed.",
        ["ModSourceInvalid"] = "The selected mod could not be read.",
        ["SelectGameFolder"] = "Select the GTA San Andreas folder",
        ["SelectModLibraryFolder"] = "Select the Mod Library / Mods Source Folder",
        ["InvalidGameFolder"] = "This folder does not contain gta_sa.exe.",
        ["InvalidModLibraryFolder"] = "This folder is not a valid mods library folder.",
        ["GameLaunchFailed"] = "The game could not be launched.",
        ["InstallingTitle"] = "Installing",
        ["Theme"] = "Theme",
        ["Language"] = "Language",
        ["Installed"] = "Installed",
        ["Status"] = "Status",
        ["Step1GameFolder"] = "Game folder",
        ["Step2Profile"] = "Base Mods Folder",
        ["Step3Mod"] = "Select mod",
        ["Step4Installing"] = "Installing",
        ["Step5Category"] = "Cars / Weapons / Skins",
        ["Step6Completed"] = "Completed",
        ["GameFolderRequired"] = "Choose your GTA San Andreas folder.",
        ["ModFolder"] = "Mod Folder",
        ["BrowseModFolder"] = "Browse mod folder",
        ["DetectedMod"] = "Detected mod",
        ["CategoryPlaceholder"] = "This category step is reserved for future advanced installers.",
        ["InstallationComplete"] = "Installation complete.",
        ["ReadMe"] = "Read Me",
        ["OpenReadmeFile"] = "Open README.txt",
        ["Previous"] = "Previous",
        ["Next"] = "Next",
        ["ShowMore"] = "Show More",
        ["ShowLess"] = "Show Less",
        ["Close"] = "Close",
        ["Warning"] = "Warning",
        ["Error"] = "Error",
        ["Failed"] = "Failed",
        ["Cancel"] = "Cancel",
        ["Continue"] = "Continue",
        ["Back"] = "Back",
        ["Completed"] = "Completed",
        ["CompletedIn"] = "Completed in",
        ["SelectModFolder"] = "Select the mod folder",
        ["ModJsonInvalid"] = "mod.json is malformed or contains unexpected content. The mod name still uses the folder name.",
        ["SelectModFirst"] = "Please select a valid mod folder first.",
        ["ModFolderEmpty"] = "The selected mod folder is empty or unreadable.",
        ["English"] = "English",
        ["Persian"] = "Persian"
    };

    private static readonly Dictionary<string, string> PersianStrings = new()
    {
        ["AppTitle"] = "مدیر مود GTA San Andreas",
        ["GameStatus"] = "وضعیت بازی",
        ["GamePath"] = "مسیر بازی",
        ["GameFolderNotConfigured"] = "پوشه بازی پیکربندی نشده است",
        ["OpenGameFolder"] = "باز کردن پوشه بازی",
        ["RunGame"] = "اجرای بازی",
        ["InstallMod"] = "نصب مود",
        ["Install"] = "نصب",
        ["ModLoaderMods"] = "مودهای ModLoader",
        ["ModLibraryTitle"] = "کتابخانه مودها",
        ["ModLibraryPath"] = "پوشه مودها",
        ["Settings"] = "تنظیمات",
        ["ModLibrary"] = "کتابخانه مودها",
        ["AvailableMods"] = "مودهای موجود",
        ["InstalledMods"] = "مودهای نصب‌شده",
        ["Change"] = "تغییر",
        ["OpenFolder"] = "باز کردن پوشه",
        ["Browse"] = "انتخاب",
        ["Configured"] = "پیکربندی شد",
        ["NotConfigured"] = "پیکربندی نشده",
        ["NoModsInstalled"] = "هنوز هیچ مود ModLoader نصب نشده است.",
        ["NoModPackagesInLibrary"] = "هنوز هیچ بسته مودی در این کتابخانه وجود ندارد.",
        ["ViewReadme"] = "نمایش README",
        ["GameFolderNotConfiguredMessage"] = "لطفاً ابتدا یک پوشه معتبر برای GTA San Andreas انتخاب کنید.",
        ["SelectMod"] = "یک پوشه یا آرشیو مود انتخاب کنید",
        ["ExtractionFailed"] = "آرشیو مود را نمی‌توان استخراج کرد.",
        ["DuplicateModPrompt"] = "مدی با نام '{0}' از قبل وجود دارد. جایگزین شود؟",
        ["Installing"] = "در حال نصب",
        ["InstallationCompleted"] = "نصب با موفقیت انجام شد.",
        ["InstallationFailed"] = "نصب مود انجام نشد.",
        ["ModSourceInvalid"] = "مود انتخاب‌شده قابل خواندن نبود.",
        ["SelectGameFolder"] = "پوشه GTA San Andreas را انتخاب کنید",
        ["SelectModLibraryFolder"] = "پوشه کتابخانه مودها / منبع مودها را انتخاب کنید",
        ["InvalidGameFolder"] = "این پوشه شامل gta_sa.exe نمی‌شود.",
        ["InvalidModLibraryFolder"] = "این پوشه یک کتابخانه معتبر برای مودها نیست.",
        ["GameLaunchFailed"] = "اجرای بازی انجام نشد.",
        ["InstallingTitle"] = "در حال نصب",
        ["Theme"] = "تم",
        ["Language"] = "زبان",
        ["Installed"] = "نصب شده",
        ["Status"] = "وضعیت",
        ["Step1GameFolder"] = "پوشه بازی",
        ["Step2Profile"] = "پروفایل بازی",
        ["Step3Mod"] = "انتخاب مود",
        ["Step4Installing"] = "در حال نصب",
        ["Step5Category"] = "ماشین / اسلحه / اسکین",
        ["Step6Completed"] = "تکمیل شد",
        ["GameFolderRequired"] = "پوشه GTA San Andreas خود را انتخاب کنید.",
        ["ModFolder"] = "پوشه مود",
        ["BrowseModFolder"] = "انتخاب پوشه مود",
        ["DetectedMod"] = "مود تشخیص داده شد",
        ["CategoryPlaceholder"] = "این مرحله برای نصب‌کننده‌های پیشرفته آینده رزرو شده است.",
        ["InstallationComplete"] = "نصب کامل شد.",
        ["ReadMe"] = "خواندن راهنما",
        ["OpenReadmeFile"] = "باز کردن README.txt",
        ["Previous"] = "قبلی",
        ["Next"] = "بعدی",
        ["ShowMore"] = "نمایش بیشتر",
        ["ShowLess"] = "نمایش کمتر",
        ["Close"] = "بستن",
        ["Warning"] = "هشدار",
        ["Error"] = "خطا",
        ["Failed"] = "ناموفق",
        ["Cancel"] = "لغو",
        ["Continue"] = "ادامه",
        ["Back"] = "بازگشت",
        ["Completed"] = "تکمیل شد",
        ["CompletedIn"] = "تکمیل شد در",
        ["SelectModFolder"] = "پوشه مود را انتخاب کنید",
        ["ModJsonInvalid"] = "mod.json نامعتبر است یا محتوای غیرمنتظره دارد. نام مود همچنان از نام پوشه گرفته می‌شود.",
        ["SelectModFirst"] = "لطفاً ابتدا یک پوشه مود معتبر انتخاب کنید.",
        ["ModFolderEmpty"] = "پوشه مود انتخاب‌شده خالی یا غیرقابل خواندن است.",
        ["English"] = "English",
        ["Persian"] = "فارسی"
    };

    public string GetString(string key, string fallback = "")
    {
        var currentLanguage = ParseLanguage(CultureInfo.CurrentUICulture.Name);
        var dictionary = currentLanguage == SupportedLanguage.Persian ? PersianStrings : EnglishStrings;
        return dictionary.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    public string Get(string key, string fallback = "") => GetString(key, fallback);

    public SupportedLanguage ParseLanguage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return SupportedLanguage.English;
        }

        if (value.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, SupportedLanguage.Persian.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return SupportedLanguage.Persian;
        }

        return SupportedLanguage.English;
    }

    public CultureInfo GetCultureForLanguage(string language)
    {
        return ParseLanguage(language) switch
        {
            SupportedLanguage.Persian => new CultureInfo("fa-IR"),
            _ => new CultureInfo("en-US")
        };
    }
}
