<!-- Copyright (c) 2025-2026 GeWuYou -->
<!-- SPDX-License-Identifier: Apache-2.0 -->

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useData, withBase } from 'vitepress'

defineProps<{ mobile?: boolean }>()

const { lang, page } = useData()
const fragment = ref('')
const chinese = computed(() => lang.value === 'zh-CN')
const targetLang = computed(() => chinese.value ? 'en' : 'zh-CN')
const href = computed(() => {
  const source = page.value.isNotFound ? 'index.md' : page.value.relativePath
  const route = source
    .replace(/^zh-CN\//, '')
    .replace(/(^|\/)index\.md$/, '$1')
    .replace(/\.md$/, '.html')
  const prefix = chinese.value ? '/' : '/zh-CN/'
  return withBase(prefix + route) + (page.value.isNotFound ? '' : fragment.value)
})

function syncFragment() {
  fragment.value = window.location.hash
}

onMounted(() => {
  syncFragment()
  window.addEventListener('hashchange', syncFragment)
})
onUnmounted(() => window.removeEventListener('hashchange', syncFragment))
watch(() => page.value.relativePath, syncFragment, { flush: 'post' })
</script>

<template>
  <a
    class="docs-locale"
    :class="{ 'docs-locale-mobile': mobile }"
    :href="href"
    :hreflang="targetLang"
    :lang="targetLang"
  >{{ chinese ? 'English' : '简体中文' }}</a>
</template>
