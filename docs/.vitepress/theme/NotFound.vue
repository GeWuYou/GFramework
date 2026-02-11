<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useData, useRouter, withBase } from 'vitepress'

const { lang } = useData()
const router = useRouter()

const isZh = computed(() => lang.value === 'zh-CN')

const countdown = ref(5)
let timer: number | null = null

const goHome = () => {
  if (timer) clearInterval(timer)
  router.go(withBase(isZh.value ? '/zh-CN/' : '/en/'))
}

onMounted(() => {
  timer = window.setInterval(() => {
    countdown.value--
    if (countdown.value <= 0) {
      goHome()
    }
  }, 1000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<template>
  <div class="not-found-container">
    <h1 class="error-code">404</h1>

    <h2 class="error-title">
      {{ isZh ? '页面未找到' : 'Page Not Found' }}
    </h2>

    <p class="error-message">
      <template v-if="isZh">
        您访问的页面不存在。
      </template>
      <template v-else>
        The page you are looking for does not exist.
      </template>
    </p>

    <p class="countdown-text">
      <template v-if="isZh">
        <span class="countdown-number">{{ countdown }}</span> 秒后自动跳转
      </template>
      <template v-else>
        Auto redirecting in <span class="countdown-number">{{ countdown }}</span> seconds
      </template>
    </p>

    <div class="suggestions">
      <h3>📚 {{ isZh ? '您可能想访问：' : 'You might be looking for:' }}</h3>
      <ul v-if="isZh">
        <li>→ <a :href="withBase('/zh-CN/getting-started/installation')">安装配置</a></li>
        <li>→ <a :href="withBase('/zh-CN/getting-started/quick-start')">快速开始</a></li>
        <li>→ <a :href="withBase('/zh-CN/core/overview')">Core 核心框架</a></li>
        <li>→ <a :href="withBase('/zh-CN/tutorials/basic-tutorial')">基础教程</a></li>
      </ul>
      <ul v-else>
        <li>→ <a :href="withBase('/en/getting-started/installation')">Installation</a></li>
        <li>→ <a :href="withBase('/en/getting-started/quick-start')">Quick Start</a></li>
        <li>→ <a :href="withBase('/en/core/overview')">Core Overview</a></li>
        <li>→ <a :href="withBase('/en/tutorials/basic-tutorial')">Basic Tutorial</a></li>
      </ul>
    </div>

    <div class="action-buttons">
      <button class="btn btn-primary" @click="goHome">
        {{ isZh ? '返回首页' : 'Go to Homepage' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.not-found-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  text-align: center;
  padding: 20px;
}

.error-code {
  font-size: 120px;
  font-weight: bold;
  background: linear-gradient(120deg, #3451b2 0%, #6366f1 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin: 0;
  line-height: 1;
}

.error-title {
  font-size: 32px;
  margin: 20px 0 10px;
  color: var(--vp-c-text-1);
}

.error-message {
  font-size: 18px;
  color: var(--vp-c-text-2);
  margin-bottom: 10px;
}

.countdown-text {
  font-size: 16px;
  color: var(--vp-c-text-3);
  margin: 20px 0;
}

.countdown-number {
  color: #3451b2;
  font-weight: bold;
  font-size: 20px;
}

.action-buttons {
  display: flex;
  gap: 16px;
  margin-top: 30px;
}

.btn {
  padding: 12px 24px;
  border-radius: 8px;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.3s;
  cursor: pointer;
  border: none;
  font-size: 16px;
}

.btn-primary {
  background: #3451b2;
  color: white;
}

.btn-primary:hover {
  background: #2841a0;
  transform: translateY(-2px);
}

.suggestions {
  margin-top: 24px;
}

.suggestions ul {
  list-style: none;
  padding: 0;
}

.suggestions li {
  margin: 6px 0;
}
</style>
