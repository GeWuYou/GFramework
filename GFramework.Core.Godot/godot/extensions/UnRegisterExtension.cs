using GFramework.Core.events;
using Godot;

namespace GFramework.Core.Godot.godot.extensions;

/// <summary>
/// 提供取消注册扩展方法的静态类
/// </summary>
public static class UnRegisterExtension
{
    /// <summary>
    /// 当节点退出场景树时自动取消注册监听器
    /// </summary>
    /// <param name="unRegister">需要在节点退出时被取消注册的监听器接口实例</param>
    /// <param name="node">Godot节点对象，当该节点退出场景树时触发取消注册操作</param>
    /// <returns>返回传入的原始IUnRegister实例，支持链式调用</returns>
    public static IUnRegister UnRegisterWhenNodeExitTree(this IUnRegister unRegister, Node node)
    {
        // 监听节点的TreeExiting事件，在节点即将退出场景树时执行取消注册操作
        node.TreeExiting += unRegister.UnRegister;
        return unRegister;
    }
}