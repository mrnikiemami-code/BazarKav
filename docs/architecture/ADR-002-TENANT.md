# ADR-002 — Tenant و ذخیره‌سازی آن

تاریخ: 2026-10-13
وضعیت: اجرا

## مرز دامین مصوب کاربر

هر کسب‌وکار مستقل یک Tenant مستقل است. شعبه و انبار زیرمجموعه همان کسب‌وکار هستند.
یک کاربر می‌تواند به چند Tenant دسترسی داشته باشد؛ عضویت و احراز هویت در این ماژول پیاده‌سازی نمی‌شود.
شناسه پایدار Tenant مرز کسب‌وکار برای ماژول‌های آینده خواهد بود؛ کد نمایشی جای شناسه امنیتی را نمی‌گیرد.

Tenant مسئول ثبت فضای کسب‌وکار، نام و چرخه وضعیت آن است. تحلیل، شعب، انبار، حسابداری و کاربران در این ماژول قرار نمی‌گیرند.

## مدل

- Id: GUID پایدار.
- Code: کد یکتای تغییرناپذیر، ۲ تا ۶۴ کاراکتر؛ حروف کوچک انگلیسی، عدد و خط تیره، شروع با حرف و پایان بدون خط تیره.
- Name: نام کسب‌وکار، غیرخالی، حداکثر ۲۰۰ کاراکتر، بدون کاراکتر کنترلی.
- Status: SettingUp=0، Active=1 و Suspended=2.
- SuspensionReason: دلیل تعلیق، ۱ تا ۵۰۰ کاراکتر؛ با فعال‌سازی مجدد پاک می‌شود.
- Version: نسخه مثبت افزایشی برای جلوگیری از ویرایش گم‌شده.
- CreatedAt و UpdatedAt: زمان UTC.

چرخه مجاز: SettingUp → Active، Active → Suspended و Suspended → Active.
بازگشت به SettingUp یا تغییر به همان وضعیت conflict است. حذف Tenant یا تغییر Code جزو قابلیت‌های این مرحله نیست.
Active در این مرحله یعنی کسب‌وکار توسط مدیر فعال شده است؛ اتصال حسابداری یا آمادگی داده هنوز موضوع این ماژول نیست.

## معماری

پنج پروژه Domain، Application، Infrastructure، Presentation و Contracts.
Domain و Contracts به بسته یا پروژه دیگری وابسته نیستند.
Command، Query و Validation پوشه‌های مستقل دارند؛ ۳ Command، ۲ Query، ۵ handler و ۵ validator.
DTOهای عمومی در Contracts هستند؛ هیچ Entity یا DbContext از endpoint خارج نمی‌شود.

## ذخیره‌سازی و UnitOfWork

PostgreSQL و EF Core انتخاب شدند؛ نسخه‌های مستقیم:
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 — مجوز PostgreSQL.
- Microsoft.EntityFrameworkCore و Relational و Design 10.0.12 — مجوز MIT.
- dotnet-ef 10.0.12 در manifest ابزار، هم‌نسخه EF.

مجوزها از nuspec همان نسخه بررسی شدند. نسخه‌های مستقیم مرکزی و نسخه‌های گذرا در lock file ثبت شدند.
مرجع: https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3

جدول tenant.tenants و migration واقعی InitialTenants همراه snapshot ایجاد شدند.
قیود نام، کد، وضعیت، دلیل تعلیق، نسخه و زمان در دیتابیس نیز محافظت می‌شوند.
unique index روی Code، مرجع نهایی جلوگیری از تکرار است؛ precheck فقط بازخورد زودهنگام می‌دهد.

ITenantUnitOfWork در Application/Abstractions و EfTenantUnitOfWork در Infrastructure/Persistence است.
هر Command یک SaveChangesAsync دارد؛ transaction همان EF Core است، نه transaction behavior عمومی.
Repository ذخیره نهایی نمی‌کند. QueryReader فقط AsNoTracking و projection به DTO دارد.
Version یک concurrency token است؛ تغییر رقابتی هم در handler با ExpectedVersion و هم در SQL update کنترل می‌شود.
فقط unique violation روی index مشخص کد و DbUpdateConcurrencyException به conflict نوع‌دار تبدیل می‌شوند.
خطای اتصال، خطای ناشناخته و cancellation بلعیده نمی‌شوند. پس از conflict، ChangeTracker پاک می‌شود.

## مرز دسترسی

API این ماژول یک registry مدیریتی در سطح برنامه است؛ فهرست Tenantها داده عمومی کاربران کسب‌وکار نیست.
تمام routeها policy TenantManagement می‌خواهند: principal احرازشده با claim bazarkav.permission=tenant.manage.
provider احراز هویت در Host آینده متصل می‌شود. ماژول scheme یا رمز پیش‌فرض یا header احراز هویت ندارد.
X-Tenant-ID و CorrelationId هیچ مجوزی ایجاد نمی‌کنند.
کاربر عادی حتی در صورت احراز هویت به registry دسترسی ندارد.
handlerهای داخلی از مرز قابل اعتماد برنامه فراخوانی می‌شوند؛ ورودی HTTP همیشه از policy و MediatR pipeline عبور می‌کند.

این ماژول هنوز مجوز دسترسی کاربر به داده کسب‌وکار، عضویت چند Tenant یا کنترل فعال بودن Tenant برای سایر ماژول‌ها را پیاده‌سازی نمی‌کند؛ آن‌ها مسئولیت مسیر دسترسی و ماژول‌های آینده هستند.

## قابلیت‌ها

| روش و مسیر | کاربرد |
|---|---|
| POST /api/v1/tenants | ایجاد، پاسخ 201 و Location |
| GET /api/v1/tenants/{id} | دریافت مشخصات |
| GET /api/v1/tenants?page=1&pageSize=20&status=1 | فهرست با فیلتر وضعیت و صفحه‌بندی |
| PUT /api/v1/tenants/{id}/name | تغییر نام با ExpectedVersion |
| PUT /api/v1/tenants/{id}/status | تغییر وضعیت با ExpectedVersion و دلیل تعلیق |

فهرست بر اساس CreatedAt و Id مرتب می‌شود. اندازه صفحه ۱ تا ۱۰۰ و شماره صفحه ۱ تا ۱۰۰۰۰۰۰ است.
totalCount و items در دو query دریافت می‌شوند؛ در صورت تغییر همزمان، این فهرست تضمین snapshot واحد ندارد.

## بررسی‌ها

تست دامین و pipeline و معماری به صورت محلی اجرا می‌شوند.
۸ تست PostgreSQL واقعی در CI: migration و عدم drift، چرخه HTTP، خطاهای ورودی و conflict، 401/403، uniqueness پس از دو precheck، concurrency و atomic rollback، query بدون tracking و cancellation.
هر تست یک دیتابیس تصادفی با پیشوند bazarkav_test_ می‌سازد و همان را پاک می‌کند؛ اتصال پایه باید نام bazarkav_test داشته باشد.
تست PostgreSQL در نبود BAZARKAV_TEST_POSTGRES صریحاً skip می‌شود؛ CI این متغیر و سرویس PostgreSQL را همیشه فراهم می‌کند.
