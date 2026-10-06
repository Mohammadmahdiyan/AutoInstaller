# انواع پیکربندی (Configuration Types)

این راهنما مقادیر `type` مربوط به پکیج را که در حال حاضر توسط **GTA San Andreas Mod Manager** شناسایی می‌شوند، توضیح می‌دهد.

نوع پکیج باید در سطح بالای فایل JSON مربوط به Manifest قرار بگیرد. نام فایل Manifest می‌تواند یکی از موارد زیر باشد:

- `mod.json`
- `config.json`
- `<نام پوشه پکیج>.json`

بررسی فایل‌ها نیز به همین ترتیب انجام می‌شود.

نام ویژگی `type` و مقدار آن **به حروف بزرگ و کوچک حساس نیستند**. Parser ابتدا مقدار را `trim` می‌کند، سپس فاصله‌ها، خط تیره (`-`) و زیرخط (`_`) را حذف کرده و در نهایت نام‌های پشتیبانی‌شده و نام‌های مستعار آن‌ها را استاندارد می‌کند.

برای معتبر بودن یک Manifest غیرخالی، وجود یک مقدار رشته‌ای (`string`) برای `type` الزامی است.

---

## PutInModLoader

فایل‌های انتخاب‌شده را در مسیر زیر نصب می‌کند:

```text
<پوشه بازی>\modloader\<نام مود>\
```

در پکیج‌های معمولی، Payload اولین زیرپوشه مجاز در سطح اصلی پکیج است که به ترتیب حروف الفبا انتخاب می‌شود.

اگر هیچ زیرپوشه مجازی وجود نداشته باشد، فایل‌های موجود در ریشه پکیج به‌عنوان Payload در نظر گرفته می‌شوند.

فایل Manifest، فایل README و فایل‌های رسانه‌ای پشتیبانی‌شده به‌عنوان فایل‌های معمولی نصب کپی نمی‌شوند.

**مقادیر قابل قبول:**

`PutInModLoader`، `ModLoader`، `PIM`

```json
{
  "type": "PutInModLoader"
}
```

### مثال پکیج:

```text
RZL Trainer/
    config.json
    RZL Trainer.cs
    preview.png
```

اسکریپت نصب‌شده در مسیر زیر قرار می‌گیرد:

```text
<game>\modloader\RZL Trainer\
```

---

## Replacing

فایل‌های Payload را در مسیرهای نسبی متناظر داخل پوشه بازی کپی کرده و فایل‌های موجود را جایگزین می‌کند.

در صورتی که امکان تهیه Backup وجود داشته باشد و کاربر آن را تأیید کرده باشد، فایل‌های اصلی قبل از جایگزینی پشتیبان‌گیری می‌شوند.

این مسیر اختصاصی برای جایگزینی توسط گزینه قدیمی **Install Mod** اجرا می‌شود؛ در حال حاضر Wizard مرحله 3 شاخه جداگانه‌ای برای `Replacing` ندارد.

**مقادیر قابل قبول:**

`Replacing`، `RIP`

```json
{
  "type": "Replacing"
}
```

برای مثال، اگر Payload شامل فایل زیر باشد:

```text
data\handling.cfg
```

مقصد آن در بازی خواهد بود:

```text
<game>\data\handling.cfg
```

---

## PutInCleo

فایل‌های انتخاب‌شده را در مسیر زیر نصب می‌کند:

```text
<game>\cleo\
```

برای انتخاب فایل‌ها از قوانین `InstallFiles`، `InstallFolders`، `IgnoreFiles` و `IgnoreFolders` که در `07-ConfigKeys.md` توضیح داده شده‌اند استفاده می‌کند.

اگر لیست‌های نصب مشخص نشده باشند، فایل‌های موجود در ریشه پکیج انتخاب می‌شوند.

زیرپوشه‌ها تنها زمانی نصب می‌شوند که انتخاب پوشه‌ها فعال شده باشد.

**مقادیر قابل قبول:**

`PutInCleo`، `PIC`

```json
{
  "type": "PutInCleo",
  "InstallFiles": ["ragdoll.asi", "Ragdoll.json"]
}
```

---

## PutInGameFolder

محتویات Payload را با حفظ مسیرهای نسبی، مستقیماً داخل پوشه بازی کپی می‌کند.

در صورت وجود فایل مقصد، در شرایطی که Backup قابل انجام باشد، فایل موجود از طریق سیستم پشتیبان‌گیری جایگزینی Backup می‌شود.

**مقادیر قابل قبول:**

`PutInGameFolder`، `PGF`

```json
{
  "type": "PutInGameFolder"
}
```

برای مثال، اگر Payload شامل این دو فایل باشد:

```text
ragdoll.asi
ragdoll.bmp
```

هر دو فایل مستقیماً در مسیر زیر قرار می‌گیرند:

```text
<game>\
```

---

## PutAndReplace

از یک آرایه الزامی به نام `replacements` استفاده می‌کند تا هر فایل موجود در Payload را به یک مسیر مقصد نسبت به پوشه بازی متصل کند.

فایل اصلی موجود در مقصد، در صورت امکان، از طریق سیستم Backup مربوط به جایگزینی پشتیبان‌گیری می‌شود.

فرمت‌های پشتیبانی‌شده آرایه در `07-ConfigKeys.md` توضیح داده شده‌اند.

**مقادیر قابل قبول:**

`PutAndReplace`، `PAR`

```json
{
  "type": "PutAndReplace",
  "replacements": [
    {
      "source": "handling.cfg",
      "target": "data/handling.cfg"
    }
  ]
}
```

---

## PutAndReplaces

فایل‌های Payload را در همان مسیرهای نسبی خود در پوشه بازی کپی می‌کند و در صورت وجود فایل مقصد، از طریق سیستم Backup جایگزینی از فایل موجود پشتیبان می‌گیرد.

برخلاف `PutAndReplace`، برای هر فایل یک نگاشت جداگانه `source → target` تعریف نمی‌کند.

**مقادیر قابل قبول:**

`PutAndReplaces`، `PRS`

```json
{
  "type": "PutAndReplaces"
}
```

برای مثال، اگر Payload شامل این فایل باشد:

```text
data\handling.cfg
```

مقصد آن خواهد بود:

```text
<game>\data\handling.cfg
```

---

## VehicleAndSkinAndWeapon (یک Asset)

از روند انتخاب Asset در **Step 3 / Step 5** برای یک Source Model استفاده می‌کند.

نام پایه (`basename`) فایل‌های `.dff` و `.txd` با Asset Catalog مقایسه می‌شود تا مشخص شود فایل مربوط به کدام وسیله نقلیه، Skin یا Weapon است.

نام `NameFile` مربوط به هدف انتخاب‌شده برای نام فایل مدل نصب‌شده در پکیج ModLoader استفاده می‌شود.

**مقادیر قابل قبول:**

`VehicleAndSkinAndWeapon`، `VSW`، `VSS`

همچنین نام قدیمی:

`VehicleAndSkinsAndWeapons`

نیز به همین نوع **تک‌Asset** تبدیل می‌شود؛ توجه کنید که در این نام، بعد از `Vehicle` حرف `s` وجود دارد.

```json
{
  "type": "VehicleAndSkinAndWeapon"
}
```

اگر مثلاً `xaa.dff` در هیچ‌یک از Catalogها پیدا نشود، Step 5 از کاربر می‌پرسد که این فایل مربوط به **Vehicle، Skin یا Weapon** است و سپس Assetهای همان نوع را نمایش می‌دهد.

---

## VehiclesAndSkinsAndWeapons (چند Asset)

پکیج را برای یافتن فایل‌های `.dff` و `.txd` بررسی می‌کند.

سپس فایل‌ها را بر اساس **نام پایه یکسان** گروه‌بندی می‌کند و برای هر مدل، Step 5 را یک‌بار نمایش می‌دهد.

هر مدل شناخته‌شده ابتدا نوع Asset شناسایی‌شده خود را دریافت می‌کند.

اگر نام مدل ناشناخته باشد، از کاربر خواسته می‌شود نوع آن را انتخاب کند.

مدل‌هایی که نگاشت داده شده‌اند و نگه داشته می‌شوند، با نام هدف انتخاب‌شده یا نام پایه اصلی، در پوشه پکیج ModLoader نصب می‌شوند.

**مقدار قابل قبول:**

`VehiclesAndSkinsAndWeapons`

> توجه: این نوع را با `VehicleAndSkinsAndWeapons` که نام مستعار نوع تک‌Asset است اشتباه نگیرید.

```json
{
  "type": "VehiclesAndSkinsAndWeapons"
}
```

برای مثال:

```text
infernus.dff
infernus.txd
```

یک Source Model محسوب می‌شوند.

اما اگر نام TXD متفاوت باشد، با آن DFF جفت نمی‌شود.

---

## SavesAndMissions

فایل‌های Save Slot با نام زیر را شناسایی می‌کند:

```text
GTASAsf<number>.b
```

این فایل‌ها را در **اولین Slot آزاد بعدی** در مسیر زیر نصب می‌کند:

```text
Documents\GTA San Andreas User Files
```

اگر لازم باشد از یک Slot موجود مجدداً استفاده شود، فایل قبلی به پوشه مخفی زیر منتقل می‌شود:

```text
Documents\GTA San Andreas User Files\.trash
```

اگر پکیج شامل فایلی با نام زیر باشد:

```text
DYOM<number>.dat
```

پکیج به‌عنوان یک پکیج **DYOM** در نظر گرفته می‌شود و همچنین وجود Dependency مربوط به DYOM در پوشه **Base Mods** الزامی خواهد بود.

**مقادیر قابل قبول:**

`SavesAndMissions`، `SAM`

```json
{
  "type": "SavesAndMissions"
}
```

---

## MissionDsl

اگر پوشه `DSL` در مسیر زیر وجود نداشته باشد، ابتدا Dependency نسخه **DYOM v8.1** را از مسیر پوشه Base Mods نصب می‌کند:

```text
Base Mods\Scripts\DYOM\DYOM v8.1
```

مقصد نصب Dependency:

```text
Documents\GTA San Andreas User Files\DYOM v8.1
```

اگر منبع Dependency پیدا نشود، نصب با هشدار متوقف می‌شود. سپس کل پوشه `DSL` در `Documents\GTA San Andreas User Files` جایگزین می‌شود و محتویات پکیج در آن کپی می‌شود.

فایل‌های Metadata، README و فایل‌های رسانه‌ای از این عملیات کپی مستثنی هستند.

وجود Dependency مربوط به DYOM در پوشه **Base Mods** الزامی است.

**مقادیر قابل قبول:**

`MissionDsl`، `DSL`

```json
{
  "type": "MissionDsl"
}
```

---

## Mixed (MIX)

نوع `Mixed` دو یا چند پوشهٔ دارای نوع نصب متفاوت را از ریشهٔ پکیج، به‌ترتیب فهرست نصب می‌کند. هر `folderName` باید یک مسیر نسبی و پوشهٔ موجود باشد. اگر یک بخش نیاز به انتخاب Asset داشته باشد، ویزارد برای انتخاب به Step 5 می‌رود و فقط پس از پایان آخرین بخش به Step 6 می‌رسد.

```modsyn
mod {
  type: MIX
  list: [
    { type: VSW folderName: "gta3img" }
    { type: Replacing folderName: "animations" backup: none }
  ]
}
```

هر بخش از نوع‌های نصب موجود به‌جز `Mixed` تو‌در‌تو پشتیبانی می‌کند. تنظیمات backup و `replacements` داخل همان object قرار می‌گیرند و فقط روی همان بخش اعمال می‌شوند. اگر یک Mixed نصب‌شده دوباره انتخاب شود، امکان حذف بخش‌ها در Step 5 نمایش داده می‌شود؛ دکمهٔ حذف Step 1 نیز بخش‌ها را جداگانه فهرست می‌کند.

# نکات مهم درباره رفتار سیستم

- اعتبارسنجی `type` در حال حاضر، پس از استانداردسازی، این مقادیر را می‌پذیرد:

```text
putinmodloader
replacing
putincleo
putingamefolder
putandreplace
putandreplaces
vehicleandskinandweapon
vehiclesandskinsandweapons
savesandmissions
missiondsl
mixed
```

- مقدار `VehicleAndSkinsAndWeapons` به نوع **تک‌Asset** تبدیل می‌شود، زیرا فاقد `Vehicles` به‌صورت جمع است.

- در مقابل، `VehiclesAndSkinsAndWeapons` یک نوع مستقل برای **چند Asset** باقی می‌ماند.

- اگر `type` وجود نداشته باشد یا خالی باشد، هنگام ساخته‌شدن `ModManifest` مقدار پیش‌فرض آن:

```text
putinmodloader
```

خواهد بود.

- با این حال، برای عبور از اعتبارسنجی، یک Manifest غیرخالی **حتماً باید دارای یک `type` از نوع string باشد**.

- مقدار `conflictCleanup` توسط سیستم خوانده و ذخیره می‌شود، اما در حال حاضر فرآیندهای نصب، دستورهای Cleanup موجود در آن را **به‌صورت خودکار اجرا نمی‌کنند**.
