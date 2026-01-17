using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.lifecycle;
using GFramework.Core.Abstractions.rule;

namespace GFramework.Core.Abstractions.system;

/// <summary>
///     系统接口，定义了系统的基本行为和功能
///     该接口继承了多个框架相关的接口，提供了系统初始化和销毁能力
/// </summary>
public interface ISystem : IContextAware, IArchitecturePhaseAware, ILifecycle;