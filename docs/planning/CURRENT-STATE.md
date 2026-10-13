# وضعیت فعلی بازارکاو

تاریخ: 2026-10-13
مرحله: TENANT_001_COMPLETED
Stop-State: USER_REVIEW
automaticNextImplementationTask: NONE

## انجام‌شده

- تصمیم قطعی: هر کسب‌وکار مستقل یک Tenant؛ یک کاربر می‌تواند چند Tenant داشته باشد.
- ماژول Tenant با پنج پروژه مستقل، سه Command، دو Query و پنج validator.
- ایجاد، دریافت، فهرست صفحه‌بندی‌شده، تغییر نام و چرخه وضعیت.
- PostgreSQL و EF Core، migration و snapshot، Repository و QueryReader و UnitOfWork اختصاصی.
- unique index، concurrency token و ExpectedVersion و کنترل atomic SaveChanges.
- policy مدیریتی روی هر پنج route؛ بدون scheme یا header احراز هویت در کد تولید.

- سه پروژه مستقل BuildingBlocks/Application، Observability و Web روی net10.0.
- MediatR 12.5.0، قراردادهای ICommand/IQuery، Result/Error و رفتارهای Logging و Validation.
- اجرای ترتیبی validatorهای async و انتقال CancellationToken به validator و handler.
- Serilog با خروجی JSON کنسول و فایل روزانه و retention قابل تنظیم.
- correlation معتبر، scope لاگ، حفظ Activity موجود و traceparent معتبر و fallback W3C.
- ActivitySource، Meter و instrumentation پایه؛ بدون exporter خارجی.
- ProblemDetails فارسی، خطای موردانتظار نوع‌دار، validation بدون افشای مقدار و ثبت امن خطای غیرمنتظره یک بار.
- solution، نسخه‌های مرکزی، lock fileها، global.json و گیت restore/build/test در CI.
- تست‌های واحد، HTTP با TestServer و وابستگی assembly.

## شواهد بررسی

- restore در locked mode: موفق.
- Release build: صفر خطا و صفر هشدار.
- UnitTests: 28/28.
- IntegrationTests محلی: 19/19؛ ۸ تست PostgreSQL به دلیل نبود سرویس محلی صریحاً skip شده‌اند.
- CI با PostgreSQL واقعی: UnitTests 28/28، IntegrationTests 27/27 و ArchitectureTests 6/6؛ مجموع 61/61 و صفر skip.
- ArchitectureTests: 6/6.
- گارد ساختاری: موفق، ۸ پروژه تولید.
- بررسی diff: بدون خطای whitespace.

Code-SHA: c7b8c93bd1ea08da41673da476b4817546c03fef
CI: https://github.com/mrnikiemami-code/BazarKav/actions/runs/38423428401
نتیجه: success؛ build بدون خطا و هشدار و هر ۸ تست PostgreSQL واقعی عبور کردند.
جزئیات در docs/architecture/evidence/TENANT-001.md ثبت شده است.

## محدوده اجرا

Host، provider احراز هویت، عضویت کاربران، شعب، انبار، آداپتر حسابداری و WebApp ایجاد نشده‌اند.
ماژول Tenant کتابخانه قابل اتصال است؛ اجرای HTTP فعلاً با TestServer در تست‌ها انجام می‌شود.
کد TestServer فقط زیر tests است.
BusinessId/BranchId/SourceId باید بعداً از زمینه معتبر دسترسی به scope اضافه شوند؛ از header دلخواه استخراج نمی‌شوند.
exporter OpenTelemetry هنوز انتخاب و فعال نشده است.

## تصمیم‌های باز

مرز سایر ماژول‌ها و frontend، تعریف شاخص‌ها، ترکیب قواعد مدیر و روال، provider احراز هویت و عضویت کاربران و رفع همپوشانی منابع.

گام بعد فقط با تصمیم کاربر: ادامه طراحی دامین یا تعیین اولین ماژول.
