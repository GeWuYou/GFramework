using GFramework.Core.extensions;
using GFramework.Core.system;
using GFramework.Game.Abstractions.setting;
using GFramework.Game.setting.commands;
using GFramework.Game.setting.events;

namespace GFramework.Game.setting;

/// <summary>
/// 设置系统，负责管理和应用各种设置配置
/// </summary>
public class SettingsSystem : AbstractSystem, ISettingsSystem
{
    private ISettingsModel _model = null!;

    /// <summary>
    /// 应用所有设置配置
    /// </summary>
    /// <returns>完成的任务</returns>
    public Task ApplyAll()
    {
        // 遍历所有设置配置并尝试应用
        foreach (var section in _model.All())
        {
            TryApply(section);
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(Type settingsType)
    {
        return this.SendCommandAsync(new ResetSettingsCommand(new ResetSettingsInput
        {
            SettingsType = settingsType
        }));
    }

    public Task ResetAsync<T>() where T : class, ISettingsData, new()
    {
        return ResetAsync(typeof(T));
    }

    public Task ResetAllAsync()
    {
        return this.SendCommandAsync(new ResetSettingsCommand(new ResetSettingsInput
        {
            SettingsType = null
        }));
    }

    private void TryApply(ISettingsSection section)
    {
        if (section is IApplyAbleSettings applyable)
        {
            this.SendEvent(new SettingsApplyingEvent<ISettingsSection>(section));

            try
            {
                applyable.Apply();
                this.SendEvent(new SettingsAppliedEvent<ISettingsSection>(section, true));
            }
            catch (Exception ex)
            {
                this.SendEvent(new SettingsAppliedEvent<ISettingsSection>(section, false, ex));
                throw;
            }
        }
    }
}