<!-- Copyright (c) 2025-2026 GeWuYou -->
<!-- SPDX-License-Identifier: Apache-2.0 -->

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { withBase } from 'vitepress'

const zh = ref(false)
onMounted(() => {
  zh.value = location.pathname.startsWith(withBase('/zh-CN/'))
})

const home = computed(() => withBase(zh.value ? '/zh-CN/' : '/'))
const guide = computed(() => withBase((zh.value ? '/zh-CN' : '') + '/getting-started/'))
</script>

<template>
  <main class="docs-not-found" :lang="zh ? 'zh-CN' : 'en'">
    <p class="docs-wordmark">404</p>
    <h1>{{ zh ? '页面未找到' : 'Page not found' }}</h1>
    <p>{{ zh ? '检查链接地址，或从入门指南继续浏览。' : 'Check the address or start with the getting started guide.' }}</p>
    <div class="docs-actions">
      <a class="docs-primary" :href="home">{{ zh ? '文档首页' : 'Documentation home' }}</a>
      <a class="docs-secondary" :href="guide">{{ zh ? '入门指南' : 'Getting started' }}</a>
    </div>
  </main>
</template>
