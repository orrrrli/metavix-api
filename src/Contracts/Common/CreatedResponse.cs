namespace Contracts.Common;

public class CreatedResponse(int successResultId, string message = "Registro creado con éxito")
{
    public int Id { get; set; } = successResultId;
    public string Message { get; set; } = message;
}
