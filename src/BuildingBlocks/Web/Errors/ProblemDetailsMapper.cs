using System.Diagnostics;
using System.Text.RegularExpressions;
using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.BuildingBlocks.Observability.Correlation;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BazarKav.BuildingBlocks.Web.Errors;

public static class ProblemDetailsMapper
{
    public static ProblemDetails FromStatus(int status, HttpContext context) =>
        Create(context, status, $"http.{status}");

    public static ProblemDetails FromError(Error error, HttpContext context)
    {
        var status = error.Kind switch
        {
            ErrorKind.Validation => 400,
            ErrorKind.Unauthorized => 401,
            ErrorKind.Forbidden => 403,
            ErrorKind.NotFound => 404,
            ErrorKind.Conflict => 409,
            ErrorKind.Unavailable => 503,
            ErrorKind.Timeout => 504,
            _ => throw new ArgumentOutOfRangeException(nameof(error))
        };
        var problem = Create(context, status, error.Code);
        problem.Detail = error.Message;
        return problem;
    }

    public static ProblemDetails FromException(Exception exception, HttpContext context)
    {
        if (exception is OperationCanceledException)
            throw new ArgumentException("Cancellation belongs to the transport boundary.", nameof(exception));
        if (exception is ValidationException validation)
        {
            // Do not echo AttemptedValue, validator message interpolation or raw exception messages.
            var errors = validation.Errors
                .Select(e => SafeField(e.PropertyName))
                .Distinct()
                .ToDictionary(field => field, _ => new[] { "مقدار این فیلد معتبر نیست." });
            var problem = new ValidationProblemDetails(errors)
            {
                Status = 400, Title = Title(400), Type = Type(400)
            };
            AddIdentifiers(problem, context, "validation.failed");
            return problem;
        }
        if (exception is BadHttpRequestException badRequest)
            return Create(context, badRequest.StatusCode is >= 400 and < 500 ? badRequest.StatusCode : 400, "request.invalid");
        return Create(context, 500, "unexpected.error");
    }

    private static ProblemDetails Create(HttpContext context, int status, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = Title(status), Type = Type(status) };
        AddIdentifiers(problem, context, code);
        return problem;
    }

    private static void AddIdentifiers(ProblemDetails problem, HttpContext context, string code)
    {
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] =
            context.Items[CorrelationId.ItemKey] as string ?? context.TraceIdentifier;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    private static string SafeField(string? field) =>
        field is { Length: > 0 and <= 128 } && Regex.IsMatch(field, @"\A[A-Za-z0-9_.\[\]-]+\z")
            ? field : "request";

    private static string Type(int status) => $"https://httpstatuses.io/{status}";
    private static string Title(int status) => status switch
    {
        400 => "اطلاعات درخواست معتبر نیست.",
        401 => "احراز هویت لازم است.",
        403 => "اجازه دسترسی ندارید.",
        404 => "اطلاعات موردنظر پیدا نشد.",
        409 => "در وضعیت فعلی امکان انجام درخواست نیست.",
        413 => "حجم درخواست بیش از حد مجاز است.",
        503 => "منبع اطلاعات موقتاً در دسترس نیست.",
        504 => "زمان انتظار برای منبع اطلاعات به پایان رسید.",
        _ => status >= 500 ? "خطای غیرمنتظره رخ داده است." : "درخواست قابل پردازش نیست."
    };
}
