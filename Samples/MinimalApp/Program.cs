// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalApp;

// 最小非游戏应用样例：标准 DI + 标准日志。
// 演示两条相互独立的使用路径：
// 1) 完全不使用 Architecture：ServiceCollection + ILoggerFactory + ILogger<T>；
// 2) 可选使用 Architecture 编排：普通服务不要求 Model/System/Utility 角色，
//    组件通过构造注入获取 ILogger<T>，销毁时由架构释放实例日志工厂。

// 1) 纯标准路径：与任何普通 .NET 应用一致，框架不介入。
var services = new ServiceCollection();
services.AddSingleton<Greeter>();
services.AddLogging(builder => builder.AddSimpleConsole());

await using var provider = services.BuildServiceProvider();
var greeter = provider.GetRequiredService<Greeter>();
greeter.Greet("standalone");

// 2) Architecture 编排路径：实例日志工厂属于当前架构，销毁时释放。
var architecture = new OrderArchitecture();
await architecture.InitializeAsync().ConfigureAwait(false);
architecture.PlaceDemoOrder("demo-0001");
await architecture.DestroyAsync().ConfigureAwait(false);
