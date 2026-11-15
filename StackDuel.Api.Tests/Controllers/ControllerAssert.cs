using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using StackDuel.Application;
using StackDuel.Application.Pagination;
using StackDuel.Application.Users.Dtos;

namespace StackDuel.Api.Tests.Controllers;

internal static class ControllerAssert
{
    internal static int StatusCodeOf<T>(ActionResult<T> result) =>
        result.Result switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            null => StatusCodes.Status200OK,
            _ => -1,
        };

    internal static TValue ValueOf<TValue>(ActionResult<TValue> result) =>
        result.Result is ObjectResult objectResult ? (TValue)objectResult.Value! : result.Value!;

    internal static UserContext AuthenticatedContext(Guid? userId = null) =>
        new()
        {
            User = new UserDto(
                userId ?? Guid.NewGuid(),
                "auth0|sub",
                "player",
                null,
                null,
                false,
                null,
                DateTime.UtcNow,
                null,
                []
            ),
        };

    internal static UserContext AnonymousContext() => new();

    internal static TController WithHttpContext<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            ActionDescriptor = new ControllerActionDescriptor(),
        };
        return controller;
    }

    internal static PageResult<T> Page<T>(params T[] items) =>
        new()
        {
            Results = items,
            Total = items.Length,
            Page = 1,
            Size = 20,
        };
}