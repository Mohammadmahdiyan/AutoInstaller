# کلیدهای پیکربندی

مانیفست یک شیء JSON است. نام کلید `type` و کلیدهای `InstallFile(s)`، `InstallFolder(s)`، `IgnoreFile(s)` و `IgnoreFolder(s)` بدون حساسیت به بزرگی و کوچکی حروف خوانده می‌شود. برای فیلدهای قدیمی `require`، `requires`، `conflictCleanup`، `deleteThis` و `replacements`، دقیقاً از شکل camelCase زیر استفاده کنید؛ تجزیه‌گر فعلی همین نام‌ها را می‌خواند.

## نوع بسته

مقادیر نوعی که اعتبارسنج پذیرفته است:

- `PutInModLoader` (`PIM`)
- `Replacing` (`RIP`)
- `PutInCleo` (`PIC`)
- `PutInGameFolder` (`PGF`)
- `PutAndReplace` (`PAR`)
- `PutAndReplaces` (`PRS`)
- `VehicleAndSkinAndWeapon` (`VSW` / `VSS`)
- `VehiclesAndSkinsAndWeapons`
- `SavesAndMissions` (`SAM`)
- `MissionDsl` (`DSL`)

مقایسهٔ نوع، فاصله و نویسه‌های `_` و `-` را حذف می‌کند و به بزرگی/کوچکی حروف حساس نیست. نکته: پیاده‌سازی فعلی `NormalizedType` هر دو املای نوع وسیله/شخصیت/اسلحه را به مقدار نوع تک‌مدلی تبدیل می‌کند؛ فرض نکنید که فعلاً دو جریان جدا دارند.

## پیش‌نیازها

`require` یک شیء می‌پذیرد؛ `requires` یک شیء یا آرایه‌ای از اشیا می‌پذیرد. فیلدهای پیش‌نیاز شامل `checkFile`، `checkFolder`، `checkFiles`، `checkFolders` و `reqAddress` هستند. `reqPath` نیز نام جایگزین `reqAddress` است. مسیرها در پوشهٔ بازی بررسی می‌شوند، مگر آنکه مطلق باشند.

```json
{
  "type": "PutInModLoader",
  "require": { "checkFile": "cleo.asi", "reqAddress": "Scripts/CLEO" },
  "requires": [{ "checkFolders": ["modloader"] }]
}
```

## پاک‌سازی و جایگزینی

`deleteThis` یک رشته یا آرایه‌ای از فایل‌ها/پوشه‌های نسبی به بازی می‌پذیرد تا پیش از نصب حذف شوند. مانیفست `conflictCleanup` با فیلدهای `file`، `folder`، `files`، `folders`، `replaceWith` و `replacesWith` تجزیه می‌شود؛ مسیرهای نصب فعلی این شیء را خودکار اجرا نمی‌کنند. نوع `PutAndReplace` یک آرایهٔ `replacements` می‌پذیرد که اعضایش مسیر رشته‌ای یا جفت `source`/`target` هستند.

## انتخاب فایل و پوشه

چهار کلید انتخاب، رشته یا آرایه‌ای از مسیرهای نسبی می‌پذیرند. شکل مفرد و جمع با هم ادغام می‌شوند. `/` به `\\` تبدیل می‌شود و مسیر خارج از ریشهٔ بسته رد می‌شود.

- `InstallFile` / `InstallFiles`: فهرست مجاز فایل‌ها؛ اگر فایل در زیرپوشه باشد، مقصد فقط نام فایل است.
- `InstallFolder` / `InstallFolders`: فهرست مجاز پوشه‌ها؛ محتوا بازگشتی نصب می‌شود و ساختار پوشه حفظ می‌شود.
- `IgnoreFile` / `IgnoreFiles`: با نام فایل در هر سطح تطبیق می‌کند؛ اگر مسیر داده شود، مسیر نسبی دقیق را حذف می‌کند.
- `IgnoreFolder` / `IgnoreFolders`: پوشه و همهٔ زیرمجموعه‌های آن را حذف می‌کند.

این فهرست‌ها در حال حاضر توسط نصب‌کنندهٔ CLEO مصرف می‌شوند. اگر `InstallFiles` مشخص نباشد، فایل‌های سطح ریشه انتخاب می‌شوند. اگر `InstallFolders` مشخص نباشد، زیرپوشه‌ها انتخاب نمی‌شوند، مگر اینکه حالت `IgnoreFolders` همهٔ پوشه‌های سطح اول را انتخاب کند.

```json
{
  "type": "PutInCleo",
  "InstallFiles": ["logs/memory2026.cs", "RZL Trainer.cs"],
  "IgnoreFile": "crash.log"
}
```
