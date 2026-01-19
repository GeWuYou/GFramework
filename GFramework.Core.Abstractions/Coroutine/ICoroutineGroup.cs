using System.Collections.Generic;

namespace GFramework.Core.Abstractions.Coroutine;

/// <summary>
///     协程分组接口，用于批量管理多个协程作用域
/// </summary>
/// <remarks>
///     协程分组提供了对多个作用域的批量操作能力，包括暂停、恢复和取消
///     适用于需要同时管理多个相关协程的场景，如场景加载、UI管理等
/// </remarks>
public interface ICoroutineGroup : ICoroutineScope
{
    /// <summary>
    ///     获取分组中所有作用域的只读列表
    /// </summary>
    IReadOnlyList<ICoroutineScope> Scopes { get; }

    /// <summary>
    ///     添加一个作用域到分组中
    /// </summary>
    /// <param name="scope">要添加的作用域</param>
    void AddScope(ICoroutineScope scope);

    /// <summary>
    ///     从分组中移除一个作用域
    /// </summary>
    /// <param name="scope">要移除的作用域</param>
    void RemoveScope(ICoroutineScope scope);

    /// <summary>
    ///     暂停分组中的所有作用域
    /// </summary>
    void PauseAll();

    /// <summary>
    ///     恢复分组中的所有作用域
    /// </summary>
    void ResumeAll();

    /// <summary>
    ///     取消分组中的所有作用域
    /// </summary>
    void CancelAll();
}