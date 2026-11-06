// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Architectures;

/// <summary>
///     当前容器的上下文访问状态。实例随架构注册，禁止配置期解析以及外部在停止期间使用旧上下文。
///     清理作用域仅流经当前异步调用链，不会给其他调用方开放停止中的服务。
/// </summary>
internal sealed class ArchitectureContextAccess
{
    private readonly AsyncLocal<int> _cleanupDepth = new();
    private volatile bool _ready;
    private volatile bool _stopping;

    /// <summary>停止屏障已发布；与当前阶段值无关，外部调用立即视为不可用。</summary>
    public bool IsStopping => _stopping;

    /// <summary>标记正式 provider 已构建，可以通过上下文解析服务。</summary>
    public void EnableServices() => _ready = true;

    /// <summary>立即阻止外部通过已保留的上下文引用发起新操作。</summary>
    public void BeginShutdown() => _stopping = true;

    /// <summary>检查配置期和停止期约束；业务停止回调在独立作用域中仍可解析其清理依赖。</summary>
    public void EnsureAvailable()
    {
        if (_stopping && _cleanupDepth.Value == 0)
            throw new ObjectDisposedException(nameof(ArchitectureContext));
        if (!_ready)
            throw new InvalidOperationException("Services cannot be resolved during configuration. Use component initialization or OnStarted.");
    }

    /// <summary>允许当前清理调用链访问仍存活的容器服务。</summary>
    /// <returns>离开当前清理作用域的句柄。</returns>
    public IDisposable EnterCleanup()
    {
        _cleanupDepth.Value++;
        return new CleanupScope(this);
    }

    /// <summary>恢复当前异步调用链的清理访问深度。</summary>
    private sealed class CleanupScope(ArchitectureContextAccess owner) : IDisposable
    {
        private bool _disposed;

        /// <summary>仅退出一次，防止重复 Dispose 使访问深度变为负数。</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            owner._cleanupDepth.Value--;
        }
    }
}
