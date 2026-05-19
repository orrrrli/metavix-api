using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
using Application.UseCases.DailyRecord.Commands;
using Application.UseCases.DailyRecord.Common;
using Application.UseCases.DailyRecord.Queries;
using Application.UseCases.LabResult.Commands;
using Application.UseCases.LabResult.Common;
using Application.UseCases.LabResult.Queries;
using Contracts.Patient.Response;

namespace API.Modules;

public class PatientModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("patient");

        group.MapGet("/get-all/{doctorId:guid}", GetAllPatient)
            .Produces<ApiSuccessResponse<List<PatientResponse>>>(StatusCodes.Status200OK)
            .WithName("GetAllPatient")
            .WithOpenApi();
        
        group.MapGet("get/{patientId:guid}", GetPatient)
            .Produces<ApiSuccessResponse<PatientResponse>>(StatusCodes.Status200OK)
            .WithName("GetPatientById")
            .WithOpenApi();

        group.MapPost("/requests-link", SendLinkRequest)
            .Produces<ApiSuccessResponse<LinkRequestResult>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("SendLinkRequest")
            .WithOpenApi();

        // Daily Records
        group.MapPost("/{patientId:guid}/records/daily", AddDailyRecord)
            .Produces<ApiSuccessResponse<DailyRecordResult>>(StatusCodes.Status201Created)
            .WithName("AddDailyRecord")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/get-all/records/daily", GetPatientDailyRecords)
            .Produces<ApiSuccessResponse<List<DailyRecordResult>>>(StatusCodes.Status200OK)
            .WithName("GetPatientDailyRecords")
            .WithOpenApi();

        group.MapGet("/{patientId:guid}/record/daily/{recordId:guid}", GetDailyRecordById)
            .Produces<ApiSuccessResponse<DailyRecordResult>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetDailyRecordById")
            .WithOpenApi();

        // Lab Results
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
    }

    private static async Task<IResult> GetAllPatient(
        IMediator mediator,
        [FromServices] IMapper mapper,
        HttpContext httpContext, 
        [FromRoute] Guid doctorId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"Doctor ID: {doctorId} ";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            PatientByDoctorIdQuery query = new(doctorId);
            ErrorOr<List<PatientResult>> result = await mediator.Send(query);

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
        IMediator mediator,
        [FromServices] IMapper mapper,
        HttpContext httpContext,
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"Doctor ID: {patientId} ";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            PatientByIdQuery query = new(patientId);
            ErrorOr<PatientResult> result = await mediator.Send(query);

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
        [FromRoute] Guid patientId)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"PatientId: {patientId}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            var result = await sender.Send(new GetPatientDailyRecordsQuery(patientId));

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
            var result = await sender.Send(new GetDailyRecordByIdQuery(recordId));

            return result.Match(
                value => ApiResults.Success(value, fullRoute),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

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
            var result = await sender.Send(new GetLabResultByIdQuery(recordId));

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