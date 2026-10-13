# ADR-001 — پایه مشترک قابل اجرا

تاریخ: 2026-10-13
وضعیت: اجرا در محدوده BuildingBlocks

## تصمیم

سه پروژه مستقل با جهت وابستگی مصوب:
- Application: MediatR، validation، logging behavior، قرارداد Command/Query و Result/Error.
- Observability: correlation امن، ActivitySource/Meter و Serilog با خروجی JSON کنسول یا فایل روزانه.
- Web: middleware، نگاشت Result/Error و ExceptionHandler با ProblemDetails فارسی.

Host و ماژول کسب‌وکار در این مرحله ایجاد نمی‌شوند. TestServer فقط در پروژه تست است.

## نسخه‌های انتخاب‌شده

SDK 10.0.401 و target framework net10.0. نسخه runtime متعلق به نصب .NET است؛ اجرای محلی از 10.0.12 استفاده می‌کند. global.json فقط patch سازگار همان feature band را می‌پذیرد.

| بسته | نسخه مستقیم | مجوز |
|---|---|---|
| MediatR | 12.5.0 | Apache-2.0 |
| FluentValidation.DependencyInjectionExtensions | 12.0.0 | Apache-2.0 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.0 | MIT |
| Serilog.Extensions.Logging | 10.0.0 | Apache-2.0 |
| Serilog.Sinks.Console | 6.0.0 | Apache-2.0 |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 |
| Microsoft.AspNetCore.TestHost | 10.0.0 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.1 | Apache-2.0 |

نسخه‌های مستقیم در Directory.Packages.props و نسخه‌های گذرا در packages.lock.json ثبت می‌شوند. CI با locked mode دریافت می‌کند. MediatR 12.5.0 برای استفاده از نسخه با مجوز Apache انتخاب شده است. مجوزهای بسته‌ها از nuspec همان نسخه بررسی شده‌اند.

منابع:
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://www.nuget.org/packages/MediatR/12.5.0
- https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions/12.0.0
- https://www.nuget.org/packages/Serilog.Extensions.Logging/10.0.0

## رفتارها

ثبت AddBazarKavApplication یک بار با تمام assemblyهای handler انجام می‌شود. ترتیب Logging سپس Validation است. validatorهای scoped به‌ترتیب اجرا می‌شوند تا سرویس اشتراکی مانند DbContext همزمان استفاده نشود. رفتار logging داده ورودی/خروجی را ثبت نمی‌کند و exception را دوباره لاگ نمی‌کند. Completed به معنی پایان اجراست؛ لزوماً موفقیت تجاری Result نیست.

AddBazarKavWeb سرویس‌ها را ثبت می‌کند؛ UseBazarKavWeb ابتدا correlation و سپس exception handler و status-code pages را فعال می‌کند. شناسه تنها ASCII شامل حرف، عدد، خط تیره و زیرخط با حداکثر ۶۴ کاراکتر است. مقدار نامعتبر یا چندگانه جایگزین می‌شود.

اگر ASP.NET Core برای درخواست Activity ایجاد کرده باشد، همان حفظ می‌شود. در نبود listener و Activity، middleware یک Activity با قالب W3C ایجاد می‌کند و traceparent معتبر را به عنوان parent می‌پذیرد. ActivitySource و Meter برای کارهای پس‌زمینه و instrumentation موجودند؛ exporter OpenTelemetry در این مرحله فعال نشده است و مقصد آن بعداً انتخاب می‌شود.

ExceptionHandler خطای غیرمنتظره را یک بار با نوع و stack trace و HResult ثبت می‌کند؛ متن exception و inner exception ممکن است حاوی اسرار باشد و ثبت نمی‌شود. diagnostics پیش‌فرض .NET 10 برای خطاهای handled خاموش است تا لاگ دوباره و ناامن ایجاد نشود. validation message و AttemptedValue خام به کاربر بازگردانده نمی‌شوند؛ پیام عمومی فارسی به تفکیک فیلد است. Error.Message فقط برای پیام بازبینی‌شده و امن قابل استفاده است.

Cancellation به مرز transport واگذار می‌شود و handler آن را به ۵۰۰ یا موفقیت تبدیل نمی‌کند. رفتار نهایی اتصال قطع‌شده تابع ASP.NET Core است.

## محدودیت‌های این مرحله

BusinessId، BranchId و SourceId از header دلخواه خوانده نمی‌شوند. افزودن scope معتبر کسب‌وکار پس از طراحی دسترسی و ماژول‌ها انجام می‌شود.
لاگ خطای framework خارج از exceptionهای handled یا لاگ‌های نوشته‌شده توسط ماژول آینده به‌صورت خودکار پالایش نمی‌شوند؛ این مرز باید در بازبینی و تست ماژول حفظ شود.
تست‌های آداپتر، persistence و جداسازی داده هنوز موضوع ندارند چون این بخش‌ها ایجاد نشده‌اند.
