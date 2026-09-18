این نسخه با همان ساختار قبلی، ولی با سه اصلاحی که گفتی:

* فقط **یک `Assets\Assets.json` داخل خود پروژه** داریم و Modها `assets.json` ندارند.
* `PutAndReplace` هر دو فرمت `string` و `object` را قبول می‌کند.
* `SavesAndMissions` فقط `type` دارد و اسم Save از **پوشه Mod/Save** گرفته می‌شود.

---

# 1. Startup و Step 1 / Step 2

### 1.1. Step 1 — Game Folder

Step 1 برای انتخاب **پوشه اصلی GTA San Andreas** است؛ یعنی همان پوشه‌ای که مدها قرار است داخل آن نصب شوند.

برنامه باید Game Folder را از Cache بخواند و اگر معتبر بود، دوباره از کاربر نخواهد.

### 1.2. Step 2 — Base GTA SA Mods Address

Step 2 فقط برای مشخص کردن **Base Address مدهای GTA SA** است تا کاربر راحت‌تر پوشه مد را در Step 3 انتخاب کند.

این Step اختیاری است و کاربر می‌تواند خالی رد شود.

نمونه:

```text
G:\F\Mods
F:\File\Game\AddonsSa
H:\Film\gamebatter
```

این مسیر بعد از انتخاب باید در Cache ذخیره شود و Input مربوط به Step 2 هنگام اجرای بعدی به‌صورت Default با همین مسیر پر شود.

مثلاً اگر کاربر انتخاب کرده:

```text
G:\F\Mods
```

در Step 3، Folder Picker انتخاب Mod باید مستقیماً از این مسیر باز شود:

```text
G:\F\Mods
```

و کاربر بتواند مثلاً این پوشه را انتخاب کند:

```text
G:\F\Mods\Maps\object gta v
```

اگر Base Address خالی باشد، Folder Picker با رفتار عادی سیستم باز شود.

### 1.3. حفظ Cache هنگام برگشت

اگر کاربر:

```text
Step 3 → Previous → Step 2
```

برگردد، Input Step 2 باید Cache شده را نشان دهد.

و اگر:

```text
Step 2 → Previous → Step 1
```

برگردد، Input Step 1 باید Game Folder ذخیره‌شده را نشان دهد.

نباید مقدار خالی UI باعث overwrite شدن Cache معتبر شود.

---

# 2. Dependency قبل از نصب Mod

قبل از نصب هر Mod، برنامه باید Game Folder را بررسی کند و وجود این موارد را چک کند:

```text
cleo.asi
modloader.asi
cleo\
modloader\
```

اگر همه موجود بودند، نصب Mod ادامه پیدا کند.

اگر موردی وجود نداشت، برنامه باید ابتدا Dependencyهای لازم را از این مسیر نصب کند:

```text
Base Mod Folder\Scripts\A1-MyReqFiles
```

محتوای این پوشه باید طبق `config.json` خودش نصب شود.

ترتیب:

```text
Check Dependencies
        ↓
Install missing dependencies from A1-MyReqFiles
        ↓
Install user's Mod
```

---

# 3. Backup System

Backup به‌صورت پیش‌فرض باید داخل Game Folder باشد:

```text
<GameFolder>\zBackupFiles
```

و پوشه `zBackupFiles` باید Hidden باشد.

### 3.1. بررسی فضای Game Drive

قبل از Backup، فضای موردنیاز واقعی Backup محاسبه شود.

برای حالت عادی، اگر فضای کافی در Drive بازی وجود داشت:

```text
<GameFolder>\zBackupFiles
```

استفاده شود.

### 3.2. اگر Game Drive فضای کافی نداشت

اگر فضای Game Drive کافی نبود، برنامه ابتدا یک فایل JSON مخفی داخل Game Folder ایجاد/استفاده کند:

```text
<GameFolder>\.zGtaSaModManager.json
```

این فایل شامل یک `GameInstanceId` یکتا باشد:

```json
{
  "schemaVersion": 1,
  "gameInstanceId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

سپس فضای موردنیاز حالت Backup روی C با مقدار محافظه‌کارانه محاسبه شود:

```text
Mod Folder Size × 3
```

و بررسی شود که C فضای کافی دارد یا خیر.

### 3.3. Backup روی C

اگر C فضای کافی داشت:

```text
C:\Program Files (x86)\GTA San Andreas\zBackupFiles\<GameInstanceId>\
```

Backup باید ساختار زیر را حفظ کند:

```text
<GameInstanceId>\
└── newHud\
    └── models\
        └── hud.txd
```

یعنی:

```text
ID\ModFolder\OriginalRelativePath
```

### 3.4. جلوگیری از اشتباه بعد از Uninstall/Reinstall

وجود Backup به‌تنهایی نباید به معنی نصب بودن Mod باشد.

`GameInstanceId` باید مشخص کند Backup متعلق به کدام نصب GTA است.

چون `.zGtaSaModManager.json` داخل Game Folder قرار دارد، اگر GTA حذف شود، این فایل هم حذف می‌شود.

در نصب مجدد GTA، چون JSON قبلی وجود ندارد، یک `GameInstanceId` جدید ایجاد می‌شود.

در نتیجه Backup قدیمی روی C با نصب جدید اشتباه گرفته نمی‌شود.

### 3.5. پاک کردن IDهای قدیمی

اگر Backup روی C ساخته می‌شود، پوشه‌های ID قدیمی می‌توانند پاک شوند، اما فقط اگر برنامه مطمئن باشد که آن‌ها متعلق به Installationهای منقضی‌شده هستند.

نباید همه IDها بدون بررسی کورکورانه حذف شوند.

### 3.6. اگر Game Drive و C هر دو کمبود فضا داشتند

به کاربر پیام داده شود:

```text
Drive X و Drive C فضای کافی برای Backup ندارند.
آیا می‌خواهید فایل‌های Backup را در مسیر دیگری ذخیره کنید؟
```

دکمه‌ها:

```text
بله، مسیر دیگری را انتخاب می‌کنم
خیر، نیازی به Backup ندارم
```

اگر کاربر مسیر دیگری را انتخاب کرد، Folder Picker باز شود.

اگر کاربر گفت Backup نمی‌خواهد، نصب بدون Backup فقط بعد از تأیید صریح کاربر انجام شود.

---

# 4. Mod Types

## 4.1. `PutInModloader`

تمام محتوای Mod داخل:

```text
<GameFolder>\modloader\<ModName>\
```

کپی شود.

ساختار نسبی فایل‌ها حفظ شود.

`config.json`:

```json
{
  "type": "PutInModloader"
}
```

---

## 4.2. `Replacing`

فایل‌های Mod به‌عنوان Replacement فایل‌های اصلی GTA نصب شوند.

قبل از Replace:

```text
Backup Original
        ↓
Replace Original
```

`config.json`:

```json
{
  "type": "Replacing"
}
```

---

## 4.3. `PutInCleo`

تمام محتوای Mod داخل:

```text
<GameFolder>\cleo\
```

کپی شود.

ساختار نسبی فایل‌ها حفظ شود.

`config.json`:

```json
{
  "type": "PutInCleo"
}
```

---

## 4.4. `PutInGameFolder`

محتوای Mod مستقیماً داخل:

```text
<GameFolder>\
```

کپی شود.

ساختار نسبی فایل‌ها حفظ شود.

در موارد نادر، اگر فایل Unlisted قرار است فایل موجود Game Folder را جایگزین کند، برنامه بررسی کند که آیا فایل موجود است و در صورت نیاز Backup بگیرد.

`config.json`:

```json
{
  "type": "PutInGameFolder"
}
```

---

## 4.5. `PutAndReplace`

محتوای Mod داخل Game Folder نصب شود، اما فایل‌هایی که باید Replacement باشند در `config.json` مشخص شوند.

دو فرمت باید **هر دو معتبر و قابل استفاده** باشند.

### فرمت کوتاه

```json
{
  "type": "PutAndReplace",
  "replacements": [
    "models/hud.txd",
    "models/generic/vehicle.txd"
  ]
}
```

در این حالت:

```text
source = target
```

در نظر گرفته شود.

یعنی:

```text
models/hud.txd
→ models/hud.txd

models/generic/vehicle.txd
→ models/generic/vehicle.txd
```

### فرمت کامل

```json
{
  "type": "PutAndReplace",
  "replacements": [
    {
      "source": "models/hud.txd",
      "target": "models/hud.txd"
    },
    {
      "source": "models/generic/vehicle.txd",
      "target": "models/generic/vehicle.txd"
    }
  ]
}
```

در این حالت `source` مسیر فایل داخل Mod Package و `target` مسیر فایل مقصد است.

بنابراین Parser باید هر دو نوع را قبول کند:

```text
string
→ source = target = string

object
→ source = object.source
→ target = object.target
```

فایل‌های Replacement:

```text
Backup Original
        ↓
Replace
```

و سایر فایل‌های Mod طبق منطق `PutInGameFolder` نصب شوند.

---

## 4.6. `PutAndReplaces`

برای Modهای سنگین با تعداد Replacement زیاد.

برنامه باید خودش تشخیص دهد کدام فایل‌های Mod قرار است فایل‌های اصلی GTA را Replace کنند.

برای Replacementها:

```text
Detect
→ Backup Original
→ Replace
```

`config.json`:

```json
{
  "type": "PutAndReplaces"
}
```

---

# 5. `VehicleAndSkinAndWeapon`

این Type برای Vehicle / Skin / Weapon است.

**هیچ `assets.json` داخل Mod وجود ندارد.**

تنها فایل Asset برنامه:

```text
Assets\Assets.json
```

است که **داخل خود پروژه C#** قرار دارد و برنامه آن را به‌عنوان دیتابیس داخلی Assetها می‌خواند.

ساختار پروژه:

```text
Assets\
└── Assets.json
```

اطلاعاتی مثل:

```text
ID
Name
NameFile
Category
Image
```

از همین فایل خوانده شوند.

ساختار `Assets.json`:

```json
{
  "Vehicles": [
    {
      "id": "522",
      "name": "NRG-500",
      "nameFile": "nrg500",
      "category": "Motorcycles",
      "image": "Assets/Vehicles/nrg500.png"
    }
  ],
  "Skins": [
    {
      "id": "7",
      "name": "Sweet",
      "nameFile": "sweet",
      "image": "Assets/Skins/sweet.png"
    }
  ],
  "Weapons": [
    {
      "id": "5",
      "name": "Baseball Bat",
      "nameFile": "bat",
      "image": "Assets/Weapons/bat.png"
    }
  ]
}
```

`config.json` خود Mod:

```json
{
  "type": "VehicleAndSkinAndWeapon"
}
```

برنامه از وجود فایل‌های `.dff` / `.txd` داخل Mod و دیتابیس داخلی `Assets.json` برای تشخیص و نمایش Assetها استفاده کند.

### 5.1. تشخیص Asset

```text
Vehicles → Vehicle
Skins → Skin
Weapons → Weapon
```

### 5.2. Vehicle Category

برای Vehicleها `category` از `Assets.json` خوانده شود.

مثلاً:

```text
Motorcycles
Cars
Planes
Helicopters
Boats
...
```

### 5.3. Step 5

اگر Vehicle وجود داشته باشد:

* Vehicleها نمایش داده شوند.
* فقط Assetهای موجود در Mod قابل انتخاب باشند.
* Category Filter نمایش داده شود.
* تعداد ستون‌ها قابل انتخاب باشد:

```text
2
3
4
5
```

### 5.4. تغییر نام فایل

نام فایل نصب‌شده از `NameFile` در `Assets.json` تعیین شود.

مثلاً اگر:

```text
NameFile = bfori
```

باشد:

```text
<GameFolder>\modloader\Mr Been\
├── bfori.dff
└── bfori.txd
```

نام Package تعیین‌کننده نام فایل Asset نباشد.

---

# 6. `VehiclesAndSkinsAndWeapons`

این Type نیز از همان **یک `Assets\Assets.json` داخلی پروژه** استفاده می‌کند.

هیچ `assets.json` داخل Mod Package وجود ندارد.

`config.json`:

```json
{
  "type": "VehiclesAndSkinsAndWeapons"
}
```

Package می‌تواند ترکیبی از:

```text
Vehicle
Skin
Weapon
```

باشد.

مثلاً:

```text
2013929-john__wick\
├── csher.dff
├── csher.txd
├── dsher.dff
├── dsher.txd
├── bullet.dff
└── bullet.txd
```

هر Item باید جداگانه در Step 5 انتخاب شود.

```text
Item 1
   ↓
Next
   ↓
Item 2
   ↓
Next
   ↓
Item 3
```

و وضعیت دکمه‌ها دقیقاً:

```text
همه بدون تغییر
→ It's OK, Install

بعضی تغییر کرده‌اند
→ enough

همه به جز یک مورد تغییر کرده‌اند
→ enough 2

همه تغییر کرده‌اند
→ install
```

---

# 7. `SavesAndMissions`

این Type برای Save و Mission است.

`config.json` فقط:

```json
{
  "type": "SavesAndMissions"
}
```

باشد.

**هیچ `contentType` یا فیلد اضافه‌ای لازم نیست.**

نام Save/Mission از **نام پوشه Package انتخاب‌شده** گرفته شود.

مثلاً:

```text
My 100 Percent Save\
├── config.json
└── GTASAsf1.b
```

نام Save:

```text
My 100 Percent Save
```

است.

مسیر User Files باید از مسیر واقعی Documents سیستم پیدا شود و هاردکد نشود.

---

## 7.1. User Files وجود ندارد

اگر:

```text
GTA San Andreas User Files\
```

وجود نداشت، ساخته شود.

`.trash` نباید ایجاد شود.

Save مستقیماً نصب شود.

---

## 7.2. User Files وجود دارد ولی Save ندارد

اگر هیچ Save موجود نباشد، Save مستقیماً نصب شود.

`.trash` لازم نیست و اگر وجود داشت حذف شود.

---

## 7.3. چند Save موجود است

مثلاً:

```text
GTASAsf1.b
GTASAsf2.b
GTASAsf5.b
GTASAsf8.b
```

پوشه:

```text
.trash
```

ساخته و Hidden شود.

اولین Slot خالی به‌صورت خودکار انتخاب شود:

```text
1 → موجود
2 → موجود
3 → خالی ← Active
4 → خالی
5 → موجود
6 → خالی
7 → خالی
8 → موجود
```

Step 5:

```text
1. GTASAsf1.b
2. GTASAsf2.b
3. Empty Save (GTASAsf3.b)
4. Empty Save (GTASAsf4.b)
5. GTASAsf5.b
6. Empty Save (GTASAsf6.b)
7. Empty Save (GTASAsf7.b)
8. GTASAsf8.b
```

کاربر بتواند Slot دیگری را انتخاب کند.

در صورت جایگزینی Save موجود، Save قبلی از طریق `.trash` مدیریت شود و Backup معمولی لازم نباشد.

---

## 7.4. نمایش نام Save

اگر Parser واقعی بتواند نام Save را از فایل `.b` استخراج کند، نمایش داده شود:

```text
3 (Save Name)
```

نباید Offset یا اطلاعات ساختگی/حدسی استفاده شود.

---

## 7.5. تشخیص Save و DYOM

برنامه باید از **محتوای Package انتخاب‌شده** تشخیص دهد که Package مربوط به Save است یا DYOM.

برای Save:

```text
GTASAsf1.b
...
GTASAsf8.b
```

برای DYOM:

```text
DYOM1.dat
...
DYOM8.dat
```

در صورت DYOM، Dependency زیر نیز نصب شود:

```text
Base Mods Address\Scripts\DYOM\DYOM v8.2
```

داخل:

```text
GTA San Andreas User Files
```

کپی شود.

منطق Slot برای DYOM نیز مشابه Save باشد.

---

# 8. `MissionDSL`

`config.json`:

```json
{
  "type": "MissionDSL"
}
```

هیچ Dependency یا `target` اضافی در `config.json` لازم نیست؛ این رفتار از خود Type مشخص است.

Dependency:

```text
Base Mods Address\Scripts\DYOM\DYOM v8.2
```

داخل:

```text
GTA San Andreas User Files
```

کپی شود.

سپس تمام محتویات:

```text
GTA San Andreas User Files\DSL\
```

پاک شود.

محتوای DSL Mod طبق ساختار خودش نصب شود.

برای Uninstall نیز محتویات DSL طبق منطق Type پاک شود.

---

# 9. Uninstall

## 9.1. PutInModloader

پوشه Mod مربوطه از:

```text
<GameFolder>\modloader\
```

حذف شود.

## 9.2. PutInCleo

فقط فایل‌هایی که همان Mod نصب کرده، بر اساس Installation Manifest حذف شوند.

## 9.3. PutInGameFolder

فقط فایل‌های نصب‌شده توسط همان Mod حذف شوند.

## 9.4. PutAndReplace

ابتدا فایل‌های Mod نصب‌شده حذف شوند.

سپس Originalها از Backup Restore شوند.

در پایان Backup مربوط به همان Mod حذف شود.

ساختار Backup:

```text
<GameInstanceId>\
└── newHud\
    └── models\
        └── hud.txd
```

## 9.5. PutAndReplaces

مانند `PutAndReplace`.

## 9.6. VehicleAndSkinAndWeapon

اگر Asset قبلاً در ModLoader نصب شده باشد، پیام باید بر اساس **Asset واقعی داخل ModLoader** باشد، نه نام Package.

مثلاً:

```text
آیا می‌خواهید وسیله نقلیه NRG-500 را پاک کنید؟
```

یا:

```text
آیا می‌خواهید شخصیت [Name] را پاک کنید؟
```

یا:

```text
آیا می‌خواهید اسلحه [Name] را پاک کنید؟
```

دکمه‌ها:

```text
بله
خیر
وسیله نقلیه / شخصیت / اسلحه را تغییر بده
```

در حالت Change، Step 5 باز شود.

Next تبدیل شود به:

```text
Change
```

## 9.7. SavesAndMissions

برای تشخیص Save نصب‌شده، فقط Slot بررسی نشود.

Installation Manifest:

```text
GTA San Andreas User Files\
└── .zGtaSaModManager\
    └── installations.json
```

مثلاً:

```json
{
  "type": "SavesAndMissions",
  "installedFiles": [
    "GTASAsf3.b"
  ],
  "modId": "..."
}
```

بنابراین برنامه دقیقاً می‌داند کدام Save را خودش نصب کرده و نباید Save شخصی کاربر را حذف کند.

همین سیستم برای:

```text
DYOM3.dat
```

نیز استفاده شود.

---

# 10. Type Alias و Case-Insensitive

نام Typeها باید Case-Insensitive باشند.

مثلاً:

```text
pUtiNModloAder
putINMOdloadeR
PUTINMODLOADER
```

همگی به:

```text
putinmodloader
```

Normalize شوند.

Aliasها:

```text
PutInModloader              → pim
Replacing                   → rip
PutInCleo                   → pic
PutInGameFolder             → pgf
PutAndReplace               → par
PutAndReplaces              → prs
VehicleAndSkinAndWeapon     → vsw
VehiclesAndSkinsAndWeapons  → vss
SavesAndMissions            → saw
MissionDSL                  → dsl
```

قبل از مقایسه، Type به lowercase تبدیل شود.

---

# 11. Design / UI

## 11.1. Title و Subtitle

در Stepهایی که Title + Subtitle + Input + Browse دارند، نباید Title یا Subtitle روی Input بیفتند.

ساختار دقیق:

```text
Title

Subtitle

[ Input................................ ] [ Browse ]
```

Title و Subtitle باید کاملاً از Input جدا باشند.

## 11.2. Input و Browse

Input انتخاب مسیر و Browse باید ظاهری شبیه کنترل‌های حرفه‌ای `Setup.exe` داشته باشند، نه یک Input و Button شبیه صفحه HTML خام.

ظاهر باید:

* مدرن
* تمیز
* حرفه‌ای
* Windows Installer-like
* دارای Padding مناسب
* Border ظریف
* ارتفاع مناسب
* Hover / Pressed / Disabled State
* در صورت هماهنگ بودن با UI، گوشه‌های کمی Rounded

باشد.

ظاهر کلی نباید حس Web Page خام یا HTML Form بدهد.
