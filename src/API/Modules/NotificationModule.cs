using API.Common;
using API.Helpers;
using Application.UseCases.Notifications.Commands;
using Application.UseCases.Notifications.Common;
using Application.UseCases.Notifications.Queries;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules;

public class NotificationModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("notifications")
            .RequireAuthorization();

        group.MapGet("/", GetMyNotifications)
            .Produces<ApiSuccessResponse<List<NotificationResult>>>(StatusCodes.Status200OK)
            .WithName("GetMyNotifications")
            .WithOpenApi();

        group.MapPost("/{notificationId:guid}/read", MarkNotificationRead)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("MarkNotificationRead")
            .WithOpenApi();
    }

    private static async Task<IResult> GetMyNotifications(
        ISender sender,
        HttpContext httpContext)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        LoggingHelper.LogRequest(fullRoute, "");

        try
        {
            var result = await sender.Send(new GetMyNotificationsQuery());

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, "");
        }
    }

    private static async Task<IResult> MarkNotificationRead(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid notificationId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"NotificationId: {notificationId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new MarkNotificationReadCommand(notificationId));

            return result.Match(
                _ => ApiResults.Success(new { }, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }
}
