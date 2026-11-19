# Dodge the Creeps Advanced

[简体中文](#简体中文) · [English](#english)

## 简体中文

把领域规则和用例放在独立 C# 工程，在 Godot 工程接入原生玩法、界面、存档和模组。见[素材授权](ASSETS.md)。

[查看教程](https://gewuyou.github.io/GFramework/zh-CN/tutorials/godot-advanced-project.html)

## English

Keep domain rules and use cases in a plain C# project. Connect native gameplay, UI, saves, and mods in the Godot project. See the [asset licenses and credits](ASSETS.md).

[Read the walkthrough](https://gewuyou.github.io/GFramework/tutorials/godot-advanced-project.html)

## 运行 · Run

```bash
bash scripts/prepare-dodge-advanced-mods.sh
dotnet build Samples/DodgeTheCreeps.Advanced/DodgeTheCreeps.Advanced.csproj -c Debug
godot --headless --editor --path Samples/DodgeTheCreeps.Advanced --import
godot --path Samples/DodgeTheCreeps.Advanced
```

## 自检 · Smoke test

```bash
godot --headless --path Samples/DodgeTheCreeps.Advanced -- --smoke
```

用输出的标识执行 `--smoke-continue=<id>` 验证新进程读取。Use the output ID with `--smoke-continue=<id>` to check reads in a new process.
