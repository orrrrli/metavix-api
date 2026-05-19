using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Patient.Common;
using Application.UseCases.Patient.Queries;
using Application.UseCases.LinkRequest.Commands;
using Application.UseCases.LinkRequest.Common;
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
}