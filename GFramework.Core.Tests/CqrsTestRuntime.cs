using System.Reflection;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Ioc;
using GFramework.Core.Logging;
using GfCqrs = GFramework.Core.Abstractions.Cqrs;

namespace GFramework.Core.Tests;

internal static class CqrsTestRuntime
{
    private static readonly MethodInfo RegisterHandlersMethod = typeof(ArchitectureContext).Assembly
        .GetType("GFramework.Core.Cqrs.Internal.CqrsHandlerRegistrar", throwOnError: true)!
        .GetMethod(
            "RegisterHandlers",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
        ?? throw new InvalidOperationException("Failed to locate CqrsHandlerRegistrar.RegisterHandlers.");

    public static void RegisterHandlers(MicrosoftDiContainer container, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(assemblies);

        var logger = LoggerFactoryResolver.Provider.CreateLogger(nameof(CqrsTestRuntime));
        RegisterHandlersMethod.Invoke(
            null,
            [container, assemblies.Where(static assembly => assembly is not null).Distinct().ToArray(), logger]);
    }

    public static ValueTask<TResponse> ExecutePipelineAsync<TRequest, TResponse>(
        IArchitectureContext context,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class, GfCqrs.IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        var handlers = context.GetServices<GfCqrs.IRequestHandler<TRequest, TResponse>>();
        if (handlers.Count == 0)
            throw new InvalidOperationException(
                $"No CQRS request handler registered for {typeof(TRequest).FullName}.");

        if (handlers.Count > 1)
            throw new InvalidOperationException(
                $"Expected a single CQRS request handler for {typeof(TRequest).FullName}, but found {handlers.Count}.");

        var handler = handlers[0];
        PrepareContext(handler, context);

        GfCqrs.MessageHandlerDelegate<TRequest, TResponse> pipeline = handler.Handle;

        var behaviors = context.GetServices<GfCqrs.IPipelineBehavior<TRequest, TResponse>>();
        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            PrepareContext(behavior, context);

            var next = pipeline;
            pipeline = (message, token) => behavior.Handle(message, next, token);
        }

        return pipeline(request, cancellationToken);
    }

    private static void PrepareContext(object instance, IArchitectureContext context)
    {
        if (instance is IContextAware contextAware)
            contextAware.SetContext(context);
    }
}
