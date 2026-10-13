# اتصال ماژول Tenant

این ماژول کتابخانه قابل اتصال و تست‌شده با TestServer است. Host اجرایی و provider احراز هویت هنوز ایجاد نشده‌اند.

## ثبت در composition root آینده

```csharp
builder.Services.AddBazarKavApplication(TenantPresentation.ApplicationAssembly);
builder.Services.AddTenantInfrastructure(connectionString);
builder.Services.AddTenantPresentation();
builder.Services.AddBazarKavWeb();
// Configure the actual authentication provider separately.

var app = builder.Build();
app.UseBazarKavWeb();
app.UseAuthentication();
app.UseAuthorization();
app.MapTenantEndpoints();
```

ثبت MediatR فقط یک بار با تمام assemblyهای Application انجام می‌شود.
برنامه باید authentication scheme واقعی داشته باشد و claim مدیریتی را از principal معتبر صادر کند.
scheme تست و X-Test-Actor فقط در پروژه تست هستند.

## migration با PowerShell

از ریشه D:\BazarKav:

```powershell
dotnet tool restore
# Set the actual connection through your secret/configuration mechanism:
$env:BAZARKAV_TENANT_CONNECTION_STRING = "Host=localhost;Port=5432;Database=bazarkav;Username=YOUR_USER;Password=YOUR_PASSWORD"
dotnet ef database update --project src/Modules/Tenant/Infrastructure --context TenantDbContext
Remove-Item Env:BAZARKAV_TENANT_CONNECTION_STRING
```

دیتابیس و نقش PostgreSQL باید از قبل ایجاد شده باشند. credential واقعی در مخزن ذخیره نمی‌شود.
راه دیگر اجرای صریح MigrateTenantDatabaseAsync در ابزار deployment است؛ migration خودکار در هر درخواست یا هر startup نداریم.

## درخواست‌ها

ایجاد:

```json
{"code":"sample-shop","name":"فروشگاه نمونه"}
```

تغییر نام:

```json
{"name":"نام جدید","expectedVersion":1}
```

فعال‌سازی:

```json
{"status":1,"reason":null,"expectedVersion":2}
```

تعلیق:

```json
{"status":2,"reason":"توقف موقت","expectedVersion":3}
```

Version را از آخرین پاسخ می‌گیرید؛ در 409 اطلاعات را دوباره دریافت می‌کنید، بدون retry کور.

## اجرای تست PostgreSQL در محیط محلی

یک دیتابیس جدا با نام bazarkav_test و نقش دارای اجازه CREATE DATABASE تهیه کنید.
BAZARKAV_TEST_POSTGRES را با اتصال همان دیتابیس تنظیم و dotnet test اجرا کنید.
تست‌ها فقط دیتابیس‌های تصادفی خودشان را حذف می‌کنند. این اتصال برای دیتابیس عملیاتی نیست.
