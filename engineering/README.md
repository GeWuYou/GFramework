# GFramework 项目开发文档

这里维护当前架构、设计决策、开发规范与实施路线。框架使用说明仍在[文档站入口](../docs/zh-CN/index.md)，源码、项目文件和测试决定当前行为。

## 架构与设计

- [当前架构](architecture/current-architecture.md)：包依赖、启动与销毁链、宿主和生成器边界。
- [核心设计评估](architecture/core-assessment.md)：源码依据、诊断结果及保留、调整、移除、暂缓清单。
- [ADR：标准 .NET 基础与可选编排](architecture/adr-001-standard-dotnet-foundation.md)：目标方向、取舍和适用边界。
- [首批核心改造设计](architecture/first-core-batch.md)：本批接口、资源责任、调用阶段和验收场景；实施状态见路线图。

## 开发规范

- [开发与验证](development/workflow.md)：仓库独立使用、验证入口及本地证据。
- [文档维护](development/documentation.md)：事实来源、读者边界、索引和更新责任。
- [贡献规则](../AGENTS.md)：编码、测试、提交和安全要求。

## 路线与任务

- [路线图入口](roadmap/README.md)：状态含义和任务维护方式。
- [核心治理任务](roadmap/core-governance.md)：本轮文档交付、下一批核心改造及后续收敛顺序。
- [保留的后续事项](roadmap/retained-followups.md)：从历史记录提取、经当前源码核对仍值得保留的非核心事项。

任务状态只维护在路线图。这里不保存逐轮执行流水、分支映射或机器环境信息；临时资料放入忽略的 `.local/`。
