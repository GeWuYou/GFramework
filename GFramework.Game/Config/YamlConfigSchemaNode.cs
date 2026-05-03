// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Config;

/// <summary>
///     表示单个 schema 节点的最小运行时描述。
///     同一个模型同时覆盖对象、数组和标量，便于递归校验逻辑只依赖一种树结构。
/// </summary>
internal sealed class YamlConfigSchemaNode
{
    private readonly NodeChildren _children;
    private readonly NodeValidation _validation;

    private YamlConfigSchemaNode(
        YamlConfigSchemaPropertyType nodeType,
        NodeChildren children,
        NodeValidation validation,
        string schemaPathHint)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(schemaPathHint);

        _children = children;
        _validation = validation;
        NodeType = nodeType;
        Properties = children.Properties;
        RequiredProperties = children.RequiredProperties;
        ItemNode = children.ItemNode;
        ReferenceTableName = validation.ReferenceTableName;
        AllowedValues = validation.AllowedValues;
        Constraints = validation.Constraints;
        ArrayConstraints = validation.ArrayConstraints;
        ObjectConstraints = validation.ObjectConstraints;
        ConstantValue = validation.ConstantValue;
        NegatedSchemaNode = validation.NegatedSchemaNode;
        SchemaPathHint = schemaPathHint;
    }

    /// <summary>
    ///     获取节点类型。
    /// </summary>
    public YamlConfigSchemaPropertyType NodeType { get; }

    /// <summary>
    ///     获取对象属性集合；非对象节点时返回空。
    /// </summary>
    public IReadOnlyDictionary<string, YamlConfigSchemaNode>? Properties { get; }

    /// <summary>
    ///     获取对象必填属性集合；非对象节点时返回空。
    /// </summary>
    public IReadOnlyCollection<string>? RequiredProperties { get; }

    /// <summary>
    ///     获取数组元素节点；非数组节点时返回空。
    /// </summary>
    public YamlConfigSchemaNode? ItemNode { get; }

    /// <summary>
    ///     获取目标引用表名称；未声明跨表引用时返回空。
    /// </summary>
    public string? ReferenceTableName { get; }

    /// <summary>
    ///     获取节点允许值集合；未声明 <c>enum</c> 时返回空。
    /// </summary>
    public IReadOnlyCollection<YamlConfigAllowedValue>? AllowedValues { get; }

    /// <summary>
    ///     获取标量范围与长度约束；未声明时返回空。
    /// </summary>
    public YamlConfigScalarConstraints? Constraints { get; }

    /// <summary>
    ///     获取对象属性数量约束；未声明时返回空。
    /// </summary>
    public YamlConfigObjectConstraints? ObjectConstraints { get; }

    /// <summary>
    ///     获取数组元素数量约束；未声明时返回空。
    /// </summary>
    public YamlConfigArrayConstraints? ArrayConstraints { get; }

    /// <summary>
    ///     获取节点常量约束；未声明 <c>const</c> 时返回空。
    /// </summary>
    public YamlConfigConstantValue? ConstantValue { get; }

    /// <summary>
    ///     获取节点声明的 <c>not</c> 子 schema；未声明时返回空。
    /// </summary>
    public YamlConfigSchemaNode? NegatedSchemaNode { get; }

    /// <summary>
    ///     获取用于诊断显示的 schema 路径提示。
    ///     当前节点本身不记录独立路径，因此对象校验会回退到所属根 schema 路径。
    /// </summary>
    public string SchemaPathHint { get; }

    /// <summary>
    ///     创建对象节点描述。
    /// </summary>
    /// <param name="properties">对象属性集合。</param>
    /// <param name="requiredProperties">对象必填属性集合。</param>
    /// <param name="objectConstraints">对象属性数量约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>对象节点模型。</returns>
    public static YamlConfigSchemaNode CreateObject(
        IReadOnlyDictionary<string, YamlConfigSchemaNode>? properties,
        IReadOnlyCollection<string>? requiredProperties,
        YamlConfigObjectConstraints? objectConstraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Object,
            new NodeChildren(properties, requiredProperties, itemNode: null),
            new NodeValidation(
                referenceTableName: null,
                allowedValues: null,
                constraints: null,
                arrayConstraints: null,
                objectConstraints,
                constantValue: null,
                negatedSchemaNode: null),
            schemaPathHint);
    }

    /// <summary>
    ///     创建数组节点描述。
    /// </summary>
    /// <param name="itemNode">数组元素节点。</param>
    /// <param name="allowedValues">数组节点允许值集合。</param>
    /// <param name="arrayConstraints">数组元素数量约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>数组节点模型。</returns>
    public static YamlConfigSchemaNode CreateArray(
        YamlConfigSchemaNode itemNode,
        IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues,
        YamlConfigArrayConstraints? arrayConstraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            YamlConfigSchemaPropertyType.Array,
            new NodeChildren(properties: null, requiredProperties: null, itemNode),
            new NodeValidation(
                referenceTableName: null,
                allowedValues,
                constraints: null,
                arrayConstraints,
                objectConstraints: null,
                constantValue: null,
                negatedSchemaNode: null),
            schemaPathHint);
    }

    /// <summary>
    ///     创建标量节点描述。
    /// </summary>
    /// <param name="nodeType">标量节点类型。</param>
    /// <param name="referenceTableName">目标引用表名称。</param>
    /// <param name="allowedValues">标量允许值集合。</param>
    /// <param name="constraints">标量范围与长度约束。</param>
    /// <param name="schemaPathHint">用于错误信息的 schema 文件路径提示。</param>
    /// <returns>标量节点模型。</returns>
    public static YamlConfigSchemaNode CreateScalar(
        YamlConfigSchemaPropertyType nodeType,
        string? referenceTableName,
        IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues,
        YamlConfigScalarConstraints? constraints,
        string schemaPathHint)
    {
        return new YamlConfigSchemaNode(
            nodeType,
            NodeChildren.None,
            new NodeValidation(
                referenceTableName,
                allowedValues,
                constraints,
                arrayConstraints: null,
                objectConstraints: null,
                constantValue: null,
                negatedSchemaNode: null),
            schemaPathHint);
    }

    /// <summary>
    ///     基于当前节点复制一个只替换引用表名称的新节点。
    ///     该方法用于把数组级别的 ref-table 语义挂接到元素节点上。
    /// </summary>
    /// <param name="referenceTableName">新的目标引用表名称。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithReferenceTable(string referenceTableName)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithReferenceTable(referenceTableName),
            SchemaPathHint);
    }

    /// <summary>
    ///     基于当前节点复制一个只替换 <c>enum</c> 允许值集合的新节点。
    /// </summary>
    /// <param name="allowedValues">新的允许值集合。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithAllowedValues(IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithAllowedValues(allowedValues),
            SchemaPathHint);
    }

    /// <summary>
    ///     基于当前节点复制一个只替换常量约束的新节点。
    /// </summary>
    /// <param name="constantValue">新的常量约束。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithConstantValue(YamlConfigConstantValue? constantValue)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithConstantValue(constantValue),
            SchemaPathHint);
    }

    /// <summary>
    ///     基于当前节点复制一个只替换 <c>not</c> 子 schema 的新节点。
    /// </summary>
    /// <param name="negatedSchemaNode">新的 negated schema。</param>
    /// <returns>复制后的节点。</returns>
    public YamlConfigSchemaNode WithNegatedSchemaNode(YamlConfigSchemaNode? negatedSchemaNode)
    {
        return new YamlConfigSchemaNode(
            NodeType,
            _children,
            _validation.WithNegatedSchemaNode(negatedSchemaNode),
            SchemaPathHint);
    }

    private sealed class NodeChildren
    {
        public NodeChildren(
            IReadOnlyDictionary<string, YamlConfigSchemaNode>? properties,
            IReadOnlyCollection<string>? requiredProperties,
            YamlConfigSchemaNode? itemNode)
        {
            Properties = properties;
            RequiredProperties = requiredProperties;
            ItemNode = itemNode;
        }

        public static NodeChildren None { get; } = new(properties: null, requiredProperties: null, itemNode: null);

        public IReadOnlyDictionary<string, YamlConfigSchemaNode>? Properties { get; }

        public IReadOnlyCollection<string>? RequiredProperties { get; }

        public YamlConfigSchemaNode? ItemNode { get; }
    }

    private sealed class NodeValidation
    {
        public NodeValidation(
            string? referenceTableName,
            IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues,
            YamlConfigScalarConstraints? constraints,
            YamlConfigArrayConstraints? arrayConstraints,
            YamlConfigObjectConstraints? objectConstraints,
            YamlConfigConstantValue? constantValue,
            YamlConfigSchemaNode? negatedSchemaNode)
        {
            ReferenceTableName = referenceTableName;
            AllowedValues = allowedValues;
            Constraints = constraints;
            ArrayConstraints = arrayConstraints;
            ObjectConstraints = objectConstraints;
            ConstantValue = constantValue;
            NegatedSchemaNode = negatedSchemaNode;
        }

        public string? ReferenceTableName { get; }

        public IReadOnlyCollection<YamlConfigAllowedValue>? AllowedValues { get; }

        public YamlConfigScalarConstraints? Constraints { get; }

        public YamlConfigArrayConstraints? ArrayConstraints { get; }

        public YamlConfigObjectConstraints? ObjectConstraints { get; }

        public YamlConfigConstantValue? ConstantValue { get; }

        public YamlConfigSchemaNode? NegatedSchemaNode { get; }

        public NodeValidation WithReferenceTable(string referenceTableName)
        {
            return new NodeValidation(referenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue, NegatedSchemaNode);
        }

        public NodeValidation WithAllowedValues(IReadOnlyCollection<YamlConfigAllowedValue>? allowedValues)
        {
            return new NodeValidation(ReferenceTableName, allowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue, NegatedSchemaNode);
        }

        public NodeValidation WithConstantValue(YamlConfigConstantValue? constantValue)
        {
            return new NodeValidation(ReferenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, constantValue, NegatedSchemaNode);
        }

        public NodeValidation WithNegatedSchemaNode(YamlConfigSchemaNode? negatedSchemaNode)
        {
            return new NodeValidation(ReferenceTableName, AllowedValues, Constraints, ArrayConstraints,
                ObjectConstraints, ConstantValue, negatedSchemaNode);
        }
    }
}
