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

    public static readonly Error InvalidOrExpiredResetToken = Error.Validation(
        code: "Auth.InvalidOrExpiredResetToken",
        description: "El enlace para restablecer contraseña es inválido o ha expirado.");

    public static readonly Error GoogleAccountOnly = Error.Validation(
        code: "Auth.GoogleAccountOnly",
        description: "Esta cuenta fue creada con Google. Usa el botón 'Continuar con Google' para acceder.");

    public static readonly Error GoogleOAuthFailed = Error.Failure(
        code: "Auth.GoogleOAuthFailed",
        description: "No se pudo completar el inicio de sesión con Google.");
}
