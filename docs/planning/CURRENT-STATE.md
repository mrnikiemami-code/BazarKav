# وضعیت فعلی بازارکاو

تاریخ: 2026-10-13
مرحله: BUILDING_BLOCKS_001_COMPLETED
Stop-State: USER_REVIEW
automaticNextImplementationTask: NONE

## انجام‌شده

- سه پروژه مستقل BuildingBlocks/Application، Observability و Web روی net10.0.
- MediatR 12.5.0، قراردادهای ICommand/IQuery، Result/Error و رفتارهای Logging و Validation.
- اجرای ترتیبی validatorهای async و انتقال CancellationToken به validator و handler.
- Serilog با خروجی JSON کنسول و فایل روزانه و retention قابل تنظیم.
- correlation معتبر، scope لاگ، حفظ Activity موجود و traceparent معتبر و fallback W3C.
- ActivitySource، Meter و instrumentation پایه؛ بدون exporter خارجی.
- ProblemDetails فارسی، خطای موردانتظار نوع‌دار، validation بدون افشای مقدار و ثبت امن خطای غیرمنتظره یک بار.
- solution، نسخه‌های مرکزی، lock fileها، global.json و گیت restore/build/test در CI.
- تست‌های واحد، HTTP با TestServer و وابستگی assembly.

## شواهد بررسی محلی

- restore در locked mode: موفق.
- Release build: صفر خطا و صفر هشدار.
- UnitTests: 12/12.
- IntegrationTests: 19/19.
- ArchitectureTests: 3/3.
- گارد ساختاری: موفق، ۳ پروژه تولید.
- بررسی diff: بدون خطای whitespace.

نتیجه CI از راه دور در این snapshot هنوز ثبت نشده است؛ نتیجه محلی جایگزین ادعای اجرای CI نیست.

## محدوده اجرا

Host، ماژول کسب‌وکار، دیتابیس، آداپتر حسابداری و WebApp ایجاد نشده‌اند.
کد TestServer فقط زیر tests است.
BusinessId/BranchId/SourceId باید بعداً از زمینه معتبر دسترسی به scope اضافه شوند؛ از header دلخواه استخراج نمی‌شوند.
exporter OpenTelemetry هنوز انتخاب و فعال نشده است.

## تصمیم‌های باز

مرز نهایی ماژول‌ها، دیتابیس و frontend، تعریف شاخص‌ها، ترکیب قواعد مدیر و روال، سیاست دسترسی و رفع همپوشانی منابع.

گام بعد فقط با تصمیم کاربر: ادامه طراحی دامین یا تعیین اولین ماژول.
