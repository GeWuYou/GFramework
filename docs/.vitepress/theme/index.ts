// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

import { h } from 'vue'
import type { Theme } from 'vitepress'
import DefaultTheme from 'vitepress/theme'
import LocaleSwitch from './LocaleSwitch.vue'
import NotFound from './NotFound.vue'
import './style.css'

export default {
  extends: DefaultTheme,
  Layout: () => h(DefaultTheme.Layout, null, {
    'nav-bar-content-after': () => h(LocaleSwitch),
    'nav-screen-content-after': () => h(LocaleSwitch, { mobile: true }),
    'not-found': () => h(NotFound)
  })
} satisfies Theme
