# Dodge the Creeps 领域与应用

此项目只引用 .NET 基础库。GameSession 维护存档资格与未保存状态，RunRules 验证完整棋盘，RunUseCases 通过 IRunBoard、IRunStore 组织开始、保存和死亡清档。设置规则也在本项目，Godot 音频、窗口、文件、内容包和菜单由宿主适配。

它不使用容器、全局 Context、引擎类型或框架角色基类。主程序集在 [Godot 进阶示例](../DodgeTheCreeps.Advanced/README.md) 装配这些对象。
