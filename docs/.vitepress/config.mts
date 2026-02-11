import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "GFramework",
  description: "面向游戏开发场景的模块化 C# 框架",
  base: "/GFramework/",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: '首页', link: '/' },
      { text: '入门指南', link: '/getting-started/installation' },
      { text: 'Core', link: '/core/overview' },
      { text: 'Game', link: '/game/overview' },
      { text: 'Godot', link: '/godot/overview' },
      { text: '源码生成器', link: '/source-generators/overview' },
      { text: '教程', link: '/tutorials/basic-tutorial' },
      { text: 'API参考', link: '/api-reference/core-api' }
    ],

    sidebar: {
      '/getting-started/': [
        {
          text: '入门指南',
          items: [
            { text: '安装配置', link: '/getting-started/installation' },
            { text: '快速开始', link: '/getting-started/quick-start' },
            { text: '架构概览', link: '/getting-started/architecture-overview' }
          ]
        }
      ],
      '/core/': [
        {
          text: 'Core 核心框架',
          items: [
            { text: '概览', link: '/core/overview' },
            { text: '架构组件', link: '/core/architecture/architecture' },
            { text: '命令查询系统', link: '/core/command-query/commands' },
            { text: '事件系统', link: '/core/events/event-bus' },
            { text: '属性系统', link: '/core/property/bindable-property' },
            { text: '工具类', link: '/core/utilities/ioc-container' }
          ]
        }
      ],
      '/game/': [
        {
          text: 'Game 游戏模块',
          items: [
            { text: '概览', link: '/game/overview' },
            { text: '模块系统', link: '/game/modules/architecture-modules' },
            { text: '存储系统', link: '/game/storage/scoped-storage' },
            { text: '资源管理', link: '/game/assets/asset-catalog' },
            { text: '序列化', link: '/game/serialization/json-serializer' }
          ]
        }
      ],
      '/godot/': [
        {
          text: 'Godot 集成',
          items: [
            { text: '概览', link: '/godot/overview' },
            { text: '集成指南', link: '/godot/integration/architecture-integration' },
            { text: '节点扩展', link: '/godot/node-extensions/node-extensions' },
            { text: '对象池', link: '/godot/pooling/node-pool' },
            { text: '日志系统', link: '/godot/logging/godot-logger' }
          ]
        }
      ],
      '/source-generators/': [
        {
          text: '源码生成器',
          items: [
            { text: '概览', link: '/source-generators/overview' },
            { text: '日志生成器', link: '/source-generators/logging-generator' },
            { text: '枚举扩展', link: '/source-generators/enum-extensions' },
            { text: '规则生成器', link: '/source-generators/rule-generator' }
          ]
        }
      ],
      '/tutorials/': [
        {
          text: '教程',
          items: [
            { text: '基础教程', link: '/tutorials/basic-tutorial' },
            { text: '高级模式', link: '/tutorials/advanced-patterns' },
            { text: '最佳实践', link: '/tutorials/best-practices' }
          ]
        }
      ],
      '/api-reference/': [
        {
          text: 'API 参考',
          items: [
            { text: 'Core API', link: '/api-reference/core-api' },
            { text: 'Game API', link: '/api-reference/game-api' },
            { text: 'Godot API', link: '/api-reference/godot-api' },
            { text: '生成器 API', link: '/api-reference/generators-api' }
          ]
        }
      ]
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/GeWuYou/GFramework' }
    ],

    footer: {
      message: '基于 Apache 2.0 许可证发布',
      copyright: 'Copyright © 2026 GeWuYou'
    }
  }
})
