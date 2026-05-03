// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Abstractions.Pause;

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
    /// <param name="id">令牌 ID，用于唯一标识暂停请求</param>
    public PauseToken(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// 创建无效令牌
    /// </summary>
    /// <returns>返回一个 ID 为 Guid.Empty 的无效暂停令牌</returns>
    public static PauseToken Invalid => new(Guid.Empty);

    /// <summary>
    /// 判断当前令牌是否与另一个令牌相等
    /// </summary>
    /// <param name="other">要比较的另一个暂停令牌</param>
    /// <returns>如果两个令牌的 ID 相同则返回 true，否则返回 false</returns>
    public bool Equals(PauseToken other) => Id.Equals(other.Id);

    /// <summary>
    /// 判断当前对象是否为暂停令牌类型并与其相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果对象是 PauseToken 类型且 ID 相同则返回 true，否则返回 false</returns>
    public override bool Equals(object? obj) => obj is PauseToken other && Equals(other);

    /// <summary>
    /// 获取令牌的哈希码
    /// </summary>
    /// <returns>基于令牌 ID 计算的哈希值</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// 重载等于运算符，判断两个暂停令牌是否相等
    /// </summary>
    /// <param name="left">左侧暂停令牌</param>
    /// <param name="right">右侧暂停令牌</param>
    /// <returns>如果两个令牌相等则返回 true，否则返回 false</returns>
    public static bool operator ==(PauseToken left, PauseToken right) => left.Equals(right);

    /// <summary>
    /// 重载不等于运算符，判断两个暂停令牌是否不相等
    /// </summary>
    /// <param name="left">左侧暂停令牌</param>
    /// <param name="right">右侧暂停令牌</param>
    /// <returns>如果两个令牌不相等则返回 true，否则返回 false</returns>
    public static bool operator !=(PauseToken left, PauseToken right) => !left.Equals(right);

    /// <summary>
    /// 将暂停令牌转换为字符串表示形式
    /// </summary>
    /// <returns>包含令牌 ID 信息的字符串</returns>
    public override string ToString() => $"PauseToken({Id})";
}