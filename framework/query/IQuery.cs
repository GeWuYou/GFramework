using GWFramework.framework.model;
using GWFramework.framework.rule;
using GWFramework.framework.system;

namespace GWFramework.framework.query;


/// <summary>
/// 查询接口，定义了执行查询操作的契约
/// </summary>
/// <typeparam name="TResult">查询结果的类型</typeparam>
public interface IQuery<out TResult> : ICanSetArchitecture, ICanGetModel, ICanGetSystem,
    ICanSendQuery
{
    /// <summary>
    /// 执行查询操作并返回结果
    /// </summary>
    /// <returns>查询的结果，类型为 TResult</returns>
    TResult Do();
}
