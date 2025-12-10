using GFramework.Core.architecture;

namespace GFramework.Core.rule;


/// <summary>
/// 定义一个接口，用于标识某个对象属于特定的架构体系。
/// 实现此接口的对象可以通过GetArchitecture方法获取其所属的架构实例。
/// </summary>
public interface IBelongToArchitecture
{
    /// <summary>
    /// 获取当前对象所属的架构实例。
    /// </summary>
    /// <returns>返回实现IArchitecture接口的架构实例</returns>
    IArchitecture GetArchitecture();
}
