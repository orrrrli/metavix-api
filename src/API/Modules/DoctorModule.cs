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
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.LabResult.Common;
using Contracts.Patient.Response;

namespace API.Modules;

public class DoctorModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("doctor")
            .RequireAuthorization(p => p.RequireRole("Doctor"));

        // === Doctor Profile ===
        group.MapGet("/me", GetMyProfile)
            .Produces<ApiSuccessResponse<DoctorProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetMyDoctorProfile")
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

        // === Linked Patient Records ===
        group.MapGet("/{doctorId:guid}/patients/{patientId:guid}/profile", GetLinkedPatientProfile)
            .Produces<ApiSuccessResponse<PatientProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetLinkedPatientProfile")
            .WithOpenApi();

        group.MapGet("/{doctorId:guid}/patients/{patientId:guid}/records/daily", GetLinkedPatientDailyRecords)
            .Produces<ApiSuccessResponse<List<DailyRecordResult>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetLinkedPatientDailyRecords")
            .WithOpenApi();

        group.MapGet("/{doctorId:guid}/patients/{patientId:guid}/records/lab", GetLinkedPatientLabResults)
            .Produces<ApiSuccessResponse<List<LabResultResult>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetLinkedPatientLabResults")
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

    // === Doctor Me ===

    private static async Task<IResult> GetMyProfile(
        ISender sender,
        HttpContext httpContext)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        LoggingHelper.LogRequest(fullRoute, string.Empty);

        try
        {
            var result = await sender.Send(new GetMyDoctorProfileQuery());

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, string.Empty);
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

    // === Linked Patient Records ===

    private static async Task<IResult> GetLinkedPatientProfile(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid doctorId,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"DoctorId: {doctorId}, PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetLinkedPatientProfileQuery(doctorId, patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetLinkedPatientDailyRecords(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid doctorId,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"DoctorId: {doctorId}, PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetLinkedPatientDailyRecordsQuery(doctorId, patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetLinkedPatientLabResults(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid doctorId,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"DoctorId: {doctorId}, PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetLinkedPatientLabResultsQuery(doctorId, patientId));

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
