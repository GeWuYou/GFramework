// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Lifecycle;

namespace GFramework.Core.Architectures;

/// <summary>
///     记录实际开始初始化的业务参与者，按逆序停止；不释放它们的 DI 依赖或外部实例。
///     provider 的资源释放由架构所有者在业务停止之后统一完成。
/// </summary>
internal sealed class ArchitectureDisposer
{
    private readonly List<object> _started = [];
    private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);

    /// <summary>在调用初始化之前记录参与者，使初始化中途失败也能执行其清理。</summary>
    /// <param name="component">本轮尝试启动的组件。</param>
    public void Register(object component)
    {
        if (_seen.Add(component))
            _started.Add(component);
    }

    /// <summary>停止所有参与者；收集异常而不提前终止后续停止动作。</summary>
    /// <returns>所有业务停止异常。</returns>
    public async ValueTask<IReadOnlyList<Exception>> StopAsync()
    {
        var errors = new List<Exception>();
        for (var i = _started.Count - 1; i >= 0; i--)
        {
            try
            {
                if (_started[i] is IAsyncDestroyable asyncDestroyable)
                    await asyncDestroyable.DestroyAsync().ConfigureAwait(false);
                else if (_started[i] is IDestroyable destroyable)
                    destroyable.Destroy();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        _started.Clear();
        _seen.Clear();
        return errors;
    }
}
