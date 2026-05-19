using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Auth.Login;
using Contracts.Auth;

namespace API.Modules;

public class AuthModule : MainModule, ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("auth");

        group.MapPost("/login", Login)
            .Produces<ApiSuccessResponse<AuthResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous()
            .WithName("Login")
            .WithOpenApi();
    }

    private static async Task<IResult> Login(
        IMediator mediator,
        HttpContext httpContext,
        [FromBody] LoginRequest request)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"Email: {request.Email}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            LoginCommand command = new(request.Email, request.Password);
            ErrorOr<LoginResult> result = await mediator.Send(command);

            return result.Match(
                value =>
                {
                    AuthResponse response = new(
                        value.AccessToken,
                        value.ExpiresAt,
                        value.Email,
                        value.Role,
                        value.FullName);
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
}
