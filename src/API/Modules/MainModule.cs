namespace API.Modules;

public class MainModule
{
    private static Exception? _exceptionError;

    protected static Exception? ExceptionError { get => _exceptionError; set => _exceptionError = value; }
}
