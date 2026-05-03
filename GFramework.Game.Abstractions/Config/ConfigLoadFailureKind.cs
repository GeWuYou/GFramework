// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Config;

/// <summary>
///     表示配置加载过程中可稳定断言的失败类别。
///     该枚举用于把文件系统、schema 校验、反序列化和跨表引用错误从自由文本消息中抽离出来，
///     便于日志、测试和上层工具以结构化方式处理失败原因。
/// </summary>
public enum ConfigLoadFailureKind
{
    /// <summary>
    ///     配置目录不存在。
    /// </summary>
    ConfigDirectoryNotFound,

    /// <summary>
    ///     绑定的 schema 文件不存在。
    /// </summary>
    SchemaFileNotFound,

    /// <summary>
    ///     读取 schema 文件失败。
    /// </summary>
    SchemaReadFailed,

    /// <summary>
    ///     schema 文件不是合法 JSON。
    /// </summary>
    SchemaInvalidJson,

    /// <summary>
    ///     schema 内容超出了当前运行时支持的子集或不满足最小约束。
    /// </summary>
    SchemaUnsupported,

    /// <summary>
    ///     读取配置文件失败。
    /// </summary>
    ConfigFileReadFailed,

    /// <summary>
    ///     YAML 文本在进入 schema 校验阶段前无法被解析。
    /// </summary>
    YamlParseFailed,

    /// <summary>
    ///     YAML 文档数量或结构不符合运行时约束。
    /// </summary>
    InvalidYamlDocument,

    /// <summary>
    ///     对象中出现了重复字段。
    /// </summary>
    DuplicateProperty,

    /// <summary>
    ///     YAML 中出现了 schema 未声明的字段。
    /// </summary>
    UnknownProperty,

    /// <summary>
    ///     YAML 缺失 schema 要求的字段。
    /// </summary>
    MissingRequiredProperty,

    /// <summary>
    ///     YAML 值类型与 schema 声明不匹配。
    /// </summary>
    PropertyTypeMismatch,

    /// <summary>
    ///     YAML 标量值为 null，但 schema 不允许。
    /// </summary>
    NullScalarValue,

    /// <summary>
    ///     YAML 标量值不在 schema 声明的 enum 集合中。
    /// </summary>
    EnumValueNotAllowed,

    /// <summary>
    ///     YAML 标量值违反了 schema 声明的最小值、最大值或长度约束。
    /// </summary>
    ConstraintViolation,

    /// <summary>
    ///     YAML 可被读取，但无法成功反序列化到目标 CLR 类型。
    /// </summary>
    DeserializationFailed,

    /// <summary>
    ///     已解析的配置项无法构造成运行时配置表。
    /// </summary>
    TableBuildFailed,

    /// <summary>
    ///     跨表引用声明的目标表不可用。
    /// </summary>
    ReferencedTableNotFound,

    /// <summary>
    ///     跨表引用值无法转换到目标表主键类型。
    /// </summary>
    ReferenceKeyTypeMismatch,

    /// <summary>
    ///     跨表引用值在目标表中不存在。
    /// </summary>
    ReferencedKeyNotFound,

    /// <summary>
    ///     兜底的未分类失败。
    /// </summary>
    UnexpectedFailure
}