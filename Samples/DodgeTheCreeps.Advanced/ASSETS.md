# 官方教程素材

本示例参考 Godot 官方 [Your first 2D game / Dodge the Creeps!](https://docs.godotengine.org/en/stable/getting_started/first_2d_game/index.html)，使用教程提供的原始图片、声音与字体。玩法代码在本仓库重新编写，菜单、暂停、设置与持久化接入 GFramework。

## 来源

- 官方下载：[Dodge the Creeps 2D assets](https://github.com/godotengine/godot-docs-project-starters/releases/download/latest-4.x/dodge_the_creeps_2d_assets.zip)。
- 下载 ZIP 的 SHA-256：6d86d06ad3d3787d8a63e3cd2cd8f294cb52053ae13aaf7e3790288a63cc7006。
- 素材文件从 ZIP 的 art/ 和 fonts/ 原样复制。
- 署名依据：[官方 C# 示例的许可说明](https://github.com/godotengine/godot-demo-projects/tree/master/mono/dodge_the_creeps#license)。

## 许可与署名

| 素材 | 作者与来源 | 许可 |
| --- | --- | --- |
| 玩家和敌人 PNG | Kenney，2016 年，[Abstract Platformer](https://www.kenney.nl/assets/abstract-platformer) | [CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/) |
| House In a Forest Loop.ogg | Copyright © 2012 [HorrorPen](https://opengameart.org/users/horrorpen)，[原始作品](https://opengameart.org/content/loop-house-in-a-forest) | [Creative Commons Attribution 3.0](https://creativecommons.org/licenses/by/3.0/)；本示例未修改音频 |
| gameover.wav | Godot 官方教程素材；随官方示例分发 | 官方示例 MIT 许可；许可全文保存在 [素材许可证](art/LICENSE-MIT.txt) |
| Xolonium Regular | Copyright © 2011–2016 Severin Meyer，Reserved Font Name Xolonium | SIL Open Font License 1.1；原始 [字体许可证](fonts/LICENSE.txt) 和 [字体说明](fonts/FONTLOG.txt) 随示例保留 |

本仓库新编写的示例源码沿用仓库 Apache-2.0 许可。分发示例或导出游戏时，请随附本页及对应素材许可证，保留 HorrorPen 音乐署名和 Xolonium 字体许可。

## 内容包素材

Coral Garden 的 `sprites/creep.png` 原样复制自 `art/enemySwimming_1.png`，`audio/reef.ogg` 原样复制自 `art/House In a Forest Loop.ogg`，沿用上表的来源、许可和音乐署名。仓库中的 `.source` 后缀仅用于保留导出前的原始字节，安装时恢复正常扩展名，没有修改素材内容。

Moon Arena 的多边形装饰场景由本示例编写，源文件位于 `official-pack/`，沿用 Apache-2.0 许可；`build-official-pack.gd` 可重新生成对应 PCK。

## 简体中文界面字体

Noto Sans SC 可变字体来自 Google Fonts 官方仓库，固定修订为 `86aced91de05cd4f96a06838124cca45be337b2b`。字体按原样复制，未经修改或子集化，以完整字符集支持内容作者提供的中文名称。该字体以 SIL Open Font License 1.1 分发，保留 Adobe 版权声明及 Reserved Font Name `Source` 声明。

- [字体原文件](https://raw.githubusercontent.com/google/fonts/86aced91de05cd4f96a06838124cca45be337b2b/ofl/notosanssc/NotoSansSC%5Bwght%5D.ttf)。
- SHA-256：`a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da`。
- 分发游戏时随附[字体许可证](fonts/NotoSansSC-OFL.txt)。

## 代码模组补丁运行时

示例通过可选的 GFramework Harmony 适配包使用 Lib.Harmony 2.4.3（Andreas Pardeike，MIT）。导出时包含其[许可证原文](third-party-licenses/Harmony-MIT.txt)。框架库的依赖关系以 NuGet 包元数据为准。
