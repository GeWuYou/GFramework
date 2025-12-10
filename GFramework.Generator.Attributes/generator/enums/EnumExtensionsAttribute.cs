
using System;

namespace GFramework.Generator.Attributes
{
    /// <summary>
    /// 标注在 enum 上，Source Generator 会为该 enum 生成扩展方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class GenerateEnumExtensionsAttribute : Attribute
    {
        /// <summary>
        /// 是否为每个枚举项生成单独的 IsXXX 方法（默认 true）。
        /// </summary>
        public bool GenerateIsMethods { get; set; } = true;

        /// <summary>
        /// 是否生成一个 IsIn(params T[]) 方法以简化多值判断（默认 true）。
        /// </summary>
        public bool GenerateIsInMethod { get; set; } = true;
    }
}