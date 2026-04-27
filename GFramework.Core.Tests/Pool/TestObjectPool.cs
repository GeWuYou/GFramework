using GFramework.Core.Pool;

namespace GFramework.Core.Tests.Pool;

/// <summary>
///     测试用对象池实现类，继承自 <see cref="AbstractObjectPoolSystem{TKey,TValue}" />，
///     用于验证对象池的获取、释放和统计行为。
/// </summary>
public class TestObjectPool : AbstractObjectPoolSystem<string, TestPoolableObject>
{
    /// <summary>
    ///     根据池键创建新的测试对象。
    /// </summary>
    /// <param name="key">用于标识对象所属池的键。</param>
    /// <returns>带有对应 <paramref name="key" /> 的测试对象实例。</returns>
    protected override TestPoolableObject Create(string key)
    {
        return new TestPoolableObject { PoolKey = key };
    }

    /// <summary>
    ///     执行对象池初始化。
    /// </summary>
    protected override void OnInit()
    {
    }
}
