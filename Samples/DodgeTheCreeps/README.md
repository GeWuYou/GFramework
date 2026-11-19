# Dodge the Creeps

[简体中文](#简体中文) · [English](#english)

## 简体中文

用 WASD 或方向键躲避敌人。打开暂停菜单调整设置、保存和返回菜单，再选择继续。复用素材时查看[授权说明](ASSETS.md)。

[查看教程](https://gewuyou.github.io/GFramework/zh-CN/tutorials/godot-complete-project.html)

## English

Use WASD or the arrows to dodge enemies. Open the pause menu to change settings, save, and return to the menu, then choose Continue. See the [asset licenses and credits](ASSETS.md) before reusing assets.

[Read the walkthrough](https://gewuyou.github.io/GFramework/tutorials/godot-complete-project.html)

## 运行 · Run

```bash
python3 scripts/build-dodge-mods.py
dotnet build Samples/DodgeTheCreeps/DodgeTheCreeps.csproj -c Debug
godot --headless --editor --path Samples/DodgeTheCreeps --import
godot --path Samples/DodgeTheCreeps
```

## 自检 · Smoke test

```bash
godot --headless --path Samples/DodgeTheCreeps -- --smoke
```

用输出的标识执行 `--smoke-continue=<id>` 验证新进程读取。Use the output ID with `--smoke-continue=<id>` to check reads in a new process.
