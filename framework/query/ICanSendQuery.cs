using GWFramework.framework.rule;

namespace GWFramework.framework.query;

/// <summary>
/// 定义一个可以发送查询的接口契约
/// 该接口继承自IBelongToArchitecture，表示实现此接口的类型属于某个架构组件
/// </summary>
public interface ICanSendQuery : IBelongToArchitecture;
