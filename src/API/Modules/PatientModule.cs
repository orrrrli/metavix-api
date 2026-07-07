using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Doctor.Common;
using Application.UseCases.Doctor.Queries;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.LinkRequest.Queries;
using Application.UseCases.DailyRecord.Commands;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;
using Application.UseCases.LabResult.Commands;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Queries;
using Application.UseCases.InsulinDm1.Commands;
using Application.UseCases.InsulinDm1.Common;
using Application.UseCases.InsulinDm1.Queries;
using Application.UseCases.Goals.Commands;
using Application.UseCases.Goals.Common;
using Application.UseCases.Patient.Commands;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using Contracts.Patient.Request;
using Domain.Enums;

namespace API.Modules;

public class PatientModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("patient")
            .RequireAuthorization(p => p.RequireRole("Patient"));

        // === Doctor Discovery (Patient perspective) ===
        group.MapGet("/get-all-doctors", GetAllDoctors)
            .Produces<ApiSuccessResponse<List<DoctorResult>>>(StatusCodes.Status200OK)
            .WithName("GetAllDoctors")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/get-linked-doctors", GetLinkedDoctors)
            .Produces<ApiSuccessResponse<List<LinkedDoctorResult>>>(StatusCodes.Status200OK)
            .WithName("GetLinkedDoctors")
            .WithOpenApi();

        // === Link Requests ===
        group.MapPost("/requests-link", SendLinkRequest)
            .Produces<ApiSuccessResponse<LinkRequestResult>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("SendLinkRequest")
            .WithOpenApi();

        group.MapPost("/requests/{requestId:guid}/revoke", RevokeDoctorAccess)
            .Produces<ApiSuccessResponse<LinkRequestResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("RevokeDoctorAccess")
            .WithOpenApi();

        // === Patient Profile ===
        group.MapGet("/me", GetMyPatientProfile)
            .Produces<ApiSuccessResponse<PatientProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetMyPatientProfile")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/profile", GetPatientProfile)
            .Produces<ApiSuccessResponse<PatientProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetPatientProfile")
            .WithOpenApi();

        group.MapPatch("/{patientId:guid}/profile", UpdatePatientProfile)
            .Produces<ApiSuccessResponse<PatientProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("UpdatePatientProfile")
            .WithOpenApi();

        // === Clinical Summary ===
        group.MapGet("/{patientId:guid}/summary", GetPatientSummary)
            .Produces<ApiSuccessResponse<PatientResumenResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetPatientSummary")
            .WithOpenApi();

        // === Daily Records ===
        group.MapPost("/{patientId:guid}/records/daily", AddDailyRecord)
            .Produces<ApiSuccessResponse<DailyRecordResult>>(StatusCodes.Status201Created)
            .WithName("AddDailyRecord")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/get-all/records/daily", GetPatientDailyRecords)
            .Produces<ApiSuccessResponse<List<DailyRecordResult>>>(StatusCodes.Status200OK)
            .WithName("GetPatientDailyRecords")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/records/daily/snapshot", GetDailyRecordSnapshot)
            .Produces<ApiSuccessResponse<DailyRecordSnapshotResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("GetDailyRecordSnapshot")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/record/daily/{recordId:guid}", GetDailyRecordById)
            .Produces<ApiSuccessResponse<DailyRecordResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetDailyRecordById")
            .WithOpenApi();

        // === Lab Results ===
        group.MapPost("/{patientId:guid}/records/lab", AddLabResult)
            .Produces<ApiSuccessResponse<LabResultResult>>(StatusCodes.Status201Created)
            .WithName("AddLabResult")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/get-all/records/lab", GetPatientLabResults)
            .Produces<ApiSuccessResponse<List<LabResultResult>>>(StatusCodes.Status200OK)
            .WithName("GetPatientLabResults")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/records/lab/{recordId:guid}", GetLabResultById)
            .Produces<ApiSuccessResponse<LabResultResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetLabResultById")
            .WithOpenApi();

        // === Insulin DM1 Tool ===
        group.MapPut("/{patientId:guid}/insulin-dm1/profile", UpsertInsulinProfile)
            .Produces<ApiSuccessResponse<InsulinDm1ProfileResult>>(StatusCodes.Status200OK)
            .WithName("UpsertInsulinProfile")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/insulin-dm1/profile", GetInsulinProfile)
            .Produces<ApiSuccessResponse<InsulinDm1ProfileResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetInsulinProfile")
            .WithOpenApi();

        group.MapPost("/{patientId:guid}/insulin-dm1/records", AddInsulinRecord)
            .Produces<ApiSuccessResponse<InsulinDm1RecordResult>>(StatusCodes.Status201Created)
            .WithName("AddInsulinRecord")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/insulin-dm1/records", GetInsulinRecords)
            .Produces<ApiSuccessResponse<List<InsulinDm1RecordResult>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetInsulinRecords")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/insulin-dm1/records/{recordId:guid}", GetInsulinRecordById)
            .Produces<ApiSuccessResponse<InsulinDm1RecordResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetInsulinRecordById")
            .WithOpenApi();

        group.MapDelete("/{patientId:guid}/insulin-dm1/records/{recordId:guid}", DeleteInsulinRecord)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("DeleteInsulinRecord")
            .WithOpenApi();

        // === Goal Evaluations ===
        group.MapPost("/{patientId:guid}/goal-evaluations", EvaluateGoals)
            .Produces<ApiSuccessResponse<EvaluateGoalsResult>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden)
            .WithName("EvaluateGoals")
            .WithOpenApi();
    }

    // === Doctor Discovery ===

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

    private static async Task<IResult> GetLinkedDoctors(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetLinkedDoctorsQuery(patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Link Requests ===

    private static async Task<IResult> SendLinkRequest(
        ISender sender,
        HttpContext httpContext,
        [FromBody] SendLinkRequestCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {command.PatientId}, DoctorId: {command.DoctorId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            ErrorOr<LinkRequestResult> result = await sender.Send(command);

            return result.Match(
                value => TypedResults.Created($"/api/patient/requests/{value.RequestId}", new ApiSuccessResponse<LinkRequestResult> { Data = value }),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> RevokeDoctorAccess(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid requestId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"RequestId: {requestId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new RevokeDoctorAccessCommand(requestId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Daily Records ===

    private static async Task<IResult> AddDailyRecord(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromBody] AddDailyRecordCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var commandWithId = command with { PatientId = patientId };
            ErrorOr<DailyRecordResult> result = await sender.Send(commandWithId);

            return result.Match(
                value => TypedResults.Created($"/api/patient/{patientId}/record/daily/{value.Id}", new ApiSuccessResponse<DailyRecordResult> { Data = value }),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetPatientDailyRecords(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}, From: {from}, To: {to}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetPatientDailyRecordsQuery(patientId, from, to));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetDailyRecordSnapshot(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromQuery] DateOnly date)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}, Date: {date}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetDailyRecordSnapshotQuery(patientId, date));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetDailyRecordById(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromRoute] Guid recordId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}, RecordId: {recordId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetDailyRecordByIdQuery(patientId, recordId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Lab Results ===

    private static async Task<IResult> AddLabResult(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromBody] AddLabResultCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var commandWithId = command with { PatientId = patientId };
            ErrorOr<LabResultResult> result = await sender.Send(commandWithId);

            return result.Match(
                value => TypedResults.Created($"/api/patient/{patientId}/records/lab/{value.Id}", new ApiSuccessResponse<LabResultResult> { Data = value }),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetPatientLabResults(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetPatientLabResultsQuery(patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetLabResultById(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromRoute] Guid recordId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}, RecordId: {recordId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetLabResultByIdQuery(patientId, recordId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Insulin DM1 Tool ===

    private static async Task<IResult> UpsertInsulinProfile(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromBody] UpsertInsulinProfileCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var commandWithId = command with { PatientId = patientId };
            var result = await sender.Send(commandWithId);

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetInsulinProfile(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetInsulinProfileQuery(patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> AddInsulinRecord(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromBody] AddInsulinRecordCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var commandWithId = command with { PatientId = patientId };
            ErrorOr<InsulinDm1RecordResult> result = await sender.Send(commandWithId);

            return result.Match(
                value => TypedResults.Created($"/api/patient/{patientId}/insulin-dm1/records/{value.Id}", new ApiSuccessResponse<InsulinDm1RecordResult> { Data = value }),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetInsulinRecords(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetInsulinRecordsQuery(patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> GetInsulinRecordById(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromRoute] Guid recordId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}, RecordId: {recordId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetInsulinRecordByIdQuery(patientId, recordId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Patient Profile ===

    private static async Task<IResult> GetMyPatientProfile(
        ISender sender,
        HttpContext httpContext)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        LoggingHelper.LogRequest(fullRoute, "");

        try
        {
            var result = await sender.Send(new GetMyPatientProfileQuery());

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, "");
        }
    }

    private static async Task<IResult> GetPatientProfile(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetPatientProfileQuery(patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> UpdatePatientProfile(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromBody] UpdatePatientProfileRequest request)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var command = new UpdatePatientProfileCommand(
                patientId,
                request.IsPregnant,
                request.HeightCm,
                request.Phone,
                request.PregnancyStartDate,
                request.PregnancyDueDate);

            var result = await sender.Send(command);

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Clinical Summary ===

    private static async Task<IResult> GetPatientSummary(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetPatientResumenQuery(patientId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    // === Goal Evaluations ===

    private static async Task<IResult> EvaluateGoals(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var command = new EvaluateGoalsCommand(patientId, EvaluationTrigger.Patient);
            ErrorOr<EvaluateGoalsResult> result = await sender.Send(command);

            return result.Match(
                value => TypedResults.Created(
                    $"/api/patient/{patientId}/goal-evaluations/{value.EvaluationId}",
                    new ApiSuccessResponse<EvaluateGoalsResult> { Data = value }),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> DeleteInsulinRecord(
        ISender sender,
        HttpContext httpContext,
        [FromRoute] Guid patientId,
        [FromRoute] Guid recordId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}, RecordId: {recordId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new DeleteInsulinRecordCommand(patientId, recordId));

            return result.Match(
                _ => TypedResults.NoContent(),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }
}