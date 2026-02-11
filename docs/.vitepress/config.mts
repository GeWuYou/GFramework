import { defineConfig } from 'vitepress'

export default defineConfig({
  title: "GFramework",
  description: "面向游戏开发场景的模块化 C# 框架",
  base: "/GFramework/",
  vite: {
    plugins: [
      {
        name: 'catch-all-404',
        configureServer(server) {
          return () => {
            server.middlewares.use((req, res, next) => {
              const url = req.url || ''

              // 排除这些路径
              const excludePaths = [
                '/@vite',
                '/@fs',
                '/node_modules',
                '/__vite',
                '/public'
              ]

              // 如果是资源文件或特殊路径，跳过
              if (excludePaths.some(path => url.startsWith(path)) ||
                  url.includes('.') ||
                  url.startsWith('/GFramework/')) {
                return next()
              }

              // 其他路径重定向到 404
              res.writeHead(302, { Location: '/GFramework/404' })
              res.end()
            })
          }
        }
      }
    ],
    server: {
      strictPort: false,
      // 添加中间件处理不带斜杠的情况
      proxy: {}
    }
  },
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
          { text: '入门指南', link: '/zh-CN/getting-started/installation' },
          { text: 'Core', link: '/zh-CN/core/overview' },
          { text: 'Game', link: '/zh-CN/game/overview' },
          { text: 'Godot', link: '/zh-CN/godot/overview' },
          { text: '源码生成器', link: '/zh-CN/source-generators/overview' },
          { text: '教程', link: '/zh-CN/tutorials/basic-tutorial' },
          { text: 'API参考', link: '/zh-CN/api-reference/core-api' }
        ],

        sidebar: {
          '/zh-CN/getting-started/': [
            {
              text: '入门指南',
              items: [
                { text: '安装配置', link: '/zh-CN/getting-started/installation' },
                { text: '快速开始', link: '/zh-CN/getting-started/quick-start' },
                { text: '架构概览', link: '/zh-CN/getting-started/architecture-overview' }
              ]
            }
          ],
          '/zh-CN/core/': [
            {
              text: 'Core 核心框架',
              items: [
                { text: '概览', link: '/zh-CN/core/overview' },
                { text: '架构组件', link: '/zh-CN/core/architecture/architecture' },
                { text: '命令查询系统', link: '/zh-CN/core/command-query/commands' },
                { text: '事件系统', link: '/zh-CN/core/events/event-bus' },
                { text: '属性系统', link: '/zh-CN/core/property/bindable-property' },
                { text: '工具类', link: '/zh-CN/core/utilities/ioc-container' }
              ]
            }
          ],
          '/zh-CN/game/': [
            {
              text: 'Game 游戏模块',
              items: [
                { text: '概览', link: '/zh-CN/game/overview' },
                { text: '模块系统', link: '/zh-CN/game/modules/architecture-modules' },
                { text: '存储系统', link: '/zh-CN/game/storage/scoped-storage' },
                { text: '资源管理', link: '/zh-CN/game/assets/asset-catalog' },
                { text: '序列化', link: '/zh-CN/game/serialization/json-serializer' }
              ]
            }
          ],
          '/zh-CN/godot/': [
            {
              text: 'Godot 集成',
              items: [
                { text: '概览', link: '/zh-CN/godot/overview' },
                { text: '集成指南', link: '/zh-CN/godot/integration/architecture-integration' },
                { text: '节点扩展', link: '/zh-CN/godot/node-extensions/node-extensions' },
                { text: '对象池', link: '/zh-CN/godot/pooling/node-pool' },
                { text: '日志系统', link: '/zh-CN/godot/logging/godot-logger' }
              ]
            }
          ],
          '/zh-CN/source-generators/': [
            {
              text: '源码生成器',
              items: [
                { text: '概览', link: '/zh-CN/source-generators/overview' },
                { text: '日志生成器', link: '/zh-CN/source-generators/logging-generator' },
                { text: '枚举扩展', link: '/zh-CN/source-generators/enum-extensions' },
                { text: '规则生成器', link: '/zh-CN/source-generators/rule-generator' }
              ]
            }
          ],
          '/zh-CN/tutorials/': [
            {
              text: '教程',
              items: [
                { text: '基础教程', link: '/zh-CN/tutorials/basic-tutorial' },
                { text: '高级模式', link: '/zh-CN/tutorials/advanced-patterns' },
                { text: '最佳实践', link: '/zh-CN/tutorials/best-practices' }
              ]
            }
          ],
          '/zh-CN/api-reference/': [
            {
              text: 'API 参考',
              items: [
                { text: 'Core API', link: '/zh-CN/api-reference/core-api' },
                { text: 'Game API', link: '/zh-CN/api-reference/game-api' },
                { text: 'Godot API', link: '/zh-CN/api-reference/godot-api' },
                { text: '生成器 API', link: '/zh-CN/api-reference/generators-api' }
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
    },

    // 未来添加英文版本时取消注释
    /*
    en: {
      label: 'English',
      lang: 'en-US',
      link: '/en/',
      themeConfig: {
        logo: '/logo-icon.png',
        
        search: {
          provider: 'local'
        },
        
        nav: [
          { text: 'Home', link: '/en/' },
          { text: 'Getting Started', link: '/en/getting-started/installation' },
          { text: 'Core', link: '/en/core/overview' },
          { text: 'Game', link: '/en/game/overview' },
          { text: 'Godot', link: '/en/godot/overview' },
          { text: 'Source Generators', link: '/en/source-generators/overview' },
          { text: 'Tutorials', link: '/en/tutorials/basic-tutorial' },
          { text: 'API Reference', link: '/en/api-reference/core-api' }
        ],
        
        sidebar: {
          '/en/getting-started/': [
            {
              text: 'Getting Started',
              items: [
                { text: 'Installation', link: '/en/getting-started/installation' },
                { text: 'Quick Start', link: '/en/getting-started/quick-start' },
                { text: 'Architecture Overview', link: '/en/getting-started/architecture-overview' }
              ]
            }
          ],
          // ... 其他英文侧边栏配置
        },
        
        socialLinks: [
          { icon: 'github', link: 'https://github.com/GeWuYou/GFramework' }
        ],
        
        footer: {
          message: 'Released under the Apache 2.0 License',
          copyright: 'Copyright © 2026 GeWuYou'
        }
      }
    }
    */
  }
})