using Application.Common.Behaviors;
using Application.Common.Interfaces.Services;
using Application.Common.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Registration order = pipeline order (MediatR 11 has no AddOpenBehavior).
        // Validation runs before persistence. ICommand and ITransactionalCommand
        // are mutually exclusive sibling markers (see their remarks), so a given
        // request only ever matches one of PersistenceBehavior/TransactionBehavior.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PersistenceBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.AddSingleton<IEgfrCalculator, EgfrCalculator>();
        return services;
    }

}