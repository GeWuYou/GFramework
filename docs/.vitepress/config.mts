// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

import { existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { defineConfig, type DefaultTheme } from 'vitepress'
import { navigation } from './navigation.mts'

const base = '/GFramework/'
const site = 'https://gewuyou.github.io'
const zhPrefix = '/zh-CN'

const search: DefaultTheme.Config['search'] = {
  provider: 'local',
  options: {
    locales: {
      'zh-CN': {
        translations: {
          button: { buttonText: '搜索', buttonAriaLabel: '搜索文档' },
          modal: {
            displayDetails: '显示详细结果',
            resetButtonTitle: '清除搜索',
            backButtonTitle: '关闭搜索',
            noResultsText: '未找到结果',
            footer: {
              selectText: '选择',
              navigateText: '移动',
              closeText: '关闭'
            }
          }
        }
      }
    }
  }
}

function publishedUrl(relativePath: string) {
  const route = relativePath.replace(/(^|\/)index\.md$/, '$1').replace(/\.md$/, '.html')
  return new URL(base + route, site).href
}

export default defineConfig({
  title: 'GFramework',
  base,
  sitemap: { hostname: site + base },
  head: [['link', { rel: 'icon', type: 'image/png', href: base + 'favicon.png' }]],
  themeConfig: {
    logo: '/logo-icon.png',
    search,
    socialLinks: [{ icon: 'github', link: 'https://github.com/GeWuYou/GFramework' }]
  },
  locales: {
    root: {
      lang: 'en',
      description: 'A modular C# framework for games and Godot projects.',
      themeConfig: {
        ...navigation('en', ''),
        footer: { message: 'Apache License 2.0', copyright: '© GeWuYou' }
      }
    },
    'zh-CN': {
      lang: 'zh-CN',
      description: '面向 C# 游戏开发与 Godot 项目的模块化框架。',
      themeConfig: {
        ...navigation('zh', zhPrefix),
        sidebarMenuLabel: '目录',
        outlineTitle: '本页目录',
        returnToTopLabel: '返回顶部',
        darkModeSwitchLabel: '外观',
        docFooter: { prev: '上一篇', next: '下一篇' },
        footer: { message: 'Apache 2.0 许可证', copyright: '© GeWuYou' }
      }
    }
  },
  transformPageData(page) {
    if (page.relativePath === '404.md') return
    const source = page.relativePath.replace(/^zh-CN\//, '')
    const translated = 'zh-CN/' + source
    const docsRoot = new URL('../', import.meta.url)
    const hasBothLocales = [source, translated].every(
      path => existsSync(fileURLToPath(new URL(path, docsRoot)))
    )
    if (!hasBothLocales) return
    page.frontmatter.head = [
      ...(page.frontmatter.head ?? []),
      ['link', { rel: 'canonical', href: publishedUrl(page.relativePath) }],
      ['link', { rel: 'alternate', hreflang: 'en', href: publishedUrl(source) }],
      ['link', { rel: 'alternate', hreflang: 'zh-CN', href: publishedUrl(translated) }]
    ]
  }
})
