namespace GFramework.Core.coroutine;

/// <summary>
/// 协程句柄
/// 用于唯一标识和管理协程实例的结构体，通过ID系统实现协程的跟踪和比较功能
/// </summary>
public readonly struct CoroutineHandle : IEquatable<CoroutineHandle>
{
    /// <summary>
    /// 预留空间常量，用于ID分配的基数
    /// </summary>
    private const byte ReservedSpace = 0x0F;

    /// <summary>
    /// 下一个索引数组，用于跟踪每个实例ID的下一个可用索引位置
    /// 索引范围：0-15，对应16个不同的实例槽位
    /// </summary>
    private static readonly int[] NextIndex = new int[16];

    /// <summary>
    /// 协程句柄的内部ID，用于唯一标识协程实例
    /// </summary>
    private readonly int _id;

    /// <summary>
    /// 静态构造函数，初始化NextIndex数组的默认值
    /// 将索引0的下一个可用位置设置为ReservedSpace + 1
    /// </summary>
    static CoroutineHandle()
    {
        NextIndex[0] = ReservedSpace + 1;
    }


    /// <summary>
    /// 获取当前协程句柄的键值（低4位）
    /// </summary>
    public byte Key => (byte)(_id & ReservedSpace);

    /// <summary>
    /// 判断当前协程句柄是否有效
    /// 有效性通过Key是否为0来判断
    /// </summary>
    public bool IsValid => Key != 0;

    /// <summary>
    /// 构造函数，创建一个新的协程句柄
    /// </summary>
    /// <param name="instanceId">实例ID，用于区分不同的协程实例槽位</param>
    public CoroutineHandle(byte instanceId)
    {
        if (instanceId > ReservedSpace)
            instanceId -= ReservedSpace;

        _id = NextIndex[instanceId] + instanceId;
        NextIndex[instanceId] += ReservedSpace + 1;
    }

    /// <summary>
    /// 比较当前协程句柄与另一个协程句柄是否相等
    /// </summary>
    /// <param name="other">要比较的协程句柄</param>
    /// <returns>如果两个句柄的ID相同则返回true，否则返回false</returns>
    public bool Equals(CoroutineHandle other) => _id == other._id;

    /// <summary>
    /// 比较当前对象与指定对象是否相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果对象是协程句柄且ID相同则返回true，否则返回false</returns>
    public override bool Equals(object? obj) => obj is CoroutineHandle handle && Equals(handle);

    /// <summary>
    /// 获取当前协程句柄的哈希码
    /// </summary>
    /// <returns>基于内部ID计算的哈希码</returns>
    public override int GetHashCode() => _id;

    /// <summary>
    /// 比较两个协程句柄是否相等
    /// </summary>
    /// <param name="a">第一个协程句柄</param>
    /// <param name="b">第二个协程句柄</param>
    /// <returns>如果两个句柄的ID相同则返回true，否则返回false</returns>
    public static bool operator ==(CoroutineHandle a, CoroutineHandle b) => a._id == b._id;

    /// <summary>
    /// 比较两个协程句柄是否不相等
    /// </summary>
    /// <param name="a">第一个协程句柄</param>
    /// <param name="b">第二个协程句柄</param>
    /// <returns>如果两个句柄的ID不同则返回true，否则返回false</returns>
    public static bool operator !=(CoroutineHandle a, CoroutineHandle b) => a._id != b._id;
}