---
title: "生成集合注册"
description: "将可枚举配置逐项注册到宿主 registry。"
---

# 生成集合注册 {#overview}

给 `partial` 宿主类型添加 `[AutoRegisterExportedCollections]`。给可读集合成员添加 `[RegisterExportedCollection(...)]`，指向存在的 registry 成员和兼容的单参数方法。典型输入是 Inspector 中准备的资源数组。

集合和 registry 均准备好后，显式调用 `__RegisterExportedCollections_Generated()`，例如在 `_Ready()`。用编译诊断定位不可枚举成员或不匹配的注册方法。
