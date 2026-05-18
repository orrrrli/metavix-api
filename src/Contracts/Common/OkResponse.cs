namespace Contracts.Common;


public class GenericOkResponse<T>(T data)
{
    public T Data { get; set; } = data;
}