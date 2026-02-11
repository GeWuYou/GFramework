---
layout: page
---

<script setup>
import { onMounted } from 'vue'

onMounted(() => {
  if (typeof window !== 'undefined') {
    // 检测浏览器语言
    const browserLang = navigator.language.toLowerCase()
    
    // 目前只有中文，未来可以根据语言跳转
    if (browserLang.startsWith('zh-CN')) {
      window.location.href = '/GFramework/zh-CN/'
    } else if (browserLang.startsWith('en')) {
      // 未来如果有英文版
      // window.location.href = '/GFramework/en/'
      window.location.href = '/GFramework/zh-CN/' // 暂时跳转到中文
    } else {
      // 默认跳转到中文
      window.location.href = '/GFramework/zh-CN/'
    }
  }
})
</script>

<div style="text-align: center; padding: 100px 20px;">
  <h1>🌐 Language Selection / 语言选择</h1>
  <div style="margin-top: 40px; display: flex; gap: 20px; justify-content: center; flex-wrap: wrap;">
    <a href="/GFramework/zh-CN/" style="padding: 12px 24px; background: #3451b2; color: white; border-radius: 8px; text-decoration: none; font-size: 16px;">
      简体中文 🇨🇳
    </a>
    <!-- 未来添加英文版本时取消注释 -->
    <!-- <a href="/GFramework/en/" style="padding: 12px 24px; background: #3451b2; color: white; border-radius: 8px; text-decoration: none; font-size: 16px;">
      English 🇺🇸
    </a> -->
  </div>
  <p style="margin-top: 40px; color: #666;">
    Auto-redirecting... / 自动跳转中...
  </p>
</div>