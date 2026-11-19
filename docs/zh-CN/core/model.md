---
title: "领域状态"
description: "把游戏状态和业务约束放在拥有它的对象中。"
---

# 领域状态 {#overview}

通过业务方法开始一局、计分和结束，用只读属性或快照向调用方提供结果。界面读取自己的展示状态，保存或恢复时将领域快照转换为存档 DTO。

<<< ../../../Samples/DodgeTheCreeps.Advanced.Core/Gameplay/GameSession.cs
