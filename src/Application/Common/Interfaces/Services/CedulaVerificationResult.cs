namespace Application.Common.Interfaces.Services;

public sealed record CedulaVerificationResult(
    string Nombre,
    string ApellidoPaterno,
    string ApellidoMaterno,
    string Institucion,
    string Carrera);
