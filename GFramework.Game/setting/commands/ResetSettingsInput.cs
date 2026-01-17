using GFramework.Core.Abstractions.command;

namespace GFramework.Game.setting.commands;

public sealed class ResetSettingsInput : ICommandInput
{
    public Type? SettingsType { get; init; }
}