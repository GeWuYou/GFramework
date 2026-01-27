using GFramework.Core.extensions;
using GFramework.Core.system;
using GFramework.Game.Abstractions.setting;
using GFramework.Game.setting.events;

namespace GFramework.Game.setting;

/// <summary>
///     设置系统，负责管理和应用各种设置配置
/// </summary>
public class SettingsSystem : AbstractSystem, ISettingsSystem
{
    private ISettingsModel _model = null!;

    /// <summary>
    ///     应用所有设置配置
    /// </summary>
    /// <returns>完成的任务</returns>
    public async Task ApplyAll()
    {
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
    ///     初始化设置系统，获取设置模型实例
    /// </summary>
    protected override void OnInit()
    {
        _model = this.GetModel<ISettingsModel>()!;
    }

    /// <summary>
    ///     尝试应用可应用的设置配置
    /// </summary>
    /// <param name="section">设置配置对象</param>
    private async Task TryApplyAsync(ISettingsSection section)
    {
        if (section is not IApplyAbleSettings applyAbleSettings) return;

        this.SendEvent(new SettingsApplyingEvent<ISettingsSection>(section));

        try
        {
            await applyAbleSettings.Apply();
            this.SendEvent(new SettingsAppliedEvent<ISettingsSection>(section, true));
        }
        catch (Exception ex)
        {
            this.SendEvent(new SettingsAppliedEvent<ISettingsSection>(section, false, ex));
        }
    }
}