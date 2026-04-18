# 抽象接口

`GFramework.*.Abstractions` 用来承载跨模块协作所需的契约，而不是运行时实现。

适合阅读这部分内容的场景：

- 你要做模块拆分，只想依赖接口，不想直接引用完整运行时
- 你要为测试、编辑器工具或插件提供替身实现
- 你在维护生成器、适配层或二次封装，需要先理解契约边界

## 阅读顺序

- Core 抽象层：[`core-abstractions.md`](./core-abstractions.md)
- Game 抽象层：[`game-abstractions.md`](./game-abstractions.md)

## 使用建议

- 如果你只是想直接使用框架功能，优先从对应运行时模块的 `README.md` 和栏目页开始。
- 只有在明确需要“契约层而非实现层”时，才单独依赖 `*.Abstractions` 包。
- 抽象层页面会解释接口分组与职责；实际安装与接入路径，仍应以运行时模块 README 与 `getting-started` 为主。