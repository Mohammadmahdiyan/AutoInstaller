# پرامپت نهایی GtaSaModManager (تنها منبع؛ پرامپت‌های قبلی حذف شده‌اند)

هر باگ یک شناسه دارد (مثلاً `S5-2`) و فقط در یک فاز درست می‌شود. هر فاز را جدا به Copilot بده. بعد از هر فاز: build، تست، commit. تا فاز قبلی تأیید نشده فاز بعدی را نده.

---

## قوانین کلی (اول هر چت بچسبان)

1. فقط شناسه‌های فاز فعلی را انجام بده. اگر حین کار به مشکل یک شناسه‌ی فاز دیگر خوردی، درستش نکن. فقط شناسه‌اش را در انتهای گزارش بنویس.
2. قبل از تغییر، فایل‌ها و متدهایی که دست می‌زنی را لیست کن. بعد از تغییر، بگو دقیقاً چه چیزی و چرا عوض شد.
3. ریفکتور، rename، فرمت‌کردن دوباره‌ی فایل و جابه‌جایی کد ممنوع است. کامنت‌های `از اینجا / تا اینجا` را دست نزن.
4. نام‌های این پرامپت از سورس واقعی آمده. اگر چیزی با کد نمی‌خواند، متوقف شو و گزارش بده. حدس نزن.
5. بعد از تغییر `dotnet build` بزن. صفر Error و صفر Warning جدید.
6. اگر نمی‌توانی UI را اجرا کنی، صریح بنویس کدام موارد تست را فقط با خواندن کد تأیید کرده‌ای.
7. اگر برای یک شناسه دو تلاش پشت‌سرهم جواب نداد، متوقف شو و گزارش بده چه دیدی. سومین حدس را نزن.
8. پروژه WinForms است (`net8.0-windows`). `ViewModel` و `ModInstaller` وجود ندارد. منطق داخل `partial class MainForm` است.
9. هر بار که شناسه‌ای تمام شد، یک خط بنویس: `شناسه — انجام شد — فایل/متد تغییرکرده`.

## تصمیم‌های ثابت (اگر با نیت تو نمی‌خواند، اینجا اصلاح کن)

- **D1 فلو:**
  - VehicleAndSkinAndWeapon: Step 3 ← Step 5 (انتخاب هدف) ← Step 4 (پیشرفت) ← Step 6.
  - بقیه‌ی نوع‌ها: Step 3 ← Step 4 ← Step 6. هرگز Step 5 نمی‌آید.
  - Previous: Step 5 به Step 3 می‌رود. در Step 4 و 6 غیرفعال است.
- **D2 rename:** در VehicleAndSkinAndWeapon، dff و txd به `nameFile` asset انتخاب‌شده rename می‌شوند. در VehiclesAndSkinsAndWeapons همه‌ی جفت‌های dff/txd با اسم خودشان نصب می‌شوند و Step 5 نمی‌آید.
- **D3:** رفتار فعلی `putincleo` (نصب در ریشه‌ی بازی) دست نخورد.
- **D4:** `reqAddress` نسبت به Base Mods Folder حساب می‌شود (مثال: `Scripts\CLEO 4.1`).
- **D5:** فقط `deleteThis` و `deleteThese`. بدون `replaceWith`.

## محیط تست (یک‌بار، خارج از repo)

```
C:\ModManagerTest\
  Game\gta_sa.exe                            (خالی)
  Base\Skins\football-Ronaldu\   config.json {"type":"VehicleAndSkinAndWeapon"}
        hmybe.dff  hmybe.txd  Screenshot (95).png  Screenshot (96).png  (ساختگی)
  Base\Skins\unknown-model\      config.json (همان type)  zzzzzz.dff  zzzzzz.txd
  Base\Scripts\no-models\        config.json (همان type)  shot.png
  Base\Vehicles\Pack\            config.json {"type":"VehiclesAndSkinsAndWeapons"}
        infernus.dff infernus.txd banshee.dff banshee.txd preview.jpg Readme.txt
  Base\Scripts\SpeedoMod\        (بدون config)  hud.txd  sounds\bang.wav  shot.png  README.txt
  Base\Scripts\A1-MyReqFiles\    config.json {"type":"PutInGameFolder"}
        Essentials (Readme - EN).txt
        Essentials\CLEO\  Essentials\modloader\  Essentials\CLEO.asi  Essentials\modloader.asi
```
فقط اسم فایل‌ها مهم است. فایل‌های خالی کافی است.

---

# فاز ۱ — Step 5

**علت‌ها (از سورس):**
- `MainForm.Step5UI.cs → PrepareDetectedAssetStep` (خط ۱۷۴) اول `_selectedAssetForInstall = null` می‌کند و در خط ۲۲۶ همان را می‌خواند. پس شاخه‌ی «حفظ انتخاب» همیشه null است.
- `Step4InstallLogic.cs → InstallSelectedModAsync` (خطوط ۵۹۰ و ۶۰۶) قبل از نصب دوباره `PrepareDetectedAssetStep` را صدا می‌زند و انتخاب کاربر را می‌پراند.
- فلو اشتباه است. Step 3 برای VehicleAndSkinAndWeapon فوراً نصب می‌کند و Step 5 قبل از نصب دیده نمی‌شود. انتهای شاخه‌ی عمومی (خط ۶۶۸) هم به Step 5 می‌رود.

**S5-1 — فلو (D1).**
- `HandleSidebarNext` (Step 3): اگر `_selectedModManifest.IsSingleAssetPackage` بود، فقط `GoToStep(Step5)`. برای بقیه `InstallSelectedModAsync`.
- `HandleSidebarPrevious`: Step 5 به Step 3. در `UpdateSidebarState` مقدار Previous برای Step 6 `false` شود.
- فلگ `_returnedToInstallStepFromCompletion` با این فلو بی‌مصرف است. همه‌ی استفاده‌هایش را بردار.
- انتهای شاخه‌ی عمومی `InstallSelectedModAsync` به جای `GoToStep(Step5)` برود `GoToStep(Step6)`.

**S5-2 — حفظ انتخاب.**
- `PrepareDetectedAssetStep` انتخاب موجود را null نکند. فقط وقتی `_selectedAssetForInstall` null است پیش‌فرض بگذارد (asset هم‌نام مدل، وگرنه اولین).
- ریست کامل فقط وقتی مد جدید در Step 3 انتخاب می‌شود (`Step1To3.cs` خط ۲۰۴ همین را انجام می‌دهد).

**S5-3 — نصب بدون انتخاب بی‌صدا.**
- دو فراخوانی `PrepareDetectedAssetStep` در `InstallSelectedModAsync` حذف شوند.
- اگر برای VehicleAndSkinAndWeapon انتخابی نبود، پیام `NoAssetSelected` بده و نصب نکن.

**S5-4 — نوع نامشخص یا بدون مدل.**
- اگر نام مدل در کاتالوگ نبود، در Step 5 یک ComboBox با Vehicle | Skin | Weapon نشان بده. گالری فقط همان نوع را نشان دهد. دیگر همه‌ی assetها را نریز.
- اگر در payload اصلاً `.dff/.txd` نبود، در Step 3 جلوی Next را بگیر و پیام واضح بده.

**S5-5 — انتخاب کارت.**
- `ToggleSelection` فقط رنگ کارت‌ها و `UpdateSidebarState()` را عوض کند. `RefreshAssetStep()` از آن حذف شود.

**S5-6 — لک ریسورس و بازسازی اضافه.**
- `RefreshAssetStep` قبل از `gallery.Controls.Clear()` کنترل‌های قبلی را Dispose کند، به‌جز Imageهای `_assetImageCache`.
- `gallery.SizeChanged` فقط وقتی عرض ستون واقعاً عوض شد بازسازی کند.

**S5-7 — نمایش خطای کاتالوگ.**
- اگر `AssetCatalogService.ValidationError` خالی نبود، یک‌بار در Step 5 نشان بده.

**تست فاز ۱:**

| # | کار | نتیجه |
|---|---|---|
| T1 | `football-Ronaldu` ← Next | مستقیم Step 5. کارت `hmybe` انتخاب‌شده. نصبی انجام نشده. |
| T2 | کارت `male01` ← Install | Step 4 ← Step 6. `Game\modloader\football Ronaldu\male01.dff/.txd` ساخته شده. |
| T3 | `male01` ← Previous ← Next | انتخاب `male01` سر جایش است. |
| T4 | زبان را در Step 5 عوض کن | گالری و انتخاب می‌مانند. |
| T5 | `unknown-model` ← Next | ComboBox نوع دیده می‌شود. |
| T6 | `no-models` انتخاب کن | Next مسدود با پیام. |
| T7 | `SpeedoMod` نصب کن | Step 5 نمی‌آید. مستقیم Step 6. |
| T8 | گالری را ببین | Category فقط وقتی بیش از یک دسته هست. سایدبار: عکس مد، فلش، عکس asset. دکمه‌ی README فقط وقتی مد عکس ندارد. |

**توقف. گزارش بده و منتظر بمان.**

---

# فاز ۲ — قانون نصب بر اساس type

**علت‌ها:**
- ویزارد برای `replacing`، `putincleo`، `putingamefolder`، `putandreplace` و `putandreplaces` منطق مخصوص ندارد و همه به `modloader\<name>` می‌روند. منطق درستشان فقط در `InstallModButton_Click` (پنل مخفی) و `InstallReplacingPackageAsync` است.
- `ModPackageService.IsMetadataOrNonInstallableFile` همه‌ی عکس‌ها را برای همه‌ی نوع‌ها حذف می‌کند و README را با `StartsWith("README")` تشخیص می‌دهد.
- شاخه‌ی VehicleAndSkinAndWeapon در `InstallTypedPackageAsync` فقط dff/txd کپی می‌کند.
- Step 3 همیشه `_selectedModPayloadPath = selected` می‌گذارد، حتی برای نوع‌هایی که payload زیرپوشه است.
- `ModLoaderService.RecordPackageInstallation` و `RecordUserFilesInstallation` هیچ‌جا صدا زده نمی‌شوند. پس برای Uninstall (فاز ۴) داده‌ای نیست.

**قانون نهایی:**

| type | payload | عکس؟ | مقصد |
|---|---|---|---|
| `putinmodloader` (و بدون config) | خود پوشه‌ی مد | بله | `game\modloader\<نام مد>\` |
| `vehicleandskinandweapon` | خود پوشه‌ی مد | بله | `game\modloader\<نام>\` با rename (D2) |
| `vehiclesandskinsandweapons` | خود پوشه‌ی مد | بله | `game\modloader\<نام>\` با اسم‌های خودشان (D2) |
| `putingamefolder`، `putincleo`، `putandreplace`، `putandreplaces`، `replacing` | `GetPayloadDirectory` (زیرپوشه) | نه | منطق فعلی مقصد و بکاپ |
| `savesandmissions`، `missiondsl` | بدون تغییر | نه | بدون تغییر |

قوانین مشترک: `config.json` و `mod.json` هرگز نصب نشوند. README هرگز نصب نشود؛ فایلی README است که اسمش (بعد از حذف فاصله و `_` و `-`) شامل `readme` باشد (همان `IsReadmeFileName` در `MainForm.Readme.cs`). ساختار زیرپوشه‌ها حفظ شود. اسم مد از `ResolveDirectoryName` می‌آید (`football-Ronaldu` می‌شود `football Ronaldu`).

**I-1 — برنامه‌ی نصب.** یک متد خالص بساز، مثلاً `ModPackageService.BuildInstallPlan(manifest, packageRoot, gameFolder, modName, selectedAsset)` که فقط لیست `(sourcePath, destinationPath)` برمی‌گرداند و چیزی کپی نمی‌کند.

**I-2 — dispatcher.** `InstallSelectedModAsync` بر اساس `NormalizedType` طبق جدول بالا به I-1 برود. منطق قدیمی `InstallModButton_Click` را به همین وصل کن. پنل مخفی را دست نزن.

**I-3 — payload در Step 3.** برای نوع‌های payload-زیرپوشه‌ای، `_selectedModPayloadPath = GetPayloadDirectory(selected)` و `_selectedModPackageRoot = selected`.

**I-4 — عکس و README.** کپی عکس فقط برای سه نوع بالای جدول. فیلتر README را به قانون «شامل readme» تغییر بده. برای VehiclesAndSkinsAndWeapons همه‌ی جفت‌های dff/txd را نصب کن (فقط اگر `nameFile` در کاتالوگ باشد). منطق بکاپ فعلی (`BackupStorageService.CreatePlan`) را تغییر نده.

**I-5 — ثبت نصب برای همه‌ی نوع‌ها.**
- بعد از هر نصب موفق، `RecordPackageInstallation` را با لیست فایل‌های واقعاً نصب‌شده صدا بزن.
- `RecordPackageInstallation` مسیر manifest بازی را از `Path.GetDirectoryName(installedDestination)` حدس می‌زند. برای نصب در ریشه‌ی بازی این پوشه‌ی والد را می‌دهد و غلط است. یک پارامتر صریح `gamePath` اضافه کن.
- برای `savesandmissions` و `missiondsl` از `RecordUserFilesInstallation` استفاده کن.

**مثال‌های قبول:**
1. `football-Ronaldu` با هدف `hmybe` ← `modloader\football Ronaldu\` = دو Screenshot + `hmybe.dff` + `hmybe.txd`. بدون config.
2. همان مد با هدف `male01` ← `male01.dff`، `male01.txd` + Screenshotها با اسم اصلی.
3. `Pack\` ← `modloader\Pack\` = `infernus.*`، `banshee.*`، `preview.jpg`. بدون README و config. Step 5 نمی‌آید.
4. `SpeedoMod\` (PutInModloader) ← `modloader\SpeedoMod\` = `hud.txd`، `sounds\bang.wav`، `shot.png`. بدون README.
5. `A1-MyReqFiles\` (PutInGameFolder) ← محتویات `Essentials` مستقیم در پوشه‌ی بازی (`CLEO\`، `modloader\`، `CLEO.asi`، `modloader.asi`). خود پوشه‌ی `Essentials`، README و config کپی نمی‌شوند.
6. `Essentials\` با `CLEO\plugin.cs` و `shot.png` و `Mod (Readme - EN).txt` ← فقط `CLEO\plugin.cs` در بازی.

**تست:** برای هر ۶ مثال خروجی `BuildInstallPlan` را (بدون UI) چاپ کن یا با یک تست ساده نشان بده. بعد یک بار نصب واقعی مثال ۱ و ۵ را در `C:\ModManagerTest\Game` انجام بده. فایل `installations.json` باید ساخته شده و فایل‌های نصب‌شده را داشته باشد.

**توقف.**

---

# فاز ۳ — زبان، تنظیمات و ورودی پوشه

**علت‌ها:**
- پنل‌های ویزارد متنشان را فقط موقع ساخت از `GetString` می‌گیرند. `RefreshLocalizedTextForControl` فقط چند کنترل با اسم خاص را به‌روز می‌کند. `RebuildWizardPanels` هست ولی هیچ‌جا صدا زده نمی‌شود.
- `LocalizationService.EnglishStrings` چند کلید با متن فارسی دارد: `Completed`، `CompletedIn`، `SelectModFolder`، `ModJsonInvalid`، `SelectModFirst`، `ModFolderEmpty`.
- `ApplyComboSelectionSafely` (در `MainForm.Theme.cs`) در `finally` هر دو handler زبان و تم را روی هر ComboBox وصل می‌کند.
- `SynchronizeStepInputs` فقط فرزندان مستقیم پنل را می‌بیند و عملاً کاری نمی‌کند.
- `PromptForModFolderSelection` (`MainForm.ModLibrary.cs` خط ۸۳) از `Path.GetDirectoryName(FileName)` استفاده می‌کند.

**L-1 — زبان (یک‌بار برای همه‌ی متن‌ها).**
- بعد از تغییر زبان `RebuildWizardPanels()` را صدا بزن.
- بعد از rebuild وضعیت این‌ها را از فیلدها برگردان: Step 3 (`folderText`، `selectedName`، گالری با `RefreshStep3Images`)، Step 4، Step 5 (انتخاب و فیلتر) و Step 6 (شمارش معکوس نباید ریست شود).
- کلیدهای فارسی داخل `EnglishStrings` را انگلیسی کن و فارسی‌شان را در `PersianStrings` بگذار. لیست کلیدها را گزارش بده.

**L-2 — handlerهای زبان و تم.** هر ComboBox فقط handler خودش را جدا و دوباره وصل کند.

**L-3 — `SynchronizeStepInputs`.** حذف شود. پنل‌ها همیشه از `_settings` ساخته می‌شوند. همه‌ی فراخوانی‌ها را هم بردار.

**L-4 — انتخاب پوشه‌ی مد.** ابتدا تست کن: پوشه‌ای را که فقط زیرپوشه دارد از دیالوگ انتخاب کن. اگر `GetDirectoryName` پوشه‌ی والد برگرداند، `PromptForModFolderSelection` را با `FolderBrowserDialog` یا `IFileDialog` با `FOS_PICKFOLDERS` جایگزین کن. اگر درست بود، دست نزن و نتیجه‌ی تست را بنویس.

**تست فاز ۳:**
- انگلیسی ← فارسی ← انگلیسی ← فارسی. عنوان همه‌ی Stepها هر بار عوض می‌شود.
- با زبان فارسی، تم را عوض کن. زبان نباید عوض شود. `settings.json` (`%LOCALAPPDATA%\GtaSaModManager`) باید `Language` و `Theme` درست داشته باشد.
- در Step 3 و Step 5 زبان را عوض کن. انتخاب‌ها می‌مانند.
- در Step 3 پوشه‌ای انتخاب کن که فقط زیرپوشه دارد. همان پوشه انتخاب شود، نه والدش.

**توقف.**

---

# فاز ۴ — قابلیت‌های نیمه‌کاره

قبل از فاز ۴ باید فاز ۲ (dispatcher و ثبت نصب) انجام و تأیید شده باشد.

**F-1 — `deleteThis` / `deleteThese`.**
- **وضعیت فعلی:** فقط `conflictCleanup` در `ModManifest` parse می‌شود و هیچ‌جا اجرا نمی‌شود.
- **فرمت:**
  ```json
  { "deleteThis": { "file": "cleo.asi", "files": ["a.asi"], "folder": "SomeOldFolder", "folders": ["x","y"] } }
  ```
  همه‌ی کلیدها اختیاری. `deleteThese` هم همان معنی را دارد. کلید قدیمی `conflictCleanup` را هم بخوان ولی `replaceWith` را نادیده بگیر.
- **مسیرها:** نسبت به پوشه‌ی بازی. با `GetSafeGamePath` بررسی شود که از پوشه‌ی بازی بیرون نروند. خود ریشه‌ی بازی هرگز حذف نشود.
- **ایمنی:** قبل از حذف یک MessageBox با لیست موارد و دکمه‌ی Yes/No نشان بده.
- **ترتیب کلی نصب:** `deleteThis` ← `require/requires` ← وابستگی‌های عمومی (`EnsureDependenciesBeforeInstallAsync`) ← نصب مد. دلیل: وابستگی عمومی CLEO/modloader نباید چیزی را که `deleteThis` پاک کرده دوباره نصب کند.

**F-2 — `require` / `requires`.**
- **فرمت:**
  ```json
  { "require": { "checkFiles": ["cleo.asi","modloader.asi"], "checkFolders": ["cleo","modloader"], "reqAddress": "Scripts\\A1-MyReqFiles" } }
  ```
  چندتا: `requires` آرایه‌ای از همان ابجکت‌ها. فرم تکی (`checkFile` و `checkFolder` رشته‌ای) هم باید پشتیبانی شود. Parse فعلی در `ModPackageService.TryReadManifest` درست است و تغییر نمی‌خواهد.
- **باگ ۱ (شرط):** `RequirementEntryIsSatisfied` هم فایل‌ها هم پوشه‌ها را با AND چک می‌کند. قانون درست: اگر همه‌ی `checkFiles` هستند، نصب‌شده حساب می‌شود؛ وگرنه اگر همه‌ی `checkFolders` هستند، نصب‌شده حساب می‌شود؛ وگرنه باید نصب شود. بعد از نصب هم همین قانون را برای تأیید استفاده کن.
- **باگ ۲ (مقصد):** `EnsureRequiredPackagesBeforeInstallAsync` پیش‌نیاز را همیشه به `modloader\<name>` کپی می‌کند. باید پیش‌نیاز را با dispatcher فاز ۲ و `config.json` خودش نصب کند. مثلاً CLEO با PutInGameFolder باید در ریشه‌ی بازی برود.
- اگر `reqAddress` پیدا نشد، پیام واضح بده و نصب نکن (رفتار فعلی را نگه دار).

**F-3 — نصب Optional در Step 6.** موارد فعلی: `GetOptionalPackageRootsForCurrentInstall`، `RefreshStep6OptionalActions` و تایمر ۱۵/۶ ثانیه هستند و درستند. این کمبودها را کامل کن:
- `InstallOptionalPackageAsync` بعد از نصب نباید Step 6 را ریست یا دوباره بسازد. الان `InstallSelectedModAsync` با `GoToStep(Step6)` شمارش و لیست را ریست می‌کند. یک پارامتر یا فلگ اضافه کن که در این حالت ناوبری نکند.
- state فیلدهای `_selectedMod*` را بعد از نصب optional به مقدار قبل برگردان.
- حالت `optional` (تکی): دکمه‌ی «Install optional mod» با تولتیپ اسم پوشه. کلیک آن را مثل انتخاب در Step 3 با dispatcher نصب می‌کند.
- حالت `optionals` (چندتا): گالری عکس Step 6 برداشته شود. زیر دکمه‌های Open game folder و Run game، لیست اسکرولی از چک‌باکس + اسم بیاید. هاور روی ردیف: اگر عکس داشت پنجره‌ی ویوئر (`OpenFullImageViewer`)، اگر فقط README داشت متنش، وگرنه هیچ. کلیک روی ردیف نصب می‌کند و بعد آن ردیف تیک می‌خورد و لیست می‌ماند.
- اگر پکیج optional از نوع VehicleAndSkinAndWeapon بود (نیاز به Step 5)، پیام «پشتیبانی نمی‌شود» بده و نصب نکن.

**F-4 — Uninstall.**
- **وضعیت فعلی:** دکمه‌ی Uninstall فقط روی پنل مخفی `ModLoaderPanel` است و `modType` را ثابت `"putinmodloader"` می‌دهد. `TryUninstallByModId`، `TryUninstallUserFilesInstall` و `IsInstalled` هیچ‌جا صدا زده نمی‌شوند.
- **داده:** فقط از رکوردهای فاز ۲ (`installations.json` و `RecordUserFilesInstallation`) و `ReplaceInstallationRecord` (`replacement-install-history.json`) استفاده کن. چیزی حدس نزن.
- **در Step 3:** وقتی مد انتخاب‌شده از قبل نصب شده، به‌جای پیام Yes/No فعلی (`DuplicateModPrompt`) سه گزینه بده: نصب/جایگزین کن، Uninstall، و (فقط برای VehicleAndSkinAndWeapon و VehiclesAndSkinsAndWeapons) تغییر asset. تغییر asset به Step 5 می‌رود و متن دکمه‌ی Next در سایدبار «Change» می‌شود. بعد از انتخاب: uninstall قبلی، نصب جدید، Step 6.
- **جدول Uninstall:**

| type | عملیات |
|---|---|
| `putinmodloader`، `vehicleandskinandweapon`، `vehiclesandskinsandweapons` | پوشه‌ی `modloader\<name>` حذف شود |
| `putincleo`، `putingamefolder` | فقط فایل‌های ثبت‌شده حذف شوند. پوشه‌های خالی‌شده هم پاک شوند |
| `putandreplace`، `putandreplaces`، `replacing` | فایل‌های نصب‌شده حذف شوند. اصل فایل‌ها از `BackupFilePath` به `OriginalFilePath` برگردند. زیرپوشه‌ی بکاپ همان مد حذف شود |
| `savesandmissions` | فایل‌های ثبت‌شده در User Files حذف شوند (`TryUninstallUserFilesInstall`) |
| `missiondsl` | فایل‌های ثبت‌شده‌ی DSL حذف شوند |

- ساختار بکاپ باید `[ID]\[نام مد]\[مسیر نسبی فایل]` باشد. `BackupOriginalFileForReplacement` این را می‌سازد. فقط تأیید کن.
- **لیست مدهای نصب‌شده:** یک دکمه‌ی «Installed mods» در سایدبار بگذار که یک پنجره‌ی ساده باز کند. کدهای کارت‌های `RefreshModListAsync` را دوباره استفاده کن. هر مد یک دکمه‌ی سطل‌آشغال دارد که uninstall را طبق جدول بالا اجرا می‌کند. از پنل مخفی استفاده نکن.

**تست فاز ۴:**

| # | کار | نتیجه |
|---|---|---|
| T1 | مدی با `deleteThis: {file:"cleo.asi"}` و `require` برای CLEO | اول تأیید حذف، بعد نصب پیش‌نیاز از config خودش، بعد نصب مد |
| T2 | `require` با `checkFile` موجود ولی `checkFolder` ناموجود | پیش‌نیاز نصب نمی‌شود (قانون OR) |
| T3 | مدی با `optional\` تکی | دکمه‌ی Install optional mod. کلیک ← نصب، Step 6 ریست نمی‌شود |
| T4 | مدی با `optionals\` و ۳ زیرپوشه | لیست چک‌باکس‌دار. نصب یکی ← تیک می‌خورد و لیست می‌ماند |
| T5 | مد نصب‌شده را دوباره در Step 3 انتخاب کن | سه گزینه. Uninstall فایل‌ها را حذف می‌کند |
| T6 | Uninstall یک مد PutAndReplace | فایل اصلی برگردانده می‌شود |
| T7 | دکمه‌ی Installed mods | لیست کارت‌ها و حذف با سطل‌آشغال |

---

## این‌ها را دست نزن (جای دیگر رسیدگی نمی‌شوند)

- `Form1.cs`، `Step5Logic.cs`، `.resx`ها، `tmp_dedupe_assets.py`، پنل‌های مخفی designer (`GamePanel`، `ModLibraryPanel`، `ModLoaderPanel`) و handlerهای قدیمی‌شان.
- مسیرهای ثابت بکاپ: `ModPackageService.GetDefaultBackupRoot` و `BackupStorageService.ExternalBackupBase`.

## این موارد ظاهراً انجام شده‌اند (فقط در تست نهایی نگاه کن؛ اگر خراب بودند گزارش بده)

- سایدبار Step 3 (README و گالری عکس)
- حذف پیام‌های «نصب موفق» (در کد `MessageBox`ی با این متن نیست)
- `InstallProgressPanel` داخل Step 4 (نه Form جدا)
- لیست فایل‌ها با درصد در Step 4
- راست‌کلیک روی عکس‌ها و باز شدن ویوئر
- ناپدید شدن پیام Detected mod بعد از ۳ ثانیه

## نقشه‌ی بخش‌های پرامپت قدیمی به شناسه‌ها

| بخش قدیمی | شناسه |
|---|---|
| ۱، ۴، ۵، ۶، ۷، ۸ | انجام‌شده (تست نهایی) |
| ۳ (زبان) | L-1، L-2 |
| ۹ (Category)، ۱۰، ۱۱، ۱۲ (گرید Step 5) | S5-2، S5-4، S5-5، S5-6 و تست T8 |
| ۱۳ (Step 5 به Step 6) | S5-1، S5-3 |
| ۱۴ (Uninstall) | I-5، F-4 |
| ۱۵ (Optional) | F-3 |
| ۱۶ (`require`) | F-2 |
| ۱۷ (`deleteThis`) | F-1 |
| کشف‌شده‌ها: type/عکس/README | I-1 تا I-5 |
| کشف‌شده‌ها: انتخاب پوشه، handlerها | L-2، L-3، L-4 |
