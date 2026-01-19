using GFramework.Core.Abstractions.storage;

namespace GFramework.Game.Abstractions.storage;

/// <summary>
/// 文件存储接口，定义了文件存储操作的契约
/// 继承自IStorage接口，提供专门针对文件的存储功能
/// </summary>
public interface IFileStorage : IStorage;