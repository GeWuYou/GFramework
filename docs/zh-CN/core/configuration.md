---
title: 应用配置与游戏配置
---

# 应用配置与游戏配置

启动配置、运行中偏好和游戏数据具有不同的更新与持久化需求。

## 应用启动配置

使用 Microsoft.Extensions.Configuration 读取配置源，以类型化 Options 提供给需要它的服务，并在启动时验证。配置读取失败应明确报告，不能把错误转换成合法的 0、false 或空字符串。

采用标准 Host 时使用其配置与 Options；使用现有引擎宿主时，也可在组合根加载并验证参数对象后直接注入，无需另建 Host 循环。

## 游戏配置与偏好

游戏数据表、首载及热重载使用 [Game 配置系统](../game/config-system.md)。玩家设置由设置服务验证，通过存储能力持久化，发布到 UI 前保持有效状态。不要把所有数据汇入可变的字符串对象字典。

参考 [.NET Options](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)。
