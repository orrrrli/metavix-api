using Carter;

using Microsoft.AspNetCore.Mvc;

using API.Common;
using API.Helpers;
using Application.Common.Interfaces.Services;
using Application.UseCases.Auth.Commands;
using Application.UseCases.Auth.Common;
using Application.UseCases.Auth.Queries;
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
            .Produces(StatusCodes.Status429TooManyRequests)
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .WithName("Login")
            .WithOpenApi();

        group.MapPost("/register/patient", RegisterPatient)
            .Produces<ApiSuccessResponse<RegisterResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status429TooManyRequests)
            .AllowAnonymous()
            .RequireRateLimiting("register")
            .WithName("RegisterPatient")
            .WithOpenApi();

        group.MapPost("/register/doctor", RegisterDoctor)
            .Produces<ApiSuccessResponse<RegisterResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status429TooManyRequests)
            .AllowAnonymous()
            .RequireRateLimiting("register")
            .WithName("RegisterDoctor")
            .WithOpenApi();

        group.MapPost("/refresh", Refresh)
            .Produces<ApiSuccessResponse<RefreshResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous()
            .WithName("Refresh")
            .WithOpenApi();

        group.MapPost("/logout", Logout)
            .Produces(StatusCodes.Status200OK)
            .AllowAnonymous()
            .WithName("Logout")
            .WithOpenApi();

        group.MapPost("/forgot-password", ForgotPassword)
            .Produces(StatusCodes.Status200OK)
            .AllowAnonymous()
            .WithName("ForgotPassword")
            .WithOpenApi();

        group.MapPost("/reset-password", ResetPassword)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous()
            .WithName("ResetPassword")
            .WithOpenApi();

        group.MapGet("/google", InitiateGoogleLogin)
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status400BadRequest)
            .AllowAnonymous()
            .WithName("GoogleLogin")
            .WithOpenApi();

        group.MapGet("/google/callback", HandleGoogleCallback)
            .Produces(StatusCodes.Status302Found)
            .AllowAnonymous()
            .WithName("GoogleCallback")
            .WithOpenApi();

        group.MapGet("/me", GetMe)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .WithName("Me")
            .WithOpenApi();
    }

    private static CookieOptions AccessTokenCookie() => new()
    {
        HttpOnly = true,
        Secure   = true,
        SameSite = SameSiteMode.None,
        Expires  = DateTimeOffset.UtcNow.AddMinutes(15),
    };

    private static CookieOptions RefreshTokenCookie() => new()
    {
        HttpOnly = true,
        Secure   = true,
        SameSite = SameSiteMode.None,
        Expires  = DateTimeOffset.UtcNow.AddDays(7),
        Path     = "/api/v1/auth",
    };

    private const string SessionCookieName = "_session";

    private static CookieOptions SessionCookie(IConfiguration configuration) => new()
    {
        HttpOnly = true,
        Secure   = true,
        SameSite = SameSiteMode.None,
        Path     = "/",
        Expires  = DateTimeOffset.UtcNow.AddMinutes(15),
        Domain   = string.IsNullOrWhiteSpace(configuration["Auth:SessionCookieDomain"])
            ? null
            : configuration["Auth:SessionCookieDomain"],
    };

    private static CookieOptions DeleteSessionCookieOptions(IConfiguration configuration) => new()
    {
        Secure   = true,
        SameSite = SameSiteMode.None,
        Path     = "/",
        Domain   = string.IsNullOrWhiteSpace(configuration["Auth:SessionCookieDomain"])
            ? null
            : configuration["Auth:SessionCookieDomain"],
    };

    private static async Task<IResult> Login(
        ISender sender,
        HttpContext httpContext,
        IConfiguration configuration,
        [FromBody] LoginRequest request)
    {
        string fullRoute  = $"{httpContext.Request.Path}";
        string parametros = $"Email: {request.Email}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            LoginCommand command = new(request.Email, request.Password);
            ErrorOr<LoginResult> result = await sender.Send(command);

            return result.Match(
                value =>
                {
                    httpContext.Response.Cookies.Append("access_token",  value.AccessToken,  AccessTokenCookie());
                    httpContext.Response.Cookies.Append("refresh_token", value.RefreshToken, RefreshTokenCookie());
                    httpContext.Response.Cookies.Append(SessionCookieName, "1",                 SessionCookie(configuration));

                    AuthResponse response = new(
                        value.UserId,
                        value.PatientId,
                        value.DoctorId,
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

    private static async Task<IResult> Refresh(
        ISender sender,
        HttpContext httpContext,
        IConfiguration configuration)
    {
        string fullRoute = $"{httpContext.Request.Path}";

        try
        {
            string? refreshToken = httpContext.Request.Cookies["refresh_token"];

            if (string.IsNullOrEmpty(refreshToken))
                return ApiResults.Problem([Application.Common.Errors.AuthErrors.InvalidRefreshToken], fullRoute);

            RefreshCommand command = new(refreshToken);
            ErrorOr<RefreshResult> result = await sender.Send(command);

            return result.Match(
                value =>
                {
                    httpContext.Response.Cookies.Append("access_token",  value.AccessToken,     AccessTokenCookie());
                    httpContext.Response.Cookies.Append("refresh_token", value.NewRefreshToken, RefreshTokenCookie());
                    httpContext.Response.Cookies.Append(SessionCookieName, "1",                  SessionCookie(configuration));

                    return ApiResults.Success(new RefreshResponse(value.ExpiresAt), fullRoute);
                },
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, string.Empty);
        }
    }

    private static async Task<IResult> Logout(
        ISender sender,
        HttpContext httpContext,
        IConfiguration configuration)
    {
        string fullRoute = $"{httpContext.Request.Path}";

        try
        {
            string? refreshToken = httpContext.Request.Cookies["refresh_token"];

            if (!string.IsNullOrEmpty(refreshToken))
                await sender.Send(new LogoutCommand(refreshToken));

            httpContext.Response.Cookies.Delete("access_token",  new CookieOptions { SameSite = SameSiteMode.None, Secure = true });
            httpContext.Response.Cookies.Delete("refresh_token", new CookieOptions { SameSite = SameSiteMode.None, Secure = true, Path = "/api/auth" });
            httpContext.Response.Cookies.Delete(SessionCookieName, DeleteSessionCookieOptions(configuration));

            return Results.Ok();
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, string.Empty);
        }
    }

    private static async Task<IResult> RegisterPatient(
        ISender sender,
        HttpContext httpContext,
        IConfiguration configuration,
        [FromBody] RegisterPatientCommand command)
    {
        string fullRoute  = $"{httpContext.Request.Path}";
        string parametros = $"Email: {command.Email}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            ErrorOr<RegisterResult> result = await sender.Send(command);

            return result.Match(
                value =>
                {
                    httpContext.Response.Cookies.Append("access_token",  value.Token,         AccessTokenCookie());
                    httpContext.Response.Cookies.Append("refresh_token", value.RefreshToken,  RefreshTokenCookie());
                    httpContext.Response.Cookies.Append(SessionCookieName, "1",              SessionCookie(configuration));

                    RegisterResponse response = new(value.UserId, value.Email, value.Role);
                    return TypedResults.Created($"/api/auth/users/{value.UserId}", new ApiSuccessResponse<RegisterResponse> { Data = response });
                },
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> RegisterDoctor(
        ISender sender,
        HttpContext httpContext,
        IConfiguration configuration,
        [FromBody] RegisterDoctorCommand command)
    {
        string fullRoute  = $"{httpContext.Request.Path}";
        string parametros = $"Email: {command.Email}";
        LoggingHelper.LogRequest(fullRoute, parametros);

        try
        {
            ErrorOr<RegisterResult> result = await sender.Send(command);

            return result.Match(
                value =>
                {
                    httpContext.Response.Cookies.Append("access_token",  value.Token,         AccessTokenCookie());
                    httpContext.Response.Cookies.Append("refresh_token", value.RefreshToken,  RefreshTokenCookie());
                    httpContext.Response.Cookies.Append(SessionCookieName, "1",              SessionCookie(configuration));

                    RegisterResponse response = new(value.UserId, value.Email, value.Role);
                    return TypedResults.Created($"/api/auth/users/{value.UserId}", new ApiSuccessResponse<RegisterResponse> { Data = response });
                },
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, parametros);
        }
    }

    private static async Task<IResult> ForgotPassword(
        ISender sender,
        HttpContext httpContext,
        [FromBody] ForgotPasswordCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";

        try
        {
            await sender.Send(command);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, $"Email: {command.Email}");
        }
    }

    private static async Task<IResult> ResetPassword(
        ISender sender,
        HttpContext httpContext,
        [FromBody] ResetPasswordCommand command)
    {
        string fullRoute = $"{httpContext.Request.Path}";

        try
        {
            ErrorOr<Unit> result = await sender.Send(command);

            return result.Match(
                _ => Results.Ok(),
                errors => ApiResults.Problem(errors, fullRoute));
        }
        catch (Exception ex)
        {
            return ApiResults.Error(ex, fullRoute, string.Empty);
        }
    }

    private static IResult InitiateGoogleLogin(
        [FromQuery] string? role,
        IGoogleOAuthService googleOAuthService)
    {
        if (role is not ("patient" or "doctor"))
            return Results.BadRequest("role must be 'patient' or 'doctor'.");

        string authUrl = googleOAuthService.BuildAuthorizationUrl(role, out _);
        return Results.Redirect(authUrl);
    }

    private static async Task<IResult> HandleGoogleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        ISender sender,
        HttpContext httpContext,
        IGoogleOAuthService googleOAuthService,
        IConfiguration configuration)
    {
        string frontendUrl = googleOAuthService.FrontendUrl;

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Results.Redirect($"{frontendUrl}/login?error=oauth_failed");

        try
        {
            ErrorOr<LoginResult> result = await sender.Send(
                new GoogleCallbackCommand(code, state));

            if (result.IsError)
                return Results.Redirect($"{frontendUrl}/login?error=oauth_failed");

            LoginResult login = result.Value;

            httpContext.Response.Cookies.Append("access_token",  login.AccessToken,  AccessTokenCookie());
            httpContext.Response.Cookies.Append("refresh_token", login.RefreshToken, RefreshTokenCookie());
            httpContext.Response.Cookies.Append(SessionCookieName, "1",             SessionCookie(configuration));

            return Results.Redirect($"{frontendUrl}/auth/callback");
        }
        catch
        {
            return Results.Redirect($"{frontendUrl}/login?error=oauth_failed");
        }
    }

    private static async Task<IResult> GetMe(
        ISender sender,
        HttpContext httpContext)
    {
        string fullRoute = $"{httpContext.Request.Path}";

        ErrorOr<MeResult> result = await sender.Send(new MeQuery());

        return result.Match(
            value => ApiResults.Success(new MeResponse(
                value.UserId,
                value.PatientId,
                value.DoctorId,
                value.Email,
                value.Role,
                value.FullName), fullRoute),
            errors => ApiResults.Problem(errors, fullRoute));
    }
}
