using GFramework.Core.Abstractions.cqrs.query;

namespace GFramework.Core.query;

/// <summary>
///     空查询输入类，用于表示不需要任何输入参数的查询操作
/// </summary>
/// <remarks>
///     该类实现了IQueryInput接口，作为占位符使用，适用于那些不需要额外输入参数的查询场景
/// </remarks>
public sealed class EmptyQueryInput : IQueryInput;