using Array = Godot.Collections.Array;

namespace GFramework.Godot.Text;

/// <summary>
///     抽象可被富文本效果控制器驱动的宿主。
///     该接口把装配决策从 Godot 原生 <see cref="RichTextLabel" /> 生命周期中解耦出来，便于在纯托管测试中验证开关、
///     配置回退和注册表替换行为。
/// </summary>
internal interface IRichTextEffectHost
{
    /// <summary>
    ///     获取或设置宿主是否启用 BBCode 解析。
    /// </summary>
    bool BbcodeEnabled { get; set; }

    /// <summary>
    ///     获取或设置当前安装到宿主上的自定义富文本效果集合。
    /// </summary>
    Array CustomEffects { get; set; }
}
