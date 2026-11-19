// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

import type { DefaultTheme } from 'vitepress'

const groups = [
  ["入门", "Get started", [
    ["入门","Get started","getting-started/index"],
    ["快速开始","Quickstart","getting-started/quick-start"],
    ["安装与选包","Installation","getting-started/installation"]
  ]],
  ["Core", "Core", [
    ["Core","Core","core/index"],
    ["应用装配与生命周期","Application composition and lifecycle","core/architecture"],
    ["依赖注入","Dependency injection","core/ioc"],
    ["生命周期组件","Lifecycle components","core/lifecycle"],
    ["异步初始化","Asynchronous initialization","core/async-initialization"],
    ["声明服务与节点依赖","Declare service and node dependencies","core/context"],
    ["CQRS 请求、通知与流","CQRS requests, notifications, and streams","core/cqrs"],
    ["同步事件","Synchronous events","core/events"],
    ["可绑定属性","Bindable properties","core/property"],
    ["集中状态","Shared state","core/state-management"],
    ["异步状态机","Asynchronous state machines","core/state-machine"],
    ["协程调度","Coroutine scheduling","core/coroutine"],
    ["暂停请求","Pause requests","core/pause"],
    ["资源租约","Resource leases","core/resource"],
    ["对象池","Object pools","core/pool"],
    ["游戏文本本地化","Localize game text","core/localization"],
    ["应用日志","Application logging","core/logging"],
    ["应用配置","Application configuration","core/configuration"],
    ["宿主环境","Host environment","core/environment"],
    ["领域状态","Domain state","core/model"],
    ["应用服务","Application services","core/system"],
    ["业务规则","Domain rules","core/rule"],
    ["处理写入请求","Handle write requests","core/command"],
    ["处理读取请求","Handle read requests","core/query"],
    ["Option 与 Result","Option and Result","core/functional"],
    ["通用扩展方法","General extensions","core/extensions"],
    ["通用工具","Utilities","core/utility"]
  ]],
  ["Game", "Game", [
    ["Game","Game","game/index"],
    ["类型化游戏配置","Typed game configuration","game/config-system"],
    ["VS Code 配置工具","VS Code configuration tool","game/config-tool"],
    ["DLC 与资料型模组","DLC and data mods","game/content-packs"],
    ["代码模组与 Harmony","Code mods and Harmony","game/code-mods"],
    ["数据与版本化存档","Data and versioned saves","game/data"],
    ["文件存储","File storage","game/storage"],
    ["JSON 序列化","JSON serialization","game/serialization"],
    ["玩家设置","Player settings","game/setting"],
    ["场景路由","Scene routing","game/scene"],
    ["UI 路由与对话框","UI routes and dialogs","game/ui"],
    ["输入绑定与 UI 动作","Input bindings and UI actions","game/input"]
  ]],
  ["Godot", "Godot", [
    ["Godot 集成","Godot integration","godot/index"],
    ["Godot 宿主装配","Compose a Godot host","godot/architecture"],
    ["挂载 Godot UI","Mount Godot UI","godot/ui"],
    ["挂载 Godot 场景","Mount Godot scenes","godot/scene"],
    ["适配 Godot InputMap","Adapt Godot InputMap","godot/input"],
    ["连接 Godot 信号","Connect Godot signals","godot/signal"],
    ["暂停 Godot 场景树","Pause the Godot scene tree","godot/pause"],
    ["在 Godot 中运行协程","Run coroutines in Godot","godot/coroutine"],
    ["复用 Godot 节点","Reuse Godot nodes","godot/pool"],
    ["加载 Godot 资源","Load Godot resources","godot/resource"],
    ["Godot 读写路径","Godot storage paths","godot/storage"],
    ["应用 Godot 设置","Apply Godot settings","godot/setting"],
    ["输出到 Godot 控制台","Log to the Godot console","godot/logging"],
    ["Godot 扩展方法","Godot extensions","godot/extensions"],
    ["运行 Godot 示例","Run the Godot runtime sample","godot/runtime-sample"]
  ]],
  ["Arch ECS", "Arch ECS", [
    ["Arch ECS","Arch ECS","ecs/index"],
    ["运行 Arch ECS 系统","Run Arch ECS systems","ecs/arch"]
  ]],
  ["可运行示例", "Runnable examples", [
    ["示例","Examples","tutorials/index"],
    ["接入 Godot","Integrate Godot","tutorials/godot-integration"],
    ["制作 Godot 小游戏","Build a Godot game","tutorials/godot-complete-project"],
    ["组织进阶 Godot 项目","Structure an advanced Godot project","tutorials/godot-advanced-project"],
    ["实现存档","Implement saves","tutorials/save-system"],
    ["实现嵌套暂停菜单","Implement nested pause menus","tutorials/pause-system"],
    ["编排跨帧游戏流程","Compose frame-based gameplay","tutorials/coroutine-tutorial"],
    ["在场景间管理资源","Manage resources between scenes","tutorials/resource-management"],
    ["编排游戏状态切换","Coordinate game state transitions","tutorials/state-machine-tutorial"],
    ["迁移版本化存档","Migrate versioned saves","tutorials/data-migration"],
    ["测试游戏规则与用例","Test game rules and use cases","tutorials/unit-testing"],
    ["组合可失败的计算","Compose fallible computations","tutorials/functional-programming"],
    ["扩展已有游戏","Extend an existing game","tutorials/advanced-patterns"],
    ["按功能组织游戏项目","Organize a game project by feature","tutorials/large-project-organization"]
  ]],
  ["第一个 C# 应用", "First C# application", [
    ["运行第一个 C# 应用","Run your first C# application","tutorials/basic/index"],
    ["准备示例环境","Prepare the example environment","tutorials/basic/01-environment"],
    ["查看启动入口","Read the application entry point","tutorials/basic/02-project-setup"],
    ["记录订单状态","Record order state","tutorials/basic/03-counter-basic"],
    ["定义领域规则","Define domain rules","tutorials/basic/04-model-refactor"],
    ["执行业务用例","Run an application use case","tutorials/basic/05-command-system"],
    ["装配依赖","Compose dependencies","tutorials/basic/06-utility-system"],
    ["继续学习","Continue learning","tutorials/basic/07-summary"]
  ]],
  ["源码生成器", "Source generators", [
    ["源码生成器","Source generators","source-generators/index"],
    ["生成枚举扩展","Generate enum extensions","source-generators/enum-generator"],
    ["生成优先级属性","Generate a priority property","source-generators/priority-generator"],
    ["配置生成参考","Configuration generator reference","source-generators/schema-config-generator"],
    ["生成 CQRS 处理器注册表","Generate a CQRS handler registry","source-generators/cqrs-handler-registry-generator"],
    ["生成 Godot 项目元数据","Generate Godot project metadata","source-generators/godot-project-generator"],
    ["生成节点查找","Generate node lookups","source-generators/get-node-generator"],
    ["生成节点信号绑定","Generate node signal bindings","source-generators/bind-node-signal-generator"],
    ["生成场景包装","Generate a scene wrapper","source-generators/auto-scene-generator"],
    ["生成集合注册","Generate collection registration","source-generators/auto-register-exported-collections-generator"]
  ]],
  ["抽象接口", "Abstractions", [
    ["抽象接口","Abstractions","abstractions/index"],
    ["Core 接口","Core contracts","abstractions/core-abstractions"],
    ["Game 接口","Game contracts","abstractions/game-abstractions"],
    ["Arch ECS 接口","Arch ECS contracts","abstractions/ecs-arch-abstractions"]
  ]],
  ["项目实践", "Project practices", [
    ["项目设计实践","Project practices","best-practices/index"],
    ["组织项目依赖","Organize project dependencies","best-practices/architecture-patterns"],
    ["处理失败与取消","Handle failures and cancellation","best-practices/error-handling"],
    ["测量游戏性能","Measure game performance","best-practices/performance"],
    ["验证移动端表现","Verify mobile behavior","best-practices/mobile-optimization"],
    ["接入游戏网络同步","Integrate network synchronization","best-practices/multiplayer"]
  ]],
  ["参考与帮助", "Reference and help", [
    ["API 参考","API reference","api-reference/index"],
    ["常见问题","Frequently asked questions","faq"],
    ["故障排查","Troubleshooting","troubleshooting"],
    ["贡献指南","Contributing","contributing"],
    ["开发环境","Development environment","contributor/development-environment"]
  ]]
] as const

const main = [
  ["入门", "Get started", "getting-started/index", "getting-started/"],
  ["指南", "Guides", "guides/index", "(guides|core|game|godot|ecs|best-practices)/"],
  ["示例", "Examples", "tutorials/index", "tutorials/"],
  ["参考", "Reference", "api-reference/index", "(api-reference|abstractions|source-generators|contributor)/|faq|troubleshooting|contributing"]
] as const

export function navigation(locale: 'en' | 'zh', prefix: string): DefaultTheme.Config {
  const language = locale === 'zh' ? 0 : 1
  const navLink = (route: string) => prefix + '/' + route.replace(/\/index$/, '/')

  return {
    nav: main.map(item => ({
      text: item[language],
      link: navLink(item[2]),
      activeMatch: `^${prefix}/(?:${item[3]})`
    })),
    sidebar: [
      ...groups.map(group => ({
        text: group[language],
        collapsed: true,
        items: group[2].map(item => ({ text: item[language], link: navLink(item[2]) }))
      }))
    ]
  }
}
