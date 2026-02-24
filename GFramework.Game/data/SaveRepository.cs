// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Globalization;
using GFramework.Core.Abstractions.storage;
using GFramework.Core.utility;
using GFramework.Game.Abstractions.data;
using GFramework.Game.storage;

namespace GFramework.Game.data;

/// <summary>
///     基于槽位的存档仓库实现
/// </summary>
/// <typeparam name="TSaveData">存档数据类型</typeparam>
public class SaveRepository<TSaveData> : AbstractContextUtility, ISaveRepository<TSaveData>
    where TSaveData : class, IData, new()
{
    private readonly SaveConfiguration _config;
    private readonly IStorage _rootStorage;

    /// <summary>
    ///     初始化存档仓库
    /// </summary>
    /// <param name="storage">存储实例</param>
    /// <param name="config">存档配置</param>
    public SaveRepository(IStorage storage, SaveConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
        _rootStorage = new ScopedStorage(storage, config.SaveRoot);
    }

    /// <summary>
    ///     检查指定槽位是否存在存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>如果存档存在返回true，否则返回false</returns>
    public async Task<bool> ExistsAsync(int slot)
    {
        var storage = GetSlotStorage(slot);
        return await storage.ExistsAsync(_config.SaveFileName);
    }

    /// <summary>
    ///     加载指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>存档数据对象，如果不存在则返回新实例</returns>
    public async Task<TSaveData> LoadAsync(int slot)
    {
        var storage = GetSlotStorage(slot);

        if (await storage.ExistsAsync(_config.SaveFileName))
            return await storage.ReadAsync<TSaveData>(_config.SaveFileName);

        return new TSaveData();
    }

    /// <summary>
    ///     保存存档到指定槽位
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <param name="data">要保存的存档数据</param>
    public async Task SaveAsync(int slot, TSaveData data)
    {
        var slotPath = $"{_config.SaveSlotPrefix}{slot}";

        // 确保槽位目录存在
        if (!await _rootStorage.DirectoryExistsAsync(slotPath))
            await _rootStorage.CreateDirectoryAsync(slotPath);

        var storage = GetSlotStorage(slot);
        await storage.WriteAsync(_config.SaveFileName, data);
    }

    /// <summary>
    ///     删除指定槽位的存档
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    public async Task DeleteAsync(int slot)
    {
        var storage = GetSlotStorage(slot);
        await storage.DeleteAsync(_config.SaveFileName);
    }

    /// <summary>
    ///     列出所有有效的存档槽位
    /// </summary>
    /// <returns>包含所有有效存档槽位编号的只读列表，按升序排列</returns>
    public async Task<IReadOnlyList<int>> ListSlotsAsync()
    {
        // 列举所有槽位目录
        var directories = await _rootStorage.ListDirectoriesAsync();

        var slots = new List<int>();

        foreach (var dirName in directories)
        {
            // 检查目录名是否符合槽位前缀
            if (!dirName.StartsWith(_config.SaveSlotPrefix, StringComparison.Ordinal))
                continue;

            // 提取槽位编号
            var slotStr = dirName[_config.SaveSlotPrefix.Length..];
            if (!int.TryParse(slotStr, CultureInfo.InvariantCulture, out var slot))
                continue;

            // 检查槽位中是否存在存档文件
            if (await ExistsAsync(slot))
                slots.Add(slot);
        }

        return slots.OrderBy(x => x).ToList();
    }

    /// <summary>
    ///     获取指定槽位的存储对象
    /// </summary>
    /// <param name="slot">存档槽位编号</param>
    /// <returns>对应槽位的存储对象</returns>
    private IStorage GetSlotStorage(int slot)
    {
        return new ScopedStorage(_rootStorage, $"{_config.SaveSlotPrefix}{slot}");
    }

    /// <summary>
    ///     初始化逻辑
    /// </summary>
    protected override void OnInit()
    {
    }
}