// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Bases;

/// <summary>
/// 预定义的优先级分组常量
/// </summary>
/// <remarks>
/// 提供标准化的优先级值，用于统一管理系统、服务等组件的执行顺序。
/// 优先级值越小，优先级越高（负数表示高优先级）。
/// </remarks>
public static class PriorityGroup
{
    /// <summary>
    /// 关键优先级 - 最高优先级，用于核心系统和基础设施
    /// </summary>
    /// <remarks>
    /// 适用场景：
    /// - 日志系统
    /// - 配置管理
    /// - IoC 容器初始化
    /// - 架构核心组件
    /// </remarks>
    public const int Critical = -100;

    /// <summary>
    /// 高优先级 - 用于重要但非核心的系统
    /// </summary>
    /// <remarks>
    /// 适用场景：
    /// - 事件总线
    /// - 资源管理器
    /// - 输入系统
    /// - 网络管理器
    /// </remarks>
    public const int High = -50;

    /// <summary>
    /// 普通优先级 - 默认优先级
    /// </summary>
    /// <remarks>
    /// 适用场景：
    /// - 游戏逻辑系统
    /// - UI 系统
    /// - 音频系统
    /// - 大部分业务逻辑
    /// </remarks>
    public const int Normal = 0;

    /// <summary>
    /// 低优先级 - 用于非关键系统
    /// </summary>
    /// <remarks>
    /// 适用场景：
    /// - 统计系统
    /// - 调试工具
    /// - 性能监控
    /// - 辅助功能
    /// </remarks>
    public const int Low = 50;

    /// <summary>
    /// 延迟优先级 - 最低优先级，用于可延迟执行的系统
    /// </summary>
    /// <remarks>
    /// 适用场景：
    /// - 分析和遥测
    /// - 后台数据同步
    /// - 缓存清理
    /// - 非紧急任务
    /// </remarks>
    public const int Deferred = 100;
}