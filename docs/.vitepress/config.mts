import { defineConfig } from 'vitepress'

const localSearch = {
  provider: 'local' as const,
  options: {
    translations: {
      button: {
        buttonText: '搜索',
        buttonAriaLabel: '搜索文档'
      },
      modal: {
        noResultsText: '无法找到相关结果',
        resetButtonTitle: '清除查询条件',
        footer: {
          selectText: '选择',
          navigateText: '切换',
          closeText: '关闭'
        }
      }
    }
  }
}

function safeGenericEscapePlugin() {
  return {
    name: 'safe-generic-escape',
    enforce: 'pre',

    transform(code: string, id: string) {
      if (!id.endsWith('.md')) return

      const codeBlocks: string[] = []
      const htmlBlocks: string[] = []

      // 1️⃣ 保护代码块 ``` ```
      let processed = code.replace(/```[\s\S]*?```/g, (match) => {
        const i = codeBlocks.length
        codeBlocks.push(match)
        return `__CODE_BLOCK_${i}__`
      })

      // 2️⃣ 保护 HTML 标签（避免破坏 Vue SFC）
      processed = processed.replace(/<\/?[a-zA-Z][^>]*>/g, (match) => {
        const i = htmlBlocks.length
        htmlBlocks.push(match)
        return `__HTML_BLOCK_${i}__`
      })

      // 3️⃣ 只转义"泛型形式"的 <T> 或 <K, V>
      processed = processed.replace(
          /<([A-Z][A-Za-z0-9_,\s]*)>/g,
          (_, inner) => `&lt;${inner}&gt;`
      )

      // 4️⃣ 恢复 HTML
      htmlBlocks.forEach((block, i) => {
        processed = processed.replace(`__HTML_BLOCK_${i}__`, block)
      })

      // 5️⃣ 恢复代码块
      codeBlocks.forEach((block, i) => {
        processed = processed.replace(`__CODE_BLOCK_${i}__`, block)
      })

      return processed
    }
  }
}

export default defineConfig({

  title: 'GFramework',
  description: '面向游戏开发场景的模块化 C# 框架',
  head: [
    ['link', { rel: 'icon', type: 'image/png', href: '/GFramework/favicon.png' }],
  ],
  /** GitHub Pages / 子路径部署 */
  base: '/GFramework/',
  /**
   * 为 GitHub Pages 产物生成稳定的绝对 URL。
   * llms-txt-action 依赖 sitemap.xml 发现站点页面，因此 hostname 需要与最终 Pages 地址对齐。
   */
  sitemap: {
    hostname: 'https://gewuyou.github.io/GFramework/'
  },
  vite: {
    plugins: [safeGenericEscapePlugin()],
    build: {
      // 提高代码块大小警告阈值（文档包含大量代码示例）
      chunkSizeWarningLimit: 1000
    }
  },
  themeConfig: {
    // 在顶层保留搜索配置，避免构建期只读取站点级配置时把搜索入口裁掉。
    search: localSearch
  },
  /** 多语言 */
  locales: {
    root: {
      label: '简体中文',
      lang: 'zh-CN',
      link: '/zh-CN/',

      themeConfig: {
        logo: '/logo-icon.png',
        search: localSearch,

        nav: [
          { text: '首页', link: '/zh-CN/' },
          { text: '入门指南', link: '/zh-CN/getting-started' },
          { text: 'Core', link: '/zh-CN/core/' },
          { text: 'Game', link: '/zh-CN/game/' },
          { text: 'Godot', link: '/zh-CN/godot/' },
          { text: '教程', link: '/zh-CN/tutorials/' },
          {
            text: '更多',
            items: [
              { text: 'ECS', link: '/zh-CN/ecs/' },
              { text: '抽象接口', link: '/zh-CN/abstractions/' },
              { text: '源码生成器', link: '/zh-CN/source-generators/' },
              { text: '最佳实践', link: '/zh-CN/best-practices/' },
              { text: 'API 参考', link: '/zh-CN/api-reference/' },
              { text: '常见问题', link: '/zh-CN/faq' },
              { text: '故障排查', link: '/zh-CN/troubleshooting' },
              { text: '贡献指南', link: '/zh-CN/contributing' },
              { text: '开发环境', link: '/zh-CN/contributor/development-environment' }
            ]
          }
        ],

        sidebar: {
          '/zh-CN/getting-started/': [
            {
              text: '入门指南',
              items: [
                { text: '架构概览', link: '/zh-CN/getting-started' },
                { text: '安装配置', link: '/zh-CN/getting-started/installation' },
                { text: '快速开始', link: '/zh-CN/getting-started/quick-start' },
              ]
            }
          ],

          '/zh-CN/core/': [
            {
              text: 'Core 核心框架',
              items: [
                { text: '概览', link: '/zh-CN/core/' },
                { text: '架构组件', link: '/zh-CN/core/architecture' },
                { text: 'Context 上下文', link: '/zh-CN/core/context' },
                { text: '异步初始化', link: '/zh-CN/core/async-initialization' },
                { text: '生命周期', link: '/zh-CN/core/lifecycle' },
                { text: '命令系统', link: '/zh-CN/core/command' },
                { text: '查询系统', link: '/zh-CN/core/query' },
                { text: 'CQRS 模式', link: '/zh-CN/core/cqrs' },
                { text: '事件系统', link: '/zh-CN/core/events' },
                { text: '属性系统', link: '/zh-CN/core/property' },
                { text: '状态管理', link: '/zh-CN/core/state-management' },
                { text: 'IoC容器', link: '/zh-CN/core/ioc' },
                { text: '协程系统', link: '/zh-CN/core/coroutine' },
                { text: '状态机', link: '/zh-CN/core/state-machine' },
                { text: '暂停系统', link: '/zh-CN/core/pause' },
                { text: '对象池', link: '/zh-CN/core/pool' },
                { text: '资源管理', link: '/zh-CN/core/resource' },
                { text: '配置管理', link: '/zh-CN/core/configuration' },
                { text: '日志系统', link: '/zh-CN/core/logging' },
                { text: '函数式编程', link: '/zh-CN/core/functional' },
                { text: '扩展方法', link: '/zh-CN/core/extensions' },
                { text: '工具类', link: '/zh-CN/core/utility' },
                { text: '模型层', link: '/zh-CN/core/model' },
                { text: '系统层', link: '/zh-CN/core/system' },
                { text: '规则系统', link: '/zh-CN/core/rule' },
                { text: '环境接口', link: '/zh-CN/core/environment' },
                { text: '本地化', link: '/zh-CN/core/localization' }
              ]
            }
          ],

          '/zh-CN/contributing': [
            {
              text: '贡献',
              items: [
                { text: '贡献指南', link: '/zh-CN/contributing' },
                { text: '开发环境', link: '/zh-CN/contributor/development-environment' }
              ]
            }
          ],

          '/zh-CN/contributor/': [
            {
              text: '贡献',
              items: [
                { text: '贡献指南', link: '/zh-CN/contributing' },
                { text: '开发环境', link: '/zh-CN/contributor/development-environment' }
              ]
            }
          ],

          '/zh-CN/ecs/': [
            {
              text: 'ECS 系统集成',
              items: [
                { text: 'ECS 概述', link: '/zh-CN/ecs/' },
                { text: 'Arch ECS 集成', link: '/zh-CN/ecs/arch' }
              ]
            }
          ],

          '/zh-CN/game/': [
            {
              text: 'Game 游戏模块',
              items: [
                { text: '概览', link: '/zh-CN/game/' },
                { text: '内容配置系统', link: '/zh-CN/game/config-system' },
                { text: 'VS Code 配置工具', link: '/zh-CN/game/config-tool' },
                { text: '数据管理', link: '/zh-CN/game/data' },
                { text: '场景系统', link: '/zh-CN/game/scene' },
                { text: 'UI 系统', link: '/zh-CN/game/ui' },
                { text: '存储系统', link: '/zh-CN/game/storage' },
                { text: '序列化', link: '/zh-CN/game/serialization' },
                { text: '游戏设置', link: '/zh-CN/game/setting' }
              ]
            }
          ],

          '/zh-CN/godot/': [
            {
              text: 'Godot 集成',
              items: [
                { text: '概览', link: '/zh-CN/godot/' },
                { text: '架构集成', link: '/zh-CN/godot/architecture' },
                { text: '场景系统', link: '/zh-CN/godot/scene' },
                { text: 'UI 系统', link: '/zh-CN/godot/ui' },
                { text: '资源仓储', link: '/zh-CN/godot/resource' },
                { text: '协程系统', link: '/zh-CN/godot/coroutine' },
                { text: '节点扩展', link: '/zh-CN/godot/extensions' },
                { text: '信号系统', link: '/zh-CN/godot/signal' },
                { text: '存储系统', link: '/zh-CN/godot/storage' },
                { text: '暂停系统', link: '/zh-CN/godot/pause' },
                { text: '对象池', link: '/zh-CN/godot/pool' },
                { text: '日志系统', link: '/zh-CN/godot/logging' },
                { text: '设置系统', link: '/zh-CN/godot/setting' }
              ]
            }
          ],

          '/zh-CN/source-generators/': [
            {
              text: '源码生成器',
              items: [
                { text: '概览', link: '/zh-CN/source-generators/' },
                { text: 'Schema 配置生成器', link: '/zh-CN/source-generators/schema-config-generator' },
                { text: '日志生成器', link: '/zh-CN/source-generators/logging-generator' },
                { text: '枚举扩展生成器', link: '/zh-CN/source-generators/enum-generator' },
                { text: 'ContextAware 生成器', link: '/zh-CN/source-generators/context-aware-generator' },
                { text: 'Priority 生成器', link: '/zh-CN/source-generators/priority-generator' },
                { text: 'Context Get 注入生成器', link: '/zh-CN/source-generators/context-get-generator' },
                { text: '模块自动注册生成器', link: '/zh-CN/source-generators/auto-register-module-generator' },
                { text: 'CQRS Handler Registry 生成器', link: '/zh-CN/source-generators/cqrs-handler-registry-generator' },
                { text: 'Godot 项目元数据生成器', link: '/zh-CN/source-generators/godot-project-generator' },
                { text: 'GetNode 生成器 (Godot)', link: '/zh-CN/source-generators/get-node-generator' },
                { text: 'BindNodeSignal 生成器 (Godot)', link: '/zh-CN/source-generators/bind-node-signal-generator' },
                { text: 'AutoUiPage 生成器', link: '/zh-CN/source-generators/auto-ui-page-generator' },
                { text: 'AutoScene 生成器', link: '/zh-CN/source-generators/auto-scene-generator' },
                { text: 'AutoRegisterExportedCollections 生成器', link: '/zh-CN/source-generators/auto-register-exported-collections-generator' }
              ]
            }
          ],

          '/zh-CN/abstractions/': [
            {
              text: '抽象接口',
              items: [
                { text: '概览', link: '/zh-CN/abstractions/' },
                { text: 'Core 抽象层说明', link: '/zh-CN/abstractions/core-abstractions' },
                { text: 'Game 抽象层说明', link: '/zh-CN/abstractions/game-abstractions' },
                { text: 'Ecs.Arch 抽象层说明', link: '/zh-CN/abstractions/ecs-arch-abstractions' }
              ]
            }
          ],

          '/zh-CN/tutorials/': [
            {
              text: '教程',
              items: [
                { text: '教程概览', link: '/zh-CN/tutorials/' },
                {
                  text: '基础教程',
                  link: '/zh-CN/tutorials/basic/',
                  collapsed: false,
                  items: [
                    { text: '教程概览', link: '/zh-CN/tutorials/basic/' },
                    { text: '1. 环境准备', link: '/zh-CN/tutorials/basic/01-environment' },
                    { text: '2. 项目创建与初始化', link: '/zh-CN/tutorials/basic/02-project-setup' },
                    { text: '3. 基础计数器实现', link: '/zh-CN/tutorials/basic/03-counter-basic' },
                    { text: '4. 引入 Model 重构', link: '/zh-CN/tutorials/basic/04-model-refactor' },
                    { text: '5. 命令系统优化', link: '/zh-CN/tutorials/basic/05-command-system' },
                    { text: '6. Utility 与 System', link: '/zh-CN/tutorials/basic/06-utility-system' },
                    { text: '7. 总结与最佳实践', link: '/zh-CN/tutorials/basic/07-summary' }
                  ]
                },
                { text: '使用协程系统', link: '/zh-CN/tutorials/coroutine-tutorial' },
                { text: '实现状态机', link: '/zh-CN/tutorials/state-machine-tutorial' },
                { text: '暂停系统实践', link: '/zh-CN/tutorials/pause-system' },
                { text: '函数式编程实践', link: '/zh-CN/tutorials/functional-programming' },
                { text: '资源管理最佳实践', link: '/zh-CN/tutorials/resource-management' },
                { text: '实现存档系统', link: '/zh-CN/tutorials/save-system' },
                { text: '数据迁移', link: '/zh-CN/tutorials/data-migration' },
                { text: 'Godot 集成', link: '/zh-CN/tutorials/godot-integration' },
                { text: 'Godot 完整项目', link: '/zh-CN/tutorials/godot-complete-project' },
                { text: '高级模式', link: '/zh-CN/tutorials/advanced-patterns' },
                { text: '大型项目组织', link: '/zh-CN/tutorials/large-project-organization' },
                { text: '单元测试', link: '/zh-CN/tutorials/unit-testing' }
              ]
            }
          ],

          '/zh-CN/best-practices/': [
            {
              text: '最佳实践',
              items: [
                { text: '概览', link: '/zh-CN/best-practices/' },
                { text: '架构模式', link: '/zh-CN/best-practices/architecture-patterns' },
                { text: '错误处理', link: '/zh-CN/best-practices/error-handling' },
                { text: '性能优化', link: '/zh-CN/best-practices/performance' },
                { text: '移动端优化', link: '/zh-CN/best-practices/mobile-optimization' },
                { text: '多人游戏', link: '/zh-CN/best-practices/multiplayer' }
              ]
            }
          ],
        },

        socialLinks: [
          { icon: 'github', link: 'https://github.com/GeWuYou/GFramework' }
        ],

        footer: {
          message: '基于 Apache 2.0 许可证发布',
          copyright: 'Copyright © 2026 GeWuYou'
        },

        outlineTitle: '页面导航',
        lastUpdatedText: '最后更新于',
        darkModeSwitchLabel: '主题',
        sidebarMenuLabel: '菜单',
        returnToTopLabel: '回到顶部',

        docFooter: {
          prev: '上一页',
          next: '下一页'
        }
      }
    }
  }
})
