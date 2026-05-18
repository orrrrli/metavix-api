using System.Reflection;
using Mapster;
using MapsterMapper;

namespace API.Extensions;

public static class MapperConfig
{
    public static IServiceCollection AddMapping(this IServiceCollection services)
    {
        TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, Mapper>();
        return services;
    }
}