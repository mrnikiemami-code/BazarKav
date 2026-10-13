# ADR-003 — استاندارد Mapster

تاریخ: 2026-10-13
وضعیت: اجرا طبق درخواست کاربر

Mapster استاندارد نگاشت مکانیکی اشیای پروژه است. نسخه 10.0.13 با مجوز MIT در نسخه‌های مرکزی pin شده و مجوز همان بسته از nuspec بررسی شد.
منبع: https://www.nuget.org/packages/Mapster/10.0.13

## جایگاه

- Application/Mappings: Entity به DTO کاربردی.
- Presentation/Mappings: Request به Command؛ Id از route به منبع نگاشت داده می‌شود.
- Infrastructure/Persistence/Configurations: projection قابل اجرای EF Core.
- Domain و Contracts مستقل از Mapster می‌مانند.

نگاشت هشت فیلد TenantDto یک بار در TenantMapping تعریف می‌شود؛ Application از Adapt و Infrastructure از ProjectToType با configuration مستقل ساخته‌شده از همان تعریف استفاده می‌کنند.
تبدیل Status به صورت cast صریح تعریف شده است؛ نتیجه به convention نگاشت نام enum وابسته نیست.
برای recordها MapToConstructor فعال است.

## تنظیمات

RequireExplicitMapping و RequireDestinationMemberSource فعال‌اند. configurationها private، جدا از GlobalSettings و پس از تعریف compile می‌شوند.
AddTenantPresentation تنظیمات نگاشت کاربردی و ورودی HTTP را پیش از سرویس‌دهی validate می‌کند.
AddTenantInfrastructure نیز CompileProjection را validate می‌کند.
در مسیر درخواست هیچ configuration جدیدی ساخته نمی‌شود؛ configهای static پس از initialization تغییر نمی‌کنند.

Query ابتدا فیلتر، ترتیب و صفحه‌بندی می‌شود، سپس ProjectToType و ToListAsync/SingleOrDefaultAsync اجرا می‌شوند. Entity برای تبدیل در حافظه materialize نمی‌شود.
شناسه route، Version و فیلدهای مجاز ورودی به صورت صریح نگاشت می‌شوند. نگاشت مستقیم Request به Entity یا تغییر شناسه/وضعیت aggregate با Adapt ممنوع است.

ساخت aggregate با factory دامین، ساخت TenantPage با count و paging، ساخت Query از پارامتر route و تصمیم Error به HTTP، تبدیل مکانیکی یک شیء نیستند و همچنان صریح اجرا می‌شوند.
ProblemDetailsMapper و قواعد خطا منطق Web هستند؛ به Mapster منتقل نمی‌شوند.

## گارد G17

AGENTS و Cursor استفاده از Mapster صریح را الزام می‌کنند.
گارد ساختاری محل استفاده، GlobalSettings و Adapt/ProjectToType بدون configuration را رد می‌کند.
گارد ساختاری بررسی کامل همه overloadهای Mapster یا معنای هر نگاشت نیست؛ تست و بازبینی تنظیمات نیز الزامی است.
تست واحد هشت فیلد DTO در هر سه وضعیت، انتقال داده Request و Id route، رد نگاشت ثبت‌نشده و استقلال configurationها را پوشش می‌دهد.
تست PostgreSQL برابری projection و نگاشت حافظه را روی داده persisted در هر سه وضعیت بررسی می‌کند.
