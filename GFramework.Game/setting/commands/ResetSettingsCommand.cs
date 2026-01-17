using GFramework.Core.command;
using GFramework.Game.Abstractions.setting;
using GFramework.Game.setting.events;

namespace GFramework.Game.setting.commands;

public sealed class ResetSettingsCommand : AbstractAsyncCommand<ResetSettingsInput>
{
    private ISettingsModel _model = null!;
    private ISettingsPersistence _persistence = null!;

    protected override void OnContextReady()
    {
        base.OnContextReady();
        _model = this.GetModel<ISettingsModel>()!;
        _persistence = this.GetUtility<ISettingsPersistence>()!;
    }

    protected override async Task OnExecuteAsync(ResetSettingsInput input)
    {
        if (input.SettingsType == null)
            await ResetAll();
        else
            await ResetSingle(input.SettingsType);
    }

    private async Task ResetSingle(Type settingsType)
    {
        if (!_model.TryGet(settingsType, out var section))
            throw new InvalidOperationException($"Settings {settingsType.Name} not found");

        if (section is not ISettingsData settingsData)
            throw new InvalidOperationException($"Settings {settingsType.Name} is not ISettingsData");

        settingsData.Reset();
        await _persistence.SaveAsync(settingsData);
        this.SendEvent(new SettingsResetEvent<ISettingsSection>(settingsData));
    }

    private async Task ResetAll()
    {
        var allSettings = _model.All()
            .OfType<ISettingsData>()
            .ToList();

        foreach (var settings in allSettings)
        {
            settings.Reset();
            await _persistence.SaveAsync(settings);
        }

        this.SendEvent(new SettingsResetAllEvent(allSettings));
    }
}