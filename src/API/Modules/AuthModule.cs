using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
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

        group.MapPost("/register", Register)
            .Produces<ApiSuccessResponse<RegisterResult>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .AllowAnonymous()
            .WithName("Register")
            .WithOpenApi();
    }

    private static async Task<IResult> Login(
        ISender sender,
        HttpContext httpContext,
        [FromBody] LoginRequest request)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"Email: {request.Email}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            LoginCommand command = new(request.Email, request.Password);
            ErrorOr<LoginResult> result = await sender.Send(command);

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
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> Register(
        ISender sender,
        HttpContext httpContext,
        [FromBody] RegisterCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";
        string parametros = $"Email: {command.Email}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            ErrorOr<RegisterResult> result = await sender.Send(command);

            return result.Match(
                value => TypedResults.Created($"/api/auth/users/{value.UserId}", new ApiSuccessResponse<RegisterResult> { Data = value }),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }
}
