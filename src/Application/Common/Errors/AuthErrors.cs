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
}
