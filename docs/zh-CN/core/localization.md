---
title: "游戏文本本地化"
description: "用语言目录和 JSON 表加载文本，并在语言变化时刷新界面。"
---

# 游戏文本本地化 {#overview}

## 准备文本表 {#tables}

将 `LocalizationConfig.LocalizationPath` 指向宿主可访问的文件系统目录。每种语言一个子目录，每个 JSON 文件一张表，文件名用作表名。例如 `localization/eng/menu.json`：

```json
{ "play": "Play", "settings": "Settings" }
```

在启动时初始化 `LocalizationManager`，调用 `GetText("menu", "play")` 读取字符串。设置默认语言和回退语言，给回退语言提供完整的表。用 `TryGetText` 处理可选条目。

## 切换语言 {#switch}

通过 `SetLanguage` 切换。订阅语言变化时保存同一个回调，以便视图关闭时调用 `UnsubscribeFromLanguageChange`。动态文本使用 `GetString`，按需要注册格式化器。
