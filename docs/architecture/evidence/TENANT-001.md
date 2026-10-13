# شواهد تکمیل Tenant

تاریخ: 2026-10-13
Base-SHA: 48bd084dbcbdd9cb0ead915df7a4a09c0afbe13a
Code-SHA: c7b8c93bd1ea08da41673da476b4817546c03fef
Stop-State: USER_REVIEW
automaticNextImplementationTask: NONE

## نتیجه CI

https://github.com/mrnikiemami-code/BazarKav/actions/runs/38423428401

Job: 115386441531 — success

- گارد ساختاری: عبور؛ ۸ پروژه تولید.
- restore با locked mode: عبور.
- Release build: صفر خطا و صفر هشدار.
- UnitTests: 28 passed، 0 failed، 0 skipped.
- IntegrationTests: 27 passed، 0 failed، 0 skipped.
- ArchitectureTests: 6 passed، 0 failed، 0 skipped.
- مجموع: 61 passed، بدون شکست یا skip.

## بررسی PostgreSQL واقعی

هر ۸ تست Tenant روی سرویس postgres:16-alpine در CI اجرا شد:

1. migration تکرارپذیر و نبود اختلاف مدل و snapshot.
2. ایجاد، تغییر نام، فعال‌سازی، تعلیق، فعال‌سازی مجدد و دریافت از scope تازه از مسیر HTTP.
3. validation، کد تکراری، نسخه قدیمی، تغییر وضعیت نامعتبر و 404.
4. پاسخ 401 و 403 و عدم ایجاد داده با header دلخواه Tenant.
5. unique constraint پس از عبور هر دو precheck.
6. جلوگیری از ویرایش گم‌شده و rollback اتمیک batch دارای کد تکراری.
7. query بدون tracking با فیلتر و صفحه‌بندی.
8. انتقال cancellation در Repository، Reader و UnitOfWork.

## محدوده تحویل

ماژول Tenant و مسیرهای عمومی اتصال آن کامل هستند. Host اجرایی، authentication provider، عضویت کاربران، شعب، انبار و اتصال حسابداری در این تحویل ایجاد نشده‌اند.
auth scheme و header تست فقط در tests قرار دارند.
این تست‌ها اثبات جداسازی داده ماژول‌های آینده نیستند؛ آن‌ها هنوز ایجاد نشده‌اند.
این ثبت فقط مستندات را تغییر می‌دهد؛ کد همان Code-SHA تأییدشده است.
