---
title: "资源租约"
description: "加载共享资源，并在最后一个使用者释放后交还给加载器。"
---

# 资源租约 {#overview}

## 获取与释放 {#leases}

1. 为资源类型登记 `IResourceLoader<T>`。
2. 通过 `AcquireAsync<T>` 加载并取得 `IResourceHandle<T>`。
3. 用 `Resource` 取得对象，使用完毕后释放句柄。
4. 需要将使用权交给其他组件时，通过 `Retain()` 创建独立租约。

缓存按请求类型和路径标识。资源的实际卸载由加载器处理。关闭时先归还租约，再异步释放管理器。

## 并发加载 {#loading}

同类型同路径的并发异步请求共享加载，每个调用获得自己的租约。将调用方取消令牌传给 `AcquireAsync`，让该调用按需停止等待。共享加载尚未完成时也用异步入口取得同一资源。

引擎项目见[加载 Godot 资源](../godot/resource.md)。
