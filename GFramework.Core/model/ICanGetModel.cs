using GFramework.Core.rule;

namespace GFramework.Core.model;

/// <summary>
///     定义一个接口，表示可以获取模型的架构组件。
///     该接口继承自IBelongToArchitecture，表明实现此接口的类型属于特定架构。
/// </summary>
public interface ICanGetModel : IBelongToArchitecture;