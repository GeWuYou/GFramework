---
title: Arch ECS 接入
---

# Arch ECS 接入

ArchEcsModule 将 World 和 ECS 系统接入标准服务装配与显式生命周期。模块配置只登记描述，World 由正式 provider 拥有。

<<< ../../../GFramework.Ecs.Arch/ArchEcsModule.cs

应用先登记系统及其依赖，在组合根安装模块，等待架构就绪后由宿主调用 Update。系统通过构造函数取得 World 与实际服务。停止先结束已启动系统，再由 provider 释放 World；部分启动失败也遵循同一清理责任。

ECS 是实际需要实体数据与批量系统更新时的可选能力。普通应用服务无需通过 ECS 调度。
