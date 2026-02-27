namespace GFramework.Core.Abstractions.pause;

/// <summary>
/// 暂停令牌，唯一标识一个暂停请求
/// </summary>
public readonly struct PauseToken : IEquatable<PauseToken>
{
    /// <summary>
    /// 令牌 ID
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 是否为有效令牌
    /// </summary>
    public bool IsValid => Id != Guid.Empty;

    /// <summary>
    /// 创建暂停令牌
    /// </summary>
    /// <param name="id">令牌 ID</param>
    public PauseToken(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// 创建无效令牌
    /// </summary>
    public static PauseToken Invalid => new(Guid.Empty);

    public bool Equals(PauseToken other) => Id.Equals(other.Id);

    public override bool Equals(object? obj) => obj is PauseToken other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(PauseToken left, PauseToken right) => left.Equals(right);

    public static bool operator !=(PauseToken left, PauseToken right) => !left.Equals(right);

    public override string ToString() => $"PauseToken({Id})";
}