import { defineConfig } from 'vitepress'

export default defineConfig({
  
  title: 'GFramework',
  description: '面向游戏开发场景的模块化 C# 框架',
  head: [
    ['link', { rel: 'icon', type: 'image/png', href: '/GFramework/favicon.png' }],
  ],
  /** GitHub Pages / 子路径部署 */
  base: '/GFramework/',
  vite: {
    plugins: [safeGenericEscapePlugin()]
  },
  /** 多语言 */
  locales: {
    root: {
      label: '简体中文',
      lang: 'zh-CN',
      link: '/zh-CN/',

      themeConfig: {
        logo: '/logo-icon.png',

        search: {
          provider: 'local',
          options: {
            translations: {
              button: {
                buttonText: '搜索文档',
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
        },

        nav: [
          { text: '首页', link: '/zh-CN/' },
          { text: '入门指南', link: '/zh-CN/getting-started' },
          { text: 'Core', link: '/zh-CN/core/' },
          { text: 'Game', link: '/zh-CN/game/' },
          { text: 'Godot', link: '/zh-CN/godot/' },
          { text: '源码生成器', link: '/zh-CN/source-generators' },
          { text: '教程', link: '/zh-CN/tutorials/' },
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
                { text: '命令系统', link: '/zh-CN/core/command' },
                { text: '查询系统', link: '/zh-CN/core/query' },
                { text: '事件系统', link: '/zh-CN/core/events' },
                { text: '属性系统', link: '/zh-CN/core/property' },
                { text: 'IoC容器', link: '/zh-CN/core/ioc' },
                { text: '对象池', link: '/zh-CN/core/pool' },
                { text: '日志系统', link: '/zh-CN/core/logging' },
                { text: '扩展方法', link: '/zh-CN/core/extensions' },
                { text: '工具类', link: '/zh-CN/core/utility' },
                { text: '模型层', link: '/zh-CN/core/model' },
                { text: '系统层', link: '/zh-CN/core/system' },
                { text: '规则系统', link: '/zh-CN/core/rule' },
                { text: '环境接口', link: '/zh-CN/core/environment' }
              ]
            }
          ],

          '/zh-CN/game/': [
            {
              text: 'Game 游戏模块',
              items: [
                { text: '概览', link: '/zh-CN/game/' },
                { text: '游戏设置', link: '/zh-CN/game/setting' }
              ]
            }
          ],

          '/zh-CN/godot/': [
            {
              text: 'Godot 集成',
              items: [
                { text: '概览', link: '/zh-CN/godot/' },
                { text: '协程系统', link: '/zh-CN/godot/coroutine' },
                { text: '节点扩展', link: '/zh-CN/godot/extensions' },
                { text: '信号系统', link: '/zh-CN/godot/signal' },
                { text: '存储系统', link: '/zh-CN/godot/storage' },
                { text: '设置系统', link: '/zh-CN/godot/setting' }
              ]
            }
          ],

          '/zh-CN/source-generators/': [
            {
              text: '源码生成器',
              items: [
                { text: '概览', link: '/zh-CN/source-generators/' },
                { text: '日志生成器', link: '/zh-CN/source-generators/logging-generator' },
                { text: '枚举扩展', link: '/zh-CN/source-generators/enum-generator' },
                { text: '规则生成器', link: '/zh-CN/source-generators/rule-generator' }
              ]
            }
          ],

          '/zh-CN/abstractions/': [
            {
              text: '抽象接口',
              items: [
                { text: 'Core Abstractions', link: '/zh-CN/abstractions/core-abstractions' },
                { text: 'Game Abstractions', link: '/zh-CN/abstractions/game-abstractions' }
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
                { text: 'Godot集成', link: '/zh-CN/tutorials/godot-integration' },
                { text: '高级模式', link: '/zh-CN/tutorials/advanced-patterns' }
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
import { defineConfig } from 'vitepress'

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

      // 3️⃣ 只转义“泛型形式”的 <T> 或 <K, V>
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
