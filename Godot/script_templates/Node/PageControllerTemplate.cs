// meta-name: UI页面控制器类模板
// meta-description: 负责管理UI页面场景的生命周期和架构关联
using Godot;
using GFramework.Core.Abstractions.controller;
using GFramework.Core.extensions;
using GFramework.Game.Abstractions.ui;
using GFramework.Godot.ui;
using GFramework.SourceGenerators.Abstractions.logging;
using GFramework.SourceGenerators.Abstractions.rule;


[ContextAware]
[Log]
public partial class _CLASS_ :_BASE_,IController,IUiPageBehaviorProvider,IUiPage
{
    /// <summary>
    /// 节点准备就绪时的回调方法
    /// 在节点添加到场景树后调用
    /// </summary>
    public override void _Ready()
    {
        
    }
	/// <summary>
    /// 页面行为实例的私有字段
    /// </summary>
	private IUiPageBehavior? _page;
    
    /// <summary>
    /// 获取页面行为实例，如果不存在则创建新的CanvasItemUiPageBehavior实例
    /// </summary>
    /// <returns>返回IUiPageBehavior类型的页面行为实例</returns>
    public IUiPageBehavior GetPage()
    {
        _page ??= new CanvasItemUiPageBehavior<_BASE_>(this);
        return _page;
    }
	
    /// <summary>
    /// 页面进入时调用的方法
    /// </summary>
    /// <param name="param">页面进入参数，可能为空</param>
    public void OnEnter(IUiPageEnterParam? param)
    {
        
    }
	/// <summary>
    /// 页面退出时调用的方法
    /// </summary>
    public void OnExit()
    {
        
    }


    /// <summary>
    /// 页面暂停时调用的方法
    /// </summary>
    public void OnPause()
    {
        
    }

    /// <summary>
    /// 页面恢复时调用的方法
    /// </summary>
    public void OnResume()
    {
        
    }

    /// <summary>
    /// 页面显示时调用的方法
    /// </summary>
    public void OnShow()
    {
       
    }

    /// <summary>
    /// 页面隐藏时调用的方法
    /// </summary>
    public void OnHide()
    {
        
    }
}