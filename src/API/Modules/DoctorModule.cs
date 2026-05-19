using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;
using Contracts.Patient.Response;

namespace API.Modules;

public class DoctorModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("doctor")
            .RequireAuthorization(p => p.RequireRole("Doctor"));

        // === Doctor Profile ===
        group.MapGet("/get-profile/{doctorId:guid}", GetDoctorProfile)
            .Produces<ApiSuccessResponse<DoctorProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetDoctorProfile")
            .WithOpenApi();

        // === Patient Management (Doctor perspective) ===
        group.MapGet("/{doctorId:guid}/get-all-patients", GetAllPatients)
            .Produces<ApiSuccessResponse<List<PatientResponse>>>(StatusCodes.Status200OK)
            .WithName("GetAllPatients")
            .WithOpenApi();

        group.MapGet("/{doctorId:guid}/get-patient/{patientId:guid}", GetPatient)
            .Produces<ApiSuccessResponse<PatientResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetPatientById")
            .WithOpenApi();

        // === Link Requests ===
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

        group.MapPost("/requests/{requestId:guid}/unlink", UnlinkPatient)
            .Produces<ApiSuccessResponse<LinkRequestResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("UnlinkPatient")
            .WithOpenApi();
    }

    // === Doctor Profile ===

    private static async Task<IResult> GetDoctorProfile(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid doctorId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"DoctorId: {doctorId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetDoctorProfileQuery(doctorId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Patient Management ===

    private static async Task<IResult> GetAllPatients(
        ISender sender,
        IMapper mapper,
        HttpContext httpContext,
        [FromRoute] Guid doctorId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"Doctor ID: {doctorId} ";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            PatientByDoctorIdQuery query = new(doctorId);
            ErrorOr<List<PatientResult>> result = await sender.Send(query);

            return result.Match(
                value =>
                {
                    List<PatientResponse> response = mapper.Map<List<PatientResponse>>(value);
                    return ApiResults.Success(response, fullRoute);
                },
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            ExceptionError = ex;
        }

        return ApiResults.Error(ExceptionError, fullRoute, parametros);
    }

    private static async Task<IResult> GetPatient(
        ISender sender,
        IMapper mapper,
        HttpContext httpContext,
        [FromRoute] Guid doctorId,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"DoctorId: {doctorId}, PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            PatientByIdQuery query = new(patientId);
            ErrorOr<PatientResult> result = await sender.Send(query);

            return result.Match(
                value =>
                {
                    PatientResponse response = mapper.Map<PatientResponse>(value);
                    return ApiResults.Success(response, fullRoute);
                },
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            ExceptionError = ex;
        }

        return ApiResults.Error(ExceptionError, fullRoute, parametros);
    }

    // === Link Requests ===

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

    private static async Task<IResult> UnlinkPatient(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid requestId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"RequestId: {requestId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new UnlinkPatientCommand(requestId));

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
