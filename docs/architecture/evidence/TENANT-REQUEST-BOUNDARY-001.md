# اصلاح مرز Requestهای HTTP

Base-SHA: 7ff00cdf008772bea05d8162122aa76bd2fe6a66
Code-SHA: eb1ed75325b2b4e0fb23c7f17b13d6cb756ae925
Stop-State: USER_REVIEW
automaticNextImplementationTask: NONE

## تغییر

TenantRequests.cs از Contracts حذف شد.
CreateTenantRequest، RenameTenantRequest و ChangeTenantStatusRequest هرکدام در فایل مستقل Presentation/Requests قرار گرفتند.
endpoint و تست‌های HTTP namespace جدید را مصرف می‌کنند؛ مسیر و فیلد JSON درخواست‌ها تغییر نکرده‌اند.
Host، schema، migration و نسخه بسته‌ها تغییر نکرده‌اند.

## گارد G16

- دستور الزامی در AGENTS.md و قواعد Cursor.
- بررسی محل declaration نوع و نام فایل در ابزار معماری، برای تمام ماژول‌ها.
- پسوند Request مختص ورودی HTTP و محل آن Presentation/Requests است.
- هر Request در فایل مستقل هم‌نام.
- تست معماری روی RequestType واقعی metadata endpointهای Tenant؛ علاوه بر بررسی نبود Request در assembly Contracts.
- regression برای فایل تجمیعی قدیمی، تغییر نام فایل جهت دور زدن گارد، فایل تجمیعی در Presentation و مسیر صحیح.

## شواهد

https://github.com/mrnikiemami-code/BazarKav/actions/runs/38429067443

Job 115406138401: success.
Build: صفر خطا و صفر هشدار.
UnitTests: 28 passed؛ IntegrationTests: 27 passed؛ ArchitectureTests: 8 passed.
مجموع برنامه: 63 passed، صفر failed و صفر skipped.
تست گارد ساختاری: 4 passed.
تمام ۸ تست PostgreSQL نیز در همان suite اجرا شدند.
