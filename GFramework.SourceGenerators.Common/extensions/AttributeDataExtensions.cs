using System;
using Microsoft.CodeAnalysis;

namespace GFramework.SourceGenerators.Common.extensions;

/// <summary>
/// 提供AttributeData的扩展方法
/// </summary>
public static class AttributeDataExtensions
{
    /// <param name="attr">特性数据对象</param>
    extension(AttributeData attr)
    {
        /// <summary>
        /// 从特性数据中获取指定名称的命名参数值
        /// </summary>
        /// <typeparam name="T">期望返回的参数类型</typeparam>
        /// <param name="name">要查找的命名参数名称</param>
        /// <param name="defaultValue">当找不到指定参数时返回的默认值</param>
        /// <returns>找到的参数值，如果未找到或类型不匹配则返回默认值</returns>
        public T? GetNamedArgument<T>(string name,
            T? defaultValue = default)
        {
            // 遍历所有命名参数以查找匹配的键值对
            foreach (var kv in attr.NamedArguments)
            {
                if (string.Equals(kv.Key, name, StringComparison.Ordinal) && kv.Value.Value is T t)
                    return t;
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取特性构造函数的第一个参数作为字符串值
        /// </summary>
        /// <param name="defaultValue">当没有构造函数参数或第一个参数不是字符串时返回的默认值</param>
        /// <returns>构造函数第一个参数的字符串值，如果不存在则返回默认值</returns>
        public string GetFirstCtorString(string defaultValue)
        {
            if (attr.ConstructorArguments.Length == 0)
                return defaultValue;

            return attr.ConstructorArguments[0].Value as string ?? defaultValue;
        }
    }
}