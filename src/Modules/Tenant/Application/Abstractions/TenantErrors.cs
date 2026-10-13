using BazarKav.BuildingBlocks.Application.Abstractions;

namespace BazarKav.Tenant.Application.Abstractions;

public static class TenantErrors
{
    public static readonly Error NotFound = new("tenant.not_found", "کسب‌وکار پیدا نشد.", ErrorKind.NotFound);
    public static readonly Error DuplicateCode = new("tenant.duplicate_code", "کد کسب‌وکار قبلاً ثبت شده است.", ErrorKind.Conflict);
    public static readonly Error Concurrency = new("tenant.version_conflict", "اطلاعات تغییر کرده است؛ دوباره دریافت کنید.", ErrorKind.Conflict);

    public static Error FromCommit(TenantCommitOutcome outcome) => outcome switch
    {
        TenantCommitOutcome.DuplicateCode => DuplicateCode,
        TenantCommitOutcome.ConcurrencyConflict => Concurrency,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };
}
