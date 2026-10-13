# بازارکاو | BazarKav

برنامه تحلیل فروش، رفتار مشتری و موجودی برای چند کسب‌وکار و شعب آن‌ها، با دریافت داده از منابع حسابداری مختلف.

- [سند معماری](docs/architecture/ARCHITECTURE.md)
- [گاردهای معماری](docs/architecture/GUARDS.md)
- [وضعیت فعلی](docs/planning/CURRENT-STATE.md)
- [دستور کار عامل‌ها](AGENTS.md)

وضعیت: پایه مشترک Application، Observability و Web پیاده‌سازی شده؛ ماژول‌های کسب‌وکار هنوز ایجاد نشده‌اند.

## اجرای بررسی‌ها

```bash
python3 tools/architecture/check_architecture.py
dotnet restore BazarKav.sln --locked-mode
dotnet build BazarKav.sln -c Release --no-restore
dotnet test BazarKav.sln -c Release --no-build --no-restore
```

[تصمیم‌های BuildingBlocks و نسخه‌ها](docs/architecture/ADR-001-BUILDING-BLOCKS.md)
