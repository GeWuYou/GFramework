# GFramework.Game.Abstractions

`GFramework.Game.Abstractions` 提供 `GFramework.Game` 的抽象层接口定义，用于解耦业务逻辑与具体实现。

## 主要内容

- 游戏业务常用抽象（数据、场景、设置、存储、UI 等）
- 与 `GFramework.Core.Abstractions` 配合使用的接口契约
- 供上层应用或扩展模块进行面向接口编程

## 使用建议

- 若你需要直接使用完整游戏扩展能力，优先安装 `GeWuYou.GFramework.Game`。
- 若你在做模块拆分、测试替身（Mock）或跨实现解耦，可单独依赖本包。
