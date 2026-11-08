# 进阶示例测试

测试纯 C# 用例的先后顺序、取消、失败保存、死亡清档、未保存移动与配置验证，并检查领域程序集不引用引擎和框架。它们不启动 Godot，不能替代主工程的原生验收。

运行：`dotnet test Samples/DodgeTheCreeps.Advanced.Tests -c Release`。Godot 侧继续使用示例中直接驱动真实按钮、节点与帧的 smoke 入口。
