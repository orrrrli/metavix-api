using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;

namespace API.Modules;

public class DoctorModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("doctor");

        group.MapGet("/get-all", GetAllDoctors)
            .Produces<ApiSuccessResponse<List<DoctorResult>>>(StatusCodes.Status200OK)
            .WithName("GetAllDoctors")
            .WithOpenApi();

        group.MapGet("/requests/pending/{doctorId:guid}", GetPendingRequests)
            .Produces<ApiSuccessResponse<List<PendingRequestResult>>>(StatusCodes.Status200OK)
            .WithName("GetPendingRequests")
            .WithOpenApi();

        group.MapPost("/requests/{requestId:guid}/accept", AcceptLinkRequest)
            .Produces<ApiSuccessResponse<LinkRequestResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("AcceptLinkRequest")
            .WithOpenApi();

        group.MapPost("/requests/{requestId:guid}/reject", RejectLinkRequest)
            .Produces<ApiSuccessResponse<LinkRequestResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("RejectLinkRequest")
            .WithOpenApi();
    }

    private static async Task<IResult> GetAllDoctors(
        ISender sender,
        HttpContext httpContext)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        LoggingHelper.LogRequest(fullRoute, "");

        try
        {
            var result = await sender.Send(new GetAllDoctorsQuery());

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, "");
        }
    }

    private static async Task<IResult> GetPendingRequests(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid doctorId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"DoctorId: {doctorId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetPendingRequestsQuery(doctorId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> AcceptLinkRequest(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid requestId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"RequestId: {requestId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new AcceptLinkRequestCommand(requestId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> RejectLinkRequest(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid requestId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"RequestId: {requestId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new RejectLinkRequestCommand(requestId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }
}
