# 分析器

> `GFramework.SourceGenerators` 当前随包提供的诊断分析器。

## 概述

除了生成器本身，仓库当前还包含两个与使用方式直接相关的分析器：

- `ContextRegistrationAnalyzer`
- `PriorityUsageAnalyzer`

它们都位于 `GFramework.SourceGenerators.Analyzers` 命名空间下。

## ContextRegistrationAnalyzer

### 作用

检查以下使用点在所属架构里是否存在静态可见注册：

- 带 `[GetModel]` / `[GetModels]` 的字段
- 带 `[GetSystem]` / `[GetSystems]` 的字段
- 带 `[GetUtility]` / `[GetUtilities]` 的字段
- 手写的 `GetModel<T>()` / `GetSystem<T>()` / `GetUtility<T>()` 调用

当前分析器不会对 Service 做同类警告。

### 当前诊断

| 诊断 ID                        | 含义                       |
|------------------------------|--------------------------|
| `GF_ContextRegistration_001` | Model 使用点在架构中找不到静态可见注册   |
| `GF_ContextRegistration_002` | System 使用点在架构中找不到静态可见注册  |
| `GF_ContextRegistration_003` | Utility 使用点在架构中找不到静态可见注册 |

### 处理方式

- 确认目标类型已在所属架构的初始化路径中注册
- 如果你依赖的是运行时动态注册，需接受该分析器无法静态证明的限制

## PriorityUsageAnalyzer

### 作用

当你对实现了 `IPrioritized` 的类型使用 `GetAll<T>()` 时，提示改用 `GetAllByPriority<T>()`。

### 当前诊断

| 诊断 ID                   | 含义                                           |
|-------------------------|----------------------------------------------|
| `GF_Priority_Usage_001` | 建议把 `GetAll<T>()` 改为 `GetAllByPriority<T>()` |

### 处理方式

- 如果顺序很重要，改用 `GetAllByPriority<T>()`
- 如果顺序无关，可以显式接受该提示
