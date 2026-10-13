using BazarKav.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace BazarKav.BuildingBlocks.Web.Errors;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext context) =>
        result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(ProblemDetailsMapper.FromError(result.Error, context));
}
