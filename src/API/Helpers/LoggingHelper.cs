using ErrorOr;
using Serilog;

namespace API.Helpers;


    public static class LoggingHelper
    {
        public static void LogRequest(string route, params object[] parameters)
        {
            Log.Information(
                "Petición Enviada. 💡 Ruta: {Route}, {Parameters}, Fecha-Hora: {Timestamp}",
                route, FormatParameters(parameters), DateTime.Now);
        }

        public static void LogSuccess(string route, object response)
        {
            Log.Information(
                "Petición Exitosa. ✅ Ruta: {Route}, Respuesta: {@Response}, Fecha-Hora: {Timestamp}",
                route, response, DateTime.Now);
        }

        public static void LogWarning(string route, List<Error> errores)
        {
            Log.Warning(
                "Petición Fallida. ⚠️ Ruta: {Route}, Errores: {@Errores}, Fecha-Hora: {Timestamp}",
                route, errores, DateTime.Now);
        }

        public static void LogError(string route, Exception ex, params object[] parameters)
        {
            Log.Error(
                ex,
                "Error inesperado durante la petición. ❌ Ruta: {Route}, {Parameters}, Fecha-Hora: {FechaHora}",
                route, FormatParameters(parameters), DateTime.Now);
        }

        private static string FormatParameters(params object[] parameters)
        {
            return string.Join(", ", parameters.Select(p => p.ToString()));
        }
    }