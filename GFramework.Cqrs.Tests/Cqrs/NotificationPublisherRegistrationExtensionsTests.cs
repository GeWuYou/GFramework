// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Architectures;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Notification;

using GFramework.Cqrs.Extensions;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 notification publisher 组合根注册扩展的关键行为。
/// </summary>
[TestFixture]
internal sealed class NotificationPublisherRegistrationExtensionsTests
{
    /// <summary>
    ///     验证显式注册内置 <see cref="TaskWhenAllNotificationPublisher" /> 后，
    ///     标准 runtime 基础设施会复用该策略并继续调度所有处理器。
    /// </summary>
    [Test]
    public async Task UseTaskWhenAllNotificationPublisher_Should_Be_Used_By_Default_Runtime_Infrastructure()
    {
        var trailingHandler = new RecordingNotificationHandler();
        var container = new ServiceTestHost();
        container.Services.UseTaskWhenAllNotificationPublisher();
        container.ExecuteServicesHook(services =>
        {
            services.AddSingleton<INotificationHandler<TestNotification>>(new ThrowingNotificationHandler());
            services.AddSingleton<INotificationHandler<TestNotification>>(trailingHandler);
        });
        CqrsTestRuntime.RegisterInfrastructure(container);
        container.Freeze();
        Assert.That(container.GetAll<INotificationHandler<TestNotification>>(), Has.Count.EqualTo(2));
        Assert.That(container.GetAll<INotificationPublisher>(), Has.Count.EqualTo(1));

        var context = new CqrsTestHost(container);
        var publishTask = context.PublishAsync(new TestNotification()).AsTask();

        try
        {
            await publishTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // `TaskWhenAll` 策略会在所有处理器都结束后聚合失败；这里仅消费异常并继续断言第二个处理器已执行。
        }

        Assert.That(trailingHandler.WasInvoked, Is.True);
        Assert.That(publishTask.Exception, Is.Not.Null);
    }

    /// <summary>
    ///     验证显式注册内置 <see cref="SequentialNotificationPublisher" /> 后，
    ///     默认 runtime 基础设施会保留“首个失败立即停止后续处理器”的顺序语义。
    /// </summary>
    [Test]
    public void UseSequentialNotificationPublisher_Should_Preserve_Stop_On_First_Failure_Semantics()
    {
        var trailingHandler = new RecordingNotificationHandler();
        var container = new ServiceTestHost();
        container.Services.UseSequentialNotificationPublisher();
        container.ExecuteServicesHook(services =>
        {
            services.AddSingleton<INotificationHandler<TestNotification>>(new ThrowingNotificationHandler());
            services.AddSingleton<INotificationHandler<TestNotification>>(trailingHandler);
        });
        CqrsTestRuntime.RegisterInfrastructure(container);
        container.Freeze();
        Assert.That(container.GetAll<INotificationHandler<TestNotification>>(), Has.Count.EqualTo(2));
        Assert.That(container.GetAll<INotificationPublisher>(), Has.Count.EqualTo(1));

        var context = new CqrsTestHost(container);

        Assert.That(
            async () => await context.PublishAsync(new TestNotification()).ConfigureAwait(false),
            Throws.InvalidOperationException.With.Message.EqualTo("boom"));
        Assert.That(trailingHandler.WasInvoked, Is.False);
        Assert.That(container.GetRequired<INotificationPublisher>(), Is.TypeOf<SequentialNotificationPublisher>());
    }

    /// <summary>
    ///     验证显式传入实例的组合根注册入口会把同一个 publisher 实例绑定到服务集合。
    /// </summary>
    [Test]
    public void UseNotificationPublisher_Instance_Overload_Should_Register_Same_Instance()
    {
        var container = new ServiceTestHost();
        var publisher = new TrackingNotificationPublisher();

        var returnedServices = container.Services.UseNotificationPublisher(publisher);
        container.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(returnedServices, Is.SameAs(container.Services));
            Assert.That(container.Get<INotificationPublisher>(), Is.SameAs(publisher));
        });
    }

    /// <summary>
    ///     验证泛型组合根注册入口会把指定的 publisher 类型注册为容器内唯一的单例策略。
    /// </summary>
    [Test]
    public void UseNotificationPublisher_Generic_Overload_Should_Register_Configured_Type()
    {
        var container = new ServiceTestHost();

        var returnedServices = container.Services.UseNotificationPublisher<TrackingNotificationPublisher>();
        container.Freeze();

        Assert.Multiple(() =>
        {
            Assert.That(returnedServices, Is.SameAs(container.Services));
            Assert.That(container.HasRegistration(typeof(INotificationPublisher)), Is.True);
            Assert.That(container.GetAll<INotificationPublisher>(), Has.Count.EqualTo(1));
            Assert.That(container.GetRequired<INotificationPublisher>(), Is.TypeOf<TrackingNotificationPublisher>());
            Assert.That(container.GetRequired<INotificationPublisher>(), Is.SameAs(container.GetRequired<INotificationPublisher>()));
        });
    }

    /// <summary>
    ///     验证当自定义 publisher 依赖其他容器服务时，泛型组合根入口仍会被默认 runtime 基础设施正确复用。
    /// </summary>
    [Test]
    public async Task UseNotificationPublisher_Generic_Overload_Should_Be_Used_By_Default_Runtime_Infrastructure()
    {
        var probe = new NotificationPublisherProbe();
        var handler = new RecordingNotificationHandler();
        var container = new ServiceTestHost();
        container.Register(probe);
        container.Services.UseNotificationPublisher<DependencyAwareNotificationPublisher>();
        container.Register<INotificationHandler<TestNotification>>(handler);
        CqrsTestRuntime.RegisterInfrastructure(container);
        container.Freeze();
        Assert.That(container.GetAll<INotificationHandler<TestNotification>>(), Has.Count.EqualTo(1));
        Assert.That(container.GetAll<INotificationPublisher>(), Has.Count.EqualTo(1));

        var context = new CqrsTestHost(container);

        await context.PublishAsync(new TestNotification()).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(probe.WasCalled, Is.True);
            Assert.That(handler.WasInvoked, Is.True);
            Assert.That(container.GetRequired<INotificationPublisher>(), Is.TypeOf<DependencyAwareNotificationPublisher>());
        });
    }

    /// <summary>
    ///     验证组合根扩展会阻止重复 notification publisher 注册，避免 runtime 创建阶段才暴露歧义。
    /// </summary>
    [Test]
    public void UseNotificationPublisher_Should_Throw_When_NotificationPublisher_Already_Registered()
    {
        var container = new ServiceTestHost();
        container.Services.UseTaskWhenAllNotificationPublisher();

        Assert.That(
            () => container.Services.UseNotificationPublisher(new TrackingNotificationPublisher()),
            Throws.InvalidOperationException.With.Message.Contains(nameof(INotificationPublisher)));
    }

    /// <summary>
    ///     验证当容器已存在 notification publisher 注册时，泛型组合根入口也会拒绝重复策略声明。
    /// </summary>
    [Test]
    public void UseNotificationPublisher_Generic_Overload_Should_Throw_When_NotificationPublisher_Already_Registered()
    {
        var container = new ServiceTestHost();
        container.Services.UseSequentialNotificationPublisher();

        Assert.That(
            () => container.Services.UseNotificationPublisher<TrackingNotificationPublisher>(),
            Throws.InvalidOperationException.With.Message.Contains(nameof(INotificationPublisher)));
    }

    /// <summary>
    ///     为本组测试提供最小 notification 类型。
    /// </summary>
    private sealed record TestNotification : INotification;

    /// <summary>
    ///     记录自己是否被执行的测试处理器。
    /// </summary>
    private sealed class RecordingNotificationHandler : INotificationHandler<TestNotification>
    {
        /// <summary>
        ///     获取当前处理器是否至少执行过一次。
        /// </summary>
        public bool WasInvoked
        {
            get; private set;
        }

        /// <summary>
        ///     记录执行痕迹并立刻完成。
        /// </summary>
        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            WasInvoked = true;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     始终抛出异常的测试处理器，用于验证并行策略不会因为首个失败而停止其余处理器。
    /// </summary>
    private sealed class ThrowingNotificationHandler : INotificationHandler<TestNotification>
    {
        /// <summary>
        ///     始终抛出测试异常。
        /// </summary>
        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("boom");
        }
    }

    /// <summary>
    ///     用于验证实例注册重载是否保留原对象身份的测试发布器。
    /// </summary>
    private sealed class TrackingNotificationPublisher : INotificationPublisher
    {
        /// <summary>
        ///     直接完成当前 publish 调用。
        /// </summary>
        public ValueTask PublishAsync<TNotification>(
            NotificationPublishContext<TNotification> context,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    ///     记录泛型 publisher 是否真正参与了 publish 调用的探针。
    /// </summary>
    private sealed class NotificationPublisherProbe
    {
        /// <summary>
        ///     获取探针是否已被自定义 publisher 标记为执行过。
        /// </summary>
        public bool WasCalled
        {
            get; private set;
        }

        /// <summary>
        ///     记录当前自定义 publisher 已接管本次通知发布。
        /// </summary>
        public void MarkCalled()
        {
            WasCalled = true;
        }
    }

    /// <summary>
    ///     依赖容器内探针服务的自定义 publisher，用于验证泛型重载确实走过了 provider 构造路径。
    /// </summary>
    private sealed class DependencyAwareNotificationPublisher(NotificationPublisherProbe probe) : INotificationPublisher
    {
        /// <summary>
        ///     记录 publisher 已参与调用，再按当前处理器顺序继续执行。
        /// </summary>
        public async ValueTask PublishAsync<TNotification>(
            NotificationPublishContext<TNotification> context,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            probe.MarkCalled();

            foreach (var handler in context.Handlers)
            {
                await context.InvokeHandlerAsync(handler, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
