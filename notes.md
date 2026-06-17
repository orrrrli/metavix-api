# Versionamiento

- Agregar API de versionamiento
- Ejemplo: /api/v1/usuarios, /api/v2/usuarios
- ASP.NET CORE tiene soporte para versionamiento de API.
- Ejemplo de implementación:
  - Instalar el paquete Microsoft.AspNetCore.Mvc.Versioning
  - Configurar en Startup.cs:
    ```csharp
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });
    ```
    
- Controladores:
- [ApiVersion("1.0")]
```cs
  [Route("api/v{version:apiVersion}/[controller]")]
  public class UsuariosController : ControllerBase
  {
      // Métodos para la versión 1.0
  }
  ```


```cs
[ApiVersion("2.0")]
 [Route("api/v{version:apiVersion}/[controller]")]
  public class UsuariosV2Controller : ControllerBase
  {
      // Métodos para la versión 2.0
  }
  ```

- RateLimiting
- Agregar API de RateLimiting para limitar la cantidad de solicitudes a la API.
- Ejemplo de implementación:
- Instalar el paquete AspNetCoreRateLimit
- Configurar en Startup.cs:
```csharp
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule 
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m"
        }
    };
});
