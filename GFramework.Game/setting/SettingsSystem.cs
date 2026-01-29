using GFramework.Core.extensions;
using GFramework.Core.system;
using GFramework.Game.Abstractions.data;
using GFramework.Game.Abstractions.setting;
using GFramework.Game.setting.events;

namespace GFramework.Game.setting;

/// <summary>
///     设置系统，负责管理和应用各种设置配置
/// </summary>
public class SettingsSystem<TRepository>(IDataRepository? repository)
    : AbstractSystem, ISettingsSystem where TRepository : class, IDataRepository
{
    private ISettingsModel _model = null!;
    private IDataRepository? _repository = repository;
    private IDataRepository Repository => _repository ?? throw new InvalidOperationException("Repository is not set");

    /// <summary>
    ///     应用所有设置配置
    /// </summary>
    /// <returns>完成的任务</returns>
    public async Task ApplyAll()
    {
        // 遍历所有设置应用器并尝试应用
        foreach (var applicator in _model.AllApplicators())
        {
            await TryApplyAsync(applicator);
        }
    }

    /// <summary>
    ///     应用指定类型的设置配置
    /// </summary>
    /// <typeparam name="T">设置配置类型，必须是类且实现ISettingsSection接口</typeparam>
    /// <returns>完成的任务</returns>
    public Task Apply<T>() where T : class, IApplyAbleSettings
    {
        var applicator = _model.GetApplicator<T>();
        return applicator != null
            ? TryApplyAsync(applicator)
            : Task.CompletedTask;
    }

    /// <summary>
    ///     保存所有设置数据到存储库
    /// </summary>
    /// <returns>完成的任务</returns>
    public async Task SaveAll()
    {
        // 遍历所有设置数据并保存可持久化的数据
        foreach (var data in _model.AllData())
        {
            if (data is IData persistable)
            {
                await Repository.SaveAsync(persistable);
            }
        }
    }

    /// <summary>
    /// 重置所有设置并应用更改
    /// </summary>
    /// <returns>异步任务</returns>
    public async Task ResetAll()
    {
        _model.ResetAll();
        await ApplyAll();
    }

    /// <summary>
    /// 重置指定类型的设置并应用更改
    /// </summary>
    /// <typeparam name="T">设置类型，必须实现IPersistentApplyAbleSettings接口且具有无参构造函数</typeparam>
    /// <returns>异步任务</returns>
    public async Task Reset<T>() where T : class, IPersistentApplyAbleSettings, new()
    {
        _model.Reset<T>();
        await Apply<T>();
    }


    /// <summary>
    ///     初始化设置系统，获取设置模型实例
    /// </summary>
    protected override void OnInit()
    {
        _model = this.GetModel<ISettingsModel>()!;
        _repository ??= this.GetUtility<TRepository>()!;
    }

    /// <summary>
    ///     尝试应用可应用的设置配置
    /// </summary>
    /// <param name="section">设置配置对象</param>
    private async Task TryApplyAsync(ISettingsSection section)
    {
        if (section is not IApplyAbleSettings applyAbleSettings) return;

        // 发送设置应用中事件
        this.SendEvent(new SettingsApplyingEvent<ISettingsSection>(section));

        try
        {
            await applyAbleSettings.Apply();
            // 发送设置应用成功事件
            this.SendEvent(new SettingsAppliedEvent<ISettingsSection>(section, true));
        }
        catch (Exception ex)
        {
            // 发送设置应用失败事件
            this.SendEvent(new SettingsAppliedEvent<ISettingsSection>(section, false, ex));
        }
    }
}