// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Runtime.ExceptionServices;
using GFramework.Core.Abstractions.Coroutine;

namespace GFramework.Core.Coroutine.Extensions;

/// <summary>
///     顺序组合枚举器的唯一资源所有者，连尚未开始的后续枚举器也会释放。
/// </summary>
/// <remarks>
///     使用显式枚举器代替迭代器 finally，保证开始前释放、异常和取消路径行为一致。
///     多个资源释放失败会汇总，避免后续资源因前一个异常而泄漏。
/// </remarks>
internal sealed class OwnedCoroutineEnumerator : IEnumerator<IYieldInstruction>, ICoroutineCompletionStatusSource
{
    private readonly IEnumerator<IYieldInstruction>?[] _coroutines;
    private Action? _onCompleted;
    private IYieldInstruction? _current;
    private int _index;
    private bool _disposed;
    private CoroutineCompletionStatus _terminalStatus;

    /// <summary>
    ///     转交一组枚举器的所有权，并复制集合以防调用方随后修改数组。
    /// </summary>
    /// <param name="coroutines">按顺序执行的枚举器。</param>
    /// <param name="onCompleted">全部自然完成后执行的动作；取消时不执行。</param>
    internal OwnedCoroutineEnumerator(IReadOnlyList<IEnumerator<IYieldInstruction>> coroutines,
        Action? onCompleted = null)
    {
        _coroutines = new IEnumerator<IYieldInstruction>?[coroutines.Count];
        for (var i = 0; i < coroutines.Count; i++)
        {
            _coroutines[i] = coroutines[i] ?? throw new ArgumentException(
                "Coroutine entries must not be null.", nameof(coroutines));
        }

        _onCompleted = onCompleted;
    }

    /// <inheritdoc />
    public IYieldInstruction Current => _current!;

    /// <inheritdoc />
    object IEnumerator.Current => Current;

    /// <summary>
    ///     将内部操作已经确认的失败分类传递给外层组合和调度器。
    /// </summary>
    CoroutineCompletionStatus ICoroutineCompletionStatusSource.CompletionStatus => _terminalStatus;

    /// <inheritdoc />
    public bool MoveNext()
    {
        if (_disposed)
        {
            return false;
        }

        try
        {
            while (_index < _coroutines.Length)
            {
                var coroutine = _coroutines[_index]!;
                if (coroutine.MoveNext())
                {
                    _current = coroutine.Current;
                    return true;
                }

                // 先解除本地所有权再调用外部 Dispose，避免异常后的收尾重复释放。
                _coroutines[_index++] = null;
                try
                {
                    coroutine.Dispose();
                }
                catch
                {
                    // 清理异常始终是失败，即使用户 Dispose 抛出的异常类型表示取消。
                    _terminalStatus = CoroutineCompletionStatus.Faulted;
                    throw;
                }
            }

            _disposed = true;
            _current = null;
            var onCompleted = _onCompleted;
            _onCompleted = null;
            onCompleted?.Invoke();
            return false;
        }
        catch (Exception exception)
        {
            if (_index < _coroutines.Length
                && _coroutines[_index] is ICoroutineCompletionStatusSource
                {
                    CompletionStatus: CoroutineCompletionStatus.Faulted
                })
            {
                _terminalStatus = CoroutineCompletionStatus.Faulted;
            }

            ExceptionDispatchInfo.Capture(DisposeAll(exception)!).Throw();
            return false;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        throw new NotSupportedException("Coroutine composition cannot be reset.");
    }

    /// <summary>
    ///     释放所有仍持有的枚举器；不会执行完成动作。
    /// </summary>
    public void Dispose()
    {
        var exception = DisposeAll();
        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    /// <summary>
    ///     在尝试释放每一项资源后汇总主异常与清理异常。
    /// </summary>
    private Exception? DisposeAll(Exception? primaryException = null)
    {
        _disposed = true;
        _current = null;
        _onCompleted = null;
        List<Exception>? exceptions = primaryException == null ? null : new List<Exception> { primaryException };
        for (var i = 0; i < _coroutines.Length; i++)
        {
            var coroutine = _coroutines[i];
            _coroutines[i] = null;
            if (coroutine == null)
            {
                continue;
            }

            try
            {
                coroutine.Dispose();
            }
            catch (Exception exception)
            {
                _terminalStatus = CoroutineCompletionStatus.Faulted;
                (exceptions ??= new List<Exception>()).Add(exception);
            }
        }

        return exceptions?.Count switch
        {
            null or 0 => null,
            1 => exceptions[0],
            _ => new AggregateException(exceptions)
        };
    }
}
