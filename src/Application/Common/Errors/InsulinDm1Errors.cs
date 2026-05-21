namespace Application.Common.Errors;

public static class InsulinDm1Errors
{
    public static readonly Error ProfileNotFound = Error.NotFound(
        code: "InsulinDm1.ProfileNotFound",
        description: "El perfil de insulina no fue encontrado.");

    public static readonly Error RecordNotFound = Error.NotFound(
        code: "InsulinDm1.RecordNotFound",
        description: "El registro de insulina no fue encontrado.");

    public static readonly Error RecordsNotFound = Error.NotFound(
        code: "InsulinDm1.RecordsNotFound",
        description: "No se encontraron registros de insulina.");
}
