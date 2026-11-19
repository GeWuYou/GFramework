---
title: "声明服务与节点依赖"
description: "使用构造函数依赖和显式宿主装配。"
---

# 声明服务与节点依赖 {#overview}

在构造函数中声明服务长期使用的依赖，通过方法参数传入单次操作的数据。在组合根解析 `Architecture.Services`，把实际接口交给应用服务或视图。

对于 Godot 创建的节点，在拥有它的场景准备好所需服务后调用节点的初始化入口，传入状态和动作接口。参见[进阶 Godot 示例](../tutorials/godot-advanced-project.md)。
