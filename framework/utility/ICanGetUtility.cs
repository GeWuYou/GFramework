using GFramework.framework.rule;

namespace GFramework.framework.utility;

/// <summary>
/// 定义一个接口，表示可以获取工具类的对象
/// 该接口继承自IBelongToArchitecture，表明实现此接口的类型属于某个架构组件
/// </summary>
public interface ICanGetUtility : IBelongToArchitecture;
