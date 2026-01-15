namespace GFramework.Game.Abstractions.ui;

/// <summary>
/// 可写的UI注册表接口
/// </summary>
/// <typeparam name="T">UI实例的类型参数</typeparam>
public interface IWritableUiRegistry<T> : IUiRegistry<T> where T : class
{
    /// <summary>
    /// 注册UI实例到注册表
    /// </summary>
    /// <param name="key">UI的唯一标识键</param>
    /// <param name="scene">要注册的UI实例</param>
    /// <returns>当前注册表实例</returns>
    IWritableUiRegistry<T> Register(string key, T scene);
}