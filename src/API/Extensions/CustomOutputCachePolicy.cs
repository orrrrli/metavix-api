using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace API.Extensions;

public sealed class CustomOutputCachePolicy : IOutputCachePolicy
{
    public static readonly CustomOutputCachePolicy Instance = new();

    private CustomOutputCachePolicy()
    {
    }

    ValueTask IOutputCachePolicy.CacheRequestAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Cualquier atributo en las queries o en las variables de ruta de la aplicacion
        // Generan un nuevo cache, esto para no enviar respuestas repetidas en diferentes peticiones
        context.CacheVaryByRules.QueryKeys = "*";
        context.CacheVaryByRules.RouteValueNames = "*";

        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeFromCacheAsync(
        OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeResponseAsync(
        OutputCacheContext context, CancellationToken cancellationToken)
    {
        HttpResponse response = context.HttpContext.Response;

        // Si tiene cookies, no lo almacena
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // Verifica el codigo de respuesta, solo almacena las respuestas con codigo 200
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // Se le asigna una duración de 2 minutos
        context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(2);

        return ValueTask.CompletedTask;
    }

    // Verifica si la peticion tiene los requisitos para ser almacenada en el cache
    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        HttpRequest request = context.HttpContext.Request;

        // Verifica el metodo, solo envía el cache en GET
        return HttpMethods.IsGet(request.Method);
    }
}
