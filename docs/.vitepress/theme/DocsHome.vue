<!-- Copyright (c) 2025-2026 GeWuYou -->
<!-- SPDX-License-Identifier: Apache-2.0 -->

<script setup lang="ts">
import { computed } from 'vue'
import { useData, withBase } from 'vitepress'

const { lang } = useData()
const zh = computed(() => lang.value === 'zh-CN')
const copy = computed(() => zh.value ? {
  heading: '用 C# 构建游戏',
  description: '按项目需要组合生命周期、消息处理、场景与 UI、存档以及 Godot 集成。',
  start: '快速开始',
  godot: '接入 Godot',
  tasks: '按任务浏览',
  sections: [
    ['选择 NuGet 包', '按 C# 或 Godot 项目选取安装组合。', '/getting-started/installation.html'],
    ['加载类型化配置', '用 YAML、JSON Schema 和生成的 C# 类型组织游戏内容。', '/game/config-system.html'],
    ['打开页面与对话框', '注册视图、传递参数并获取对话框结果。', '/godot/ui.html'],
    ['保存游戏进度', '定义存档数据、选择槽位并恢复进度。', '/tutorials/save-system.html'],
    ['分发请求与通知', '注册 CQRS 处理器并组合管道行为。', '/core/cqrs.html'],
    ['浏览可运行示例', '从最小 C# 应用到完整 Godot 游戏。', '/tutorials/']
  ]
} : {
  heading: 'Build games with C#',
  description: 'Add lifecycle management, messaging, scenes and UI, save data, and Godot integration as your project needs them.',
  start: 'Quickstart',
  godot: 'Integrate Godot',
  tasks: 'Browse by task',
  sections: [
    ['Choose NuGet packages', 'Find the installation for your C# or Godot project.', '/getting-started/installation.html'],
    ['Load typed configuration', 'Organize game content with YAML, JSON Schema, and generated C# types.', '/game/config-system.html'],
    ['Open pages and dialogs', 'Register views, pass arguments, and receive dialog results.', '/godot/ui.html'],
    ['Save game progress', 'Define save data, choose a slot, and resume a run.', '/tutorials/save-system.html'],
    ['Dispatch requests and notifications', 'Register CQRS handlers and compose pipeline behaviors.', '/core/cqrs.html'],
    ['Browse runnable examples', 'Go from a minimal C# application to a complete Godot game.', '/tutorials/']
  ]
})

function href(path: string) {
  return withBase((zh.value ? '/zh-CN' : '') + path)
}
</script>

<template>
  <main class="docs-home" aria-labelledby="home-heading">
    <div class="docs-hero">
      <div class="docs-hero-intro">
        <p class="docs-wordmark">GFramework</p>
        <h1 id="home-heading">{{ copy.heading }}</h1>
        <p class="docs-lede">{{ copy.description }}</p>
        <div class="docs-actions">
          <a class="docs-primary" :href="href('/getting-started/quick-start.html')">{{ copy.start }}</a>
          <a class="docs-secondary" :href="href('/tutorials/godot-integration.html')">{{ copy.godot }}</a>
        </div>
      </div>
      <div class="docs-example vp-doc">
        <div class="docs-example-title">MinimalApp</div>
        <slot />
      </div>
    </div>
    <section class="docs-tasks" aria-labelledby="task-heading">
      <h2 id="task-heading">{{ copy.tasks }}</h2>
      <ul class="docs-task-list">
        <li v-for="[title, description, path] in copy.sections" :key="path">
          <a :href="href(path)">
            <h3>{{ title }}</h3>
            <p>{{ description }}</p>
          </a>
        </li>
      </ul>
    </section>
  </main>
</template>
