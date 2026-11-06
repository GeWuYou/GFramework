// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Notification;

namespace GFramework.Cqrs.Extensions;

/// <summary>
///     为 CQRS runtime 提供 notification publisher 策略的组合根注册入口。
/// </summary>
/// <remarks>
///     <para>默认 runtime 只会消费一个 <see cref="INotificationPublisher" /> 实例，因此该扩展类把“选择哪种策略”显式收敛到容器配置阶段。</para>
///     <para>这些入口应在 runtime 创建前调用；对于走标准 <c>GFramework.Core</c> 启动路径的架构，它们会被 <c>CqrsRuntimeModule</c> 自动复用。</para>
/// </remarks>
public static class NotificationPublisherRegistrationExtensions
{
    /// <summary>
    ///     将指定的 notification publisher 实例注册为当前容器唯一的发布策略。
    /// </summary>
    /// <param name="services">目标依赖注入容器。</param>
    /// <param name="notificationPublisher">要复用的 notification publisher 实例。</param>
    /// <returns>同一个 <paramref name="services" />，便于在组合根中继续链式配置。</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="services" /> 或 <paramref name="notificationPublisher" /> 为 <see langword="null" />。
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     当前服务集合已存在 <see cref="INotificationPublisher" /> 注册，无法再切换为另一个策略。
    /// </exception>
    public static IServiceCollection UseNotificationPublisher(
        this IServiceCollection services,
        INotificationPublisher notificationPublisher)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(notificationPublisher);

        ThrowIfNotificationPublisherAlreadyRegistered(services);
        services.AddSingleton(notificationPublisher);
        return services;
    }

    /// <summary>
    ///     将指定类型的 notification publisher 注册为当前容器唯一的发布策略。
    /// </summary>
    /// <typeparam name="TNotificationPublisher">发布策略实现类型。</typeparam>
    /// <param name="services">目标依赖注入容器。</param>
    /// <returns>同一个 <paramref name="services" />，便于在组合根中继续链式配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     当前服务集合已存在 <see cref="INotificationPublisher" /> 注册，无法再切换为另一个策略。
    /// </exception>
    public static IServiceCollection UseNotificationPublisher<TNotificationPublisher>(this IServiceCollection services)
        where TNotificationPublisher : class, INotificationPublisher
    {
        ArgumentNullException.ThrowIfNull(services);

        ThrowIfNotificationPublisherAlreadyRegistered(services);
        services.AddSingleton<INotificationPublisher, TNotificationPublisher>();
        return services;
    }

    /// <summary>
    ///     将内置 <see cref="TaskWhenAllNotificationPublisher" /> 注册为当前容器唯一的 notification publisher 策略。
    /// </summary>
    /// <param name="services">目标依赖注入容器。</param>
    /// <returns>同一个 <paramref name="services" />，便于在组合根中继续链式配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     当前服务集合已存在 <see cref="INotificationPublisher" /> 注册，无法再切换为另一个策略。
    /// </exception>
    /// <remarks>
    ///     该策略更适合“等待所有处理器完成并统一观察失败”的语义诉求；
    ///     若只是为了降低 steady-state publish 开销，应先结合实际 benchmark 结果评估是否值得切换。
    /// </remarks>
    public static IServiceCollection UseTaskWhenAllNotificationPublisher(this IServiceCollection services)
    {
        return UseNotificationPublisher(services, new TaskWhenAllNotificationPublisher());
    }

    /// <summary>
    ///     将内置 <see cref="SequentialNotificationPublisher" /> 注册为当前容器唯一的 notification publisher 策略。
    /// </summary>
    /// <param name="services">目标依赖注入容器。</param>
    /// <returns>同一个 <paramref name="services" />，便于在组合根中继续链式配置。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> 为 <see langword="null" />。</exception>
    /// <exception cref="InvalidOperationException">
    ///     当前服务集合已存在 <see cref="INotificationPublisher" /> 注册，无法再切换为另一个策略。
    /// </exception>
    /// <remarks>
    ///     该策略适合处理器之间存在顺序依赖，或调用方希望在首个失败处立即停止后续分发的场景。
    /// </remarks>
    public static IServiceCollection UseSequentialNotificationPublisher(this IServiceCollection services)
    {
        return UseNotificationPublisher(services, new SequentialNotificationPublisher());
    }

    /// <summary>
    ///     在组合根阶段阻止多个 notification publisher 策略同时注册，避免 runtime 创建时出现歧义。
    /// </summary>
    /// <param name="services">当前正在配置的依赖注入容器。</param>
    /// <exception cref="InvalidOperationException">当前服务集合已存在 notification publisher 注册。</exception>
    private static void ThrowIfNotificationPublisherAlreadyRegistered(IServiceCollection services)
    {
        if (!services.Any(static descriptor => descriptor.ServiceType == typeof(INotificationPublisher)))
        {
            return;
        }

        throw new InvalidOperationException(
            $"An {typeof(INotificationPublisher).FullName} is already registered. Remove the existing notification publisher strategy before calling {nameof(UseNotificationPublisher)} again.");
    }
}
