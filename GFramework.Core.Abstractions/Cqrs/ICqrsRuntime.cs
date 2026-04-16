using System.ComponentModel;

namespace GFramework.Core.Abstractions.Cqrs;

/// <summary>
///     提供旧 <c>GFramework.Core.Abstractions.Cqrs</c> 命名空间下的 CQRS runtime 兼容别名。
/// </summary>
/// <remarks>
///     正式 runtime seam 已迁移到 <see cref="GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime" />，
///     但当前仍保留该接口以避免立即打断历史公开路径与既有二进制引用。
///     新代码应优先依赖 <c>GFramework.Cqrs.Abstractions.Cqrs</c> 下的正式契约。
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ICqrsRuntime : GFramework.Cqrs.Abstractions.Cqrs.ICqrsRuntime
{
}
