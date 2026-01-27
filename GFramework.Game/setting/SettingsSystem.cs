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
        // 遍历所有设置配置并尝试应用
        foreach (var section in _model.All()) await TryApplyAsync(section);
    }

    /// <summary>
    ///     应用指定类型的设置配置
    /// </summary>
    /// <typeparam name="T">设置配置类型，必须是类且实现ISettingsSection接口</typeparam>
    /// <returns>完成的任务</returns>
    public Task Apply<T>() where T : class, ISettingsSection
    {
        return Apply(typeof(T));
    }

    /// <summary>
    ///     应用指定类型的设置配置
    /// </summary>
    /// <param name="settingsType">设置配置类型</param>
    /// <returns>完成的任务</returns>
    public async Task Apply(Type settingsType)
    {
        if (_model.TryGet(settingsType, out var section))
        {
            await TryApplyAsync(section);
        }
    }

    /// <summary>
    ///     应用指定类型集合的设置配置
    /// </summary>
    /// <param name="settingsTypes">设置配置类型集合</param>
    /// <returns>完成的任务</returns>
    public async Task Apply(IEnumerable<Type> settingsTypes)
    {
        // 去重后遍历设置类型，获取并应用对应的设置配置
        foreach (var type in settingsTypes.Distinct())
        {
            if (_model.TryGet(type, out var section))
            {
                await TryApplyAsync(section);
            }
        }
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