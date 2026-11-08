// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GFramework.Core.Abstractions.Architectures;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Cqrs;
using GFramework.Cqrs.Notification;
using GFramework.Cqrs.Tests.Logging;
using Microsoft.Extensions.Logging;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证默认 dispatcher 在上下文注入前置条件不满足时的失败语义。
/// </summary>
[TestFixture]
internal sealed class CqrsDispatcherContextValidationTests
{
    /// <summary>
    ///     验证当 request handler 需要上下文注入、但当前 CQRS 上下文不实现 <see cref="GFramework.Core.Abstractions.Architectures.IArchitectureContext" /> 时，
    ///     dispatcher 会在调用前显式失败。
    /// </summary>
    [Test]
    public void SendAsync_Should_Throw_When_Context_Does_Not_Implement_IArchitectureContext()
    {
        var runtime = CreateRuntime(
            provider =>
            {
                provider.Register(typeof(IRequestHandler<ContextAwareRequest, int>), new ContextAwareRequestHandler());
            });

        Assert.That(
            async () => await runtime.SendAsync(new ContextAwareRequest(), cancellationToken: default, new FakeCqrsContext()).ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.Contains("does not provide an IArchitectureContext"));
    }

    /// <summary>
    ///     验证 request 上下文校验失败时，<see cref="GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime.SendAsync{TResponse}" />
    ///     不会在调用点同步抛出，而是返回一个 faulted <see cref="ValueTask{TResult}" /> 保持既有异步失败语义。
    /// </summary>
    [Test]
    public void SendAsync_Should_Return_Faulted_ValueTask_When_Context_Preparation_Fails()
    {
        var runtime = CreateRuntime(
            provider =>
            {
                provider.Register(typeof(IRequestHandler<ContextAwareRequest, int>), new ContextAwareRequestHandler());
            });

        ValueTask<int> dispatch = default;
        Assert.That(
            () => { dispatch = runtime.SendAsync(new ContextAwareRequest(), cancellationToken: default, new FakeCqrsContext()); },
            Throws.Nothing);
        Assert.That(
            async () => await dispatch.ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.Contains("does not provide an IArchitectureContext"));
    }

    /// <summary>
    ///     验证 request handler 缺失时，dispatcher 仍返回 faulted <see cref="ValueTask{TResult}" />，
    ///     而不是在调用点同步抛出异常。
    /// </summary>
    [Test]
    public void SendAsync_Should_Return_Faulted_ValueTask_When_Handler_Is_Missing()
    {
        var runtime = CreateRuntime(_ => { });

        ValueTask<int> dispatch = default;
        Assert.That(
            () => { dispatch = runtime.SendAsync(new ContextAwareRequest()); },
            Throws.Nothing);
        Assert.That(
            async () => await dispatch.ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.Contains("No CQRS request handler registered"));
    }

    /// <summary>
    ///     验证当 notification handler 需要上下文注入、但当前 CQRS 上下文不实现 <see cref="GFramework.Core.Abstractions.Architectures.IArchitectureContext" /> 时，
    ///     dispatcher 会在发布前显式失败。
    /// </summary>
    [Test]
    public void PublishAsync_Should_Throw_When_Context_Does_Not_Implement_IArchitectureContext()
    {
        var runtime = CreateRuntime(
            provider =>
            {
                provider.Register(typeof(INotificationHandler<ContextAwareNotification>), new ContextAwareNotificationHandler());
            });

        Assert.That(
            async () => await runtime.PublishAsync(new ContextAwareNotification(), cancellationToken: default, new FakeCqrsContext()).ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.Contains("does not provide an IArchitectureContext"));
    }

    /// <summary>
    ///     验证当 stream handler 需要上下文注入、但当前 CQRS 上下文不实现 <see cref="GFramework.Core.Abstractions.Architectures.IArchitectureContext" /> 时，
    ///     dispatcher 会在建流前显式失败。
    /// </summary>
    [Test]
    public void CreateStream_Should_Throw_When_Context_Does_Not_Implement_IArchitectureContext()
    {
        var runtime = CreateRuntime(
            provider =>
            {
                provider.Register(typeof(IStreamRequestHandler<ContextAwareStreamRequest, int>), new ContextAwareStreamHandler());
            });

        Assert.That(
            () => runtime.CreateStream(new ContextAwareStreamRequest(), cancellationToken: default, new FakeCqrsContext()),
            Throws.InvalidOperationException.With.Message.Contains("does not provide an IArchitectureContext"));
    }

    /// <summary>
    ///     验证 stream handler 缺失时，dispatcher 会在建流调用点同步抛出异常，
    ///     而不是返回一个延迟到枚举阶段才失败的异步流。
    /// </summary>
    [Test]
    public void CreateStream_Should_Throw_When_Handler_Is_Missing()
    {
        var runtime = CreateRuntime(_ => { });

        Assert.That(
            () => runtime.CreateStream(new ContextAwareStreamRequest()),
            Throws.InvalidOperationException.With.Message.Contains("No CQRS stream handler registered"));
    }

    /// <summary>
    ///     验证当 stream pipeline behavior 需要上下文注入、但当前 CQRS 上下文不实现
    ///     <see cref="GFramework.Core.Abstractions.Architectures.IArchitectureContext" /> 时，
    ///     dispatcher 会在建流前显式失败。
    /// </summary>
    [Test]
    public void CreateStream_Should_Throw_When_Stream_Pipeline_Behavior_Context_Does_Not_Implement_IArchitectureContext()
    {
        var runtime = CreateRuntime(
            provider =>
            {
                provider.Register(typeof(IStreamRequestHandler<ContextAwareStreamRequest, int>), new PassthroughStreamHandler());
                provider.Register(typeof(IStreamPipelineBehavior<ContextAwareStreamRequest, int>), new ContextAwareStreamBehavior());
            });

        Assert.That(
            () => runtime.CreateStream(new ContextAwareStreamRequest(), cancellationToken: default, new FakeCqrsContext()),
            Throws.InvalidOperationException.With.Message.Contains("does not provide an IArchitectureContext"));
    }

    /// <summary>
    ///     创建一个只满足当前测试最小依赖面的 dispatcher runtime。
    /// </summary>
    /// <param name="configureProvider">对测试用服务提供者的额外注册。</param>
    /// <returns>默认 CQRS runtime。</returns>
    private static GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime CreateRuntime(
        Action<FakeDispatchServiceProvider> configureProvider)
    {
        var provider = new FakeDispatchServiceProvider();
        configureProvider(provider);
        var logger = new TestLogger("CqrsDispatcherContextValidationTests", LogLevel.Debug);
        return GFramework.Cqrs.CqrsRuntimeFactory.CreateRuntime(provider, logger);
    }

    /// <summary>
    ///     为失败语义测试提供最小 CQRS 上下文标记，但故意不实现架构上下文能力。
    /// </summary>
    private sealed class FakeCqrsContext : ICqrsContext;

    /// <summary>
    ///     为 request 上下文校验提供最小测试请求。
    /// </summary>
    private sealed record ContextAwareRequest : IRequest<int>;

    /// <summary>
    ///     为 notification 上下文校验提供最小测试通知。
    /// </summary>
    private sealed record ContextAwareNotification : INotification;

    /// <summary>
    ///     为 stream 上下文校验提供最小测试请求。
    /// </summary>
    private sealed record ContextAwareStreamRequest : IStreamRequest<int>;

    /// <summary>
    ///     为 request 上下文校验提供需要注入架构上下文的最小 handler。
    /// </summary>
    private sealed class ContextAwareRequestHandler : CqrsContextAwareHandlerBase, IRequestHandler<ContextAwareRequest, int>
    {
        /// <summary>
        ///     返回固定结果；当前测试只关心调用前的上下文校验。
        /// </summary>
        /// <param name="request">当前请求。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>固定整型结果。</returns>
        public ValueTask<int> Handle(ContextAwareRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(1);
        }
    }

    /// <summary>
    ///     为 notification 上下文校验提供需要注入架构上下文的最小 handler。
    /// </summary>
    private sealed class ContextAwareNotificationHandler
        : CqrsContextAwareHandlerBase,
            INotificationHandler<ContextAwareNotification>
    {
        /// <summary>
        ///     返回已完成任务；当前测试只关心调用前的上下文校验。
        /// </summary>
        /// <param name="notification">当前通知。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>已完成任务。</returns>
        public ValueTask Handle(ContextAwareNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     为 stream 上下文校验提供需要注入架构上下文的最小 handler。
    /// </summary>
    private sealed class ContextAwareStreamHandler
        : CqrsContextAwareHandlerBase,
            IStreamRequestHandler<ContextAwareStreamRequest, int>
    {
        /// <summary>
        ///     返回一个最小流；当前测试只关心建流前的上下文校验。
        /// </summary>
        /// <param name="request">当前流请求。</param>
        /// <param name="cancellationToken">取消枚举时使用的取消令牌。</param>
        /// <returns>包含单个固定元素的异步流。</returns>
        public async IAsyncEnumerable<int> Handle(
            ContextAwareStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     为 stream behavior 上下文校验提供不依赖上下文注入的最小 handler。
    /// </summary>
    private sealed class PassthroughStreamHandler : IStreamRequestHandler<ContextAwareStreamRequest, int>
    {
        /// <summary>
        ///     返回一个最小流；当前测试只关心 behavior 注入前的上下文校验。
        /// </summary>
        /// <param name="request">当前流请求。</param>
        /// <param name="cancellationToken">取消枚举时使用的取消令牌。</param>
        /// <returns>包含单个固定元素的异步流。</returns>
        public async IAsyncEnumerable<int> Handle(
            ContextAwareStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return 1;
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     为 stream behavior 上下文校验提供需要注入架构上下文的最小 behavior。
    /// </summary>
    private sealed class ContextAwareStreamBehavior
        : CqrsContextAwareHandlerBase,
            IStreamPipelineBehavior<ContextAwareStreamRequest, int>
    {
        /// <summary>
        ///     直接转发到下一个处理阶段；当前测试只关心调用前的上下文校验。
        /// </summary>
        /// <param name="message">当前流式请求。</param>
        /// <param name="next">下一个处理阶段。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>下游处理阶段返回的异步流。</returns>
        public IAsyncEnumerable<int> Handle(
            ContextAwareStreamRequest message,
            StreamMessageHandlerDelegate<ContextAwareStreamRequest, int> next,
            CancellationToken cancellationToken)
        {
            return next(message, cancellationToken);
        }
    }

    /// <summary>
    ///     最小化实现标准 <see cref="IServiceProvider" /> 解析契约的测试替身。
    ///     同时实现 <see cref="IServiceProviderIsService" />，覆盖 dispatcher 的行为注册探测路径。
    /// </summary>
    private sealed class FakeDispatchServiceProvider : IServiceProvider, IServiceProviderIsService
    {
        private readonly Dictionary<Type, List<object>> _registrations = [];

        /// <summary>
        ///     注册指定服务类型的一个或多个实例。
        /// </summary>
        /// <param name="serviceType">服务类型。</param>
        /// <param name="instances">要暴露的实例集合。</param>
        public void Register(Type serviceType, params object[] instances)
        {
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(instances);

            if (!_registrations.TryGetValue(serviceType, out var bucket))
            {
                bucket = [];
                _registrations[serviceType] = bucket;
            }

            bucket.AddRange(instances);
        }

        /// <inheritdoc />
        public object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            // 标准 DI 的 GetServices(Type) 扩展会把多实例解析转换为 IEnumerable<serviceType> 的单例请求；
            // 这里按同样语义暴露注册集合，使 dispatcher 的多实例解析路径保持真实。
            if (serviceType.IsGenericType &&
                serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                return _registrations.TryGetValue(elementType, out var instances)
                    ? [.. instances]
                    : Array.Empty<object>();
            }

            return _registrations.TryGetValue(serviceType, out var singleInstances) && singleInstances.Count > 0
                ? singleInstances[0]
                : null;
        }

        /// <inheritdoc />
        public bool IsService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            if (_registrations.ContainsKey(serviceType))
            {
                return true;
            }

            return serviceType.IsConstructedGenericType &&
                   _registrations.ContainsKey(serviceType.GetGenericTypeDefinition());
        }
    }
}
