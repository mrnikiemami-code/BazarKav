# تأیید Mapster

Base-SHA: 995ff4a38d0c67499c0dd3f01b4a4c60866fc8d8
Code-SHA: b8f24b1fa2cfa39efe11da59116c9892c6870e58
Stop-State: USER_REVIEW
automaticNextImplementationTask: NONE

Mapster 10.0.13 با نسخه ثابت و مجوز MIT اضافه شد.
نگاشت دستی قدیمی Application/DTOs/TenantMapping.cs حذف شد.
Entity به DTO و Request به Command از نگاشت صریح Mapster استفاده می‌کنند.
projection EF Core نیز با همان تعریف DTO از ProjectToType استفاده می‌کند؛ تنظیمات مستقل‌اند.
Domain، Contracts، Host و schema دیتابیس تغییر نکردند.
G17 در دستورهای عامل‌ها و CI ثبت شد.

## CI

https://github.com/mrnikiemami-code/BazarKav/actions/runs/38435700863

Job 115428965928: success.

- Release build: صفر خطا و صفر هشدار.
- UnitTests: 33 passed.
- IntegrationTests: 28 passed.
- ArchitectureTests: 8 passed.
- مجموع برنامه: 69 passed، صفر failed و صفر skipped.
- گارد ساختاری و 7 regression گارد: عبور.

نگاشت هر هشت فیلد TenantDto در سه وضعیت، نگاشت Request و Id مسیر، رد نگاشت ثبت‌نشده و جدایی configurationها بررسی شدند.
تست PostgreSQL، برابری projection با نگاشت Entity persisted را در هر سه وضعیت و عدم tracking بررسی کرد.
همچنین تمام تست‌های قبلی HTTP، lifecycle، conflict، concurrency، migration و authorization دوباره در CI عبور کردند.
