namespace Application.Common.Errors;

public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("Auth.InvalidCredentials", "Email o contraseña incorrectos");

    public static readonly Error AccountInactive = Error.Forbidden(
        code: "Auth.AccountInactive",
        description: "La cuenta está inactiva.");

    public static readonly Error EmailAlreadyExists = Error.Conflict(
        code: "Auth.EmailAlreadyExists",
        description: "El correo electrónico ya está registrado.");

    public static readonly Error Forbidden = Error.Forbidden(
        code: "Auth.Forbidden",
        description: "No tienes permiso para acceder a este recurso.");

    public static readonly Error TooManyFailedAttempts = Error.Forbidden(
        code: "Auth.TooManyFailedAttempts",
        description: "Cuenta bloqueada temporalmente por demasiados intentos fallidos. Intenta de nuevo en 15 minutos.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        code: "Auth.InvalidRefreshToken",
        description: "El refresh token es inválido o ha expirado.");
}
