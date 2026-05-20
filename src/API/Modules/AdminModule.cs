using API.Common;
using Application.Common.Models;
using Application.UseCases.Admin.Common;
using Application.UseCases.Admin.Queries;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace API.Modules;

public class AdminModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("admin")
            .RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapGet("/logs", GetLogs)
            .Produces<ApiSuccessResponse<PaginatedResult<LogEntryResult>>>(StatusCodes.Status200OK)
            .WithName("GetLogs")
            .WithOpenApi();

        group.MapGet("/logs/{correlationId}", GetLogsByCorrelationId)
            .Produces<ApiSuccessResponse<List<LogEntryResult>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetLogsByCorrelationId")
            .WithOpenApi();
    }

    private static async Task<IResult> GetLogs(
        ISender sender,
        [FromQuery] string? level = null,
        [FromQuery] string? endpoint = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await sender.Send(new GetLogsQuery(level, endpoint, userId, from, to, page, pageSize));

        return result.Match(
            value => ApiResults.Success(value, "/admin/logs", isPaginated: true),
            errors => ApiResults.Problem(errors, "/admin/logs"));
    }

    private static async Task<IResult> GetLogsByCorrelationId(
        ISender sender,
        [FromRoute] string correlationId)
    {
        var result = await sender.Send(new GetLogsByCorrelationIdQuery(correlationId));

        return result.Match(
            value => ApiResults.Success(value, $"/admin/logs/{correlationId}"),
            errors => ApiResults.Problem(errors, $"/admin/logs/{correlationId}"));
    }
}
