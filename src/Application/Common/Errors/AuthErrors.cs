namespace Application.Common.Errors;

public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("Auth.InvalidCredentials", "Email o contraseña incorrectos");

    public static Error AccountInactive =>
        Error.Forbidden("Auth.AccountInactive", "La cuenta está desactivada");
}
