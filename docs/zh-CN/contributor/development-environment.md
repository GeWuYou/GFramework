---
title: 开发环境能力清单
description: 说明 GFramework 当前开发和 AI 协作依赖的环境能力、推荐工具与刷新方式。
---

# 开发环境能力清单

这份文档只记录对 `GFramework` 当前开发和 AI 协作真正有用的环境能力，不收录与本项目无关的系统工具。

如果某个工具没有出现在这里，默认表示它对当前仓库不是必需项，AI 也不应因为“系统里刚好装了”就优先使用它。

## 当前验证环境

当前仓库主要在下面这组环境中验证：

- **运行环境**：WSL2
- **发行版**：Ubuntu 24.04 LTS
- **Shell**：`bash`

机器可读的环境数据分成两层：

- `GFramework/.ai/environment/tools.raw.yaml`：完整事实采集
- `GFramework/.ai/environment/tools.ai.yaml`：给 AI 看的精简决策提示

AI 应优先读取 `tools.ai.yaml`，只有在需要追溯完整事实时才查看 `tools.raw.yaml`。

## 当前项目需要的运行时

| 工具        | 是否需要 | 在 GFramework 中的用途               |
|-----------|------|---------------------------------|
| `dotnet`  | 必需   | 构建、测试、打包整个解决方案                  |
| `python3` | 推荐   | 运行本地辅助脚本、环境采集和轻量自动化             |
| `node`    | 推荐   | 作为文档工具链的 JavaScript 运行时         |
| `bun`     | 推荐   | 安装并预览 `docs/` 下的 VitePress 文档站点 |

## 当前项目需要的命令行工具

| 工具       | 是否需要 | 在 GFramework 中的用途                             |
|----------|------|-----------------------------------------------|
| `git`    | 必需   | 提交代码、查看 diff、审查变更                             |
| `bash`   | 必需   | 执行仓库脚本，例如 `scripts/validate-csharp-naming.sh` |
| `rg`     | 必需   | 在仓库中快速搜索代码和文档                                 |
| `jq`     | 推荐   | 处理 JSON 输出，便于本地脚本和 AI 做结构化检查                  |
| `docker` | 可选   | 运行 MegaLinter 等容器化检查工具                        |

这里只保留和当前仓库直接相关的 CLI。像 `kubectl`、`terraform`、`helm`、`java`、数据库客户端等工具，即使系统已安装，也不进入正式清单。

## Python 包

Python 包只记录两类内容：

- 当前环境里已经存在、对开发辅助有价值的包
- 明确对 AI/脚本化开发有帮助、后续可能会安装的包

| 包          | 当前状态    | 用途                  |
|------------|---------|---------------------|
| `requests` | 当前环境已安装 | 用于简单 HTTP 调用和脚本集成   |
| `rich`     | 当前环境已安装 | 用于更易读的终端输出          |
| `openai`   | 当前环境可选  | 用于脚本化调用 OpenAI API  |
| `tiktoken` | 当前环境可选  | 用于 token 估算和上下文检查   |
| `pydantic` | 当前环境可选  | 用于结构化配置和模式校验        |
| `pytest`   | 当前环境可选  | 用于 Python 辅助脚本的小型测试 |

如果某个 Python 包与当前仓库没有直接关系，就不要加入清单。

## AI 使用约定

AI 在这个仓库里应优先使用：

- `rg` 做文本搜索
- `jq` 做 JSON 检查
- `bash` 执行仓库脚本
- `dotnet` 做构建和测试
- `bun` 做文档预览
- `python3 + requests` 做轻量本地辅助脚本

AI 不应直接把原始探测数据当成决策规则；应以 `tools.ai.yaml` 中的推荐和 fallback 为准。如果确实需要引入新工具，应先更新环境清单，再在任务中使用。

## 如何刷新环境清单

使用仓库脚本先采集原始环境，再生成 AI 版本：

```bash
# 输出原始环境清单到终端
bash scripts/collect-dev-environment.sh --check

# 写回原始清单
bash scripts/collect-dev-environment.sh --write

# 由原始清单生成 AI 决策清单
python3 scripts/generate-ai-environment.py
```

## 文档站 LLM 索引接入说明

### 访问路径

LLM 索引文件与文档站一起部署在 GitHub Pages，遵循 `docs/.vitepress/config.mts` 里 `base: '/GFramework/'` 的路径规则。部署之后可以直接访问 `https://gewuyou.github.io/GFramework/llms.txt` 和 `https://gewuyou.github.io/GFramework/llms-full.txt`。这些文件最终会被写入 `docs/.vitepress/dist/`，但生成动作发生在 `publish-docs` workflow 的 `demodrive-ai/llms-txt-action` 步骤，而不是单独执行 `bun run build` 时直接产出。

### 生成时机与依赖

`demodrive-ai/llms-txt-action` 负责把文档站打包后的页面转换成 LLM 索引，它的 `docs_dir` 已指定为 `docs/.vitepress/dist`，并通过 `sitemap.xml` 解析页面 URL。只能在 `bun run build` 之后（即 VitePress 将页面输出到 `dist` 并生成 `sitemap.xml`）执行；如果没有 sitemap，action 会得不到页面列表，生成的 `llms.txt` 就会不完整。

### 验证流程

1. 本地执行 `bun run build`，确认 `docs/.vitepress/dist/sitemap.xml` 已生成，并检查其中的 URL 是否与 GitHub Pages
   地址一致。添加或删除文档页面后必须重新运行一次全量构建。
2. 在 Pull Request 或发布前查看 `publish-docs` workflow 日志，确认 `Verify LLM artifacts` 步骤通过，并检查
   `docs/.vitepress/dist/llms.txt`、`docs/.vitepress/dist/llms-full.txt` 已作为 Pages artifact 上传。
3. 部署完成后通过 GitHub Pages 打开 `https://gewuyou.github.io/GFramework/llms.txt`
   和 `https://gewuyou.github.io/GFramework/llms-full.txt`，确认可访问且内容覆盖最新页面。
4. 如果后续需要对 LLM 索引行为做变更，优先思考是否影响 `sitemap` 结构或 `docs_dir` 路径；失效通常表现为 `llms`
   文件缺失、内容为空，或链接仍指向旧页面。

## 维护规则

- 目标不是记录“这台机器装了什么”，而是记录“GFramework 开发和 AI 协作实际该用什么”。
- 新工具只有在满足以下条件之一时才应加入清单：
    - 当前仓库构建、测试、文档或验证直接依赖它
    - AI 在当前仓库中会高频使用，且能明显提升效率
    - 新贡献者配置当前仓库开发环境时确实需要知道它
- 不满足上述条件的工具，不写入文档，也不写入 `.ai/environment/tools.raw.yaml` / `.ai/environment/tools.ai.yaml`。
