using GFramework.Core.Abstractions.rule;
using GFramework.Core.Abstractions.system;

namespace GFramework.Core.Abstractions.ioc;

/// <summary>
///     依赖注入容器接口，定义了服务注册、解析和管理的基本操作
/// </summary>
public interface IIocContainer : IContextAware
{
    #region Register Methods

    /// <summary>
    ///     注册单例
    ///     一个类型只允许一个实例
    /// </summary>
    /// <typeparam name="T">要注册为单例的类型</typeparam>
    /// <param name="instance">要注册的单例实例</param>
    /// <exception cref="InvalidOperationException">当该类型已经注册过单例时抛出异常</exception>
    void RegisterSingleton<T>(T instance);


    /// <summary>
    ///     注册多个实例
    ///     将实例注册到其实现的所有接口和具体类型上
    /// </summary>
    /// <param name="instance">要注册的实例</param>
    public void RegisterPlurality(object instance);

    /// <summary>
    ///     注册系统实例，将其绑定到其所有实现的接口上
    /// </summary>
    /// <param name="system">系统实例对象</param>
    void RegisterSystem(ISystem system);

    /// <summary>
    ///     注册指定类型的实例到容器中
    /// </summary>
    /// <typeparam name="T">要注册的实例类型</typeparam>
    /// <param name="instance">要注册的实例对象，不能为null</param>
    void Register<T>(T instance);

    /// <summary>
    ///     注册指定类型的实例到容器中
    /// </summary>
    /// <param name="type">要注册的实例类型</param>
    /// <param name="instance">要注册的实例对象</param>
    void Register(Type type, object instance);

    #endregion

    #region Get Methods

    /// <summary>
    ///     获取单个实例（通常用于具体类型）
    ///     如果存在多个，只返回第一个
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>找到的第一个实例；如果未找到则返回 null</returns>
    T? Get<T>() where T : class;

    /// <summary>
    ///     获取指定类型的必需实例
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>找到的唯一实例</returns>
    /// <exception cref="InvalidOperationException">当没有注册实例或注册了多个实例时抛出</exception>
    T GetRequired<T>() where T : class;

    /// <summary>
    ///     获取指定类型的所有实例（接口 / 抽象类推荐使用）
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <returns>所有符合条件的实例列表；如果没有则返回空数组</returns>
    IReadOnlyList<T> GetAll<T>() where T : class;

    /// <summary>
    ///     获取并排序（系统调度专用）
    /// </summary>
    /// <typeparam name="T">期望获取的实例类型</typeparam>
    /// <param name="comparison">比较器委托，定义排序规则</param>
    /// <returns>按指定方式排序后的实例列表</returns>
    IReadOnlyList<T> GetAllSorted<T>(Comparison<T> comparison) where T : class;

    #endregion

    #region Utility Methods

    /// <summary>
    ///     检查容器中是否包含指定类型的实例
    /// </summary>
    /// <typeparam name="T">要检查的类型</typeparam>
    /// <returns>如果容器中包含指定类型的实例则返回true，否则返回false</returns>
    bool Contains<T>();

    /// <summary>
    ///     判断容器中是否包含某个具体的实例对象
    /// </summary>
    /// <param name="instance">待查询的实例对象</param>
    /// <returns>若容器中包含该实例则返回true，否则返回false</returns>
    bool ContainsInstance(object instance);

    /// <summary>
    ///     清空容器中的所有实例
    /// </summary>
    void Clear();

    /// <summary>
    ///     冻结容器，防止后续修改
    /// </summary>
    void Freeze();

    #endregion
}