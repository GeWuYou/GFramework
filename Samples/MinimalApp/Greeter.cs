// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace MinimalApp;

/// <summary>纯标准路径下的示例服务；只依赖标准日志契约。</summary>
/// <param name="logger">标准日志记录器。</param>
internal sealed class Greeter(ILogger<Greeter> logger)
{
    /// <summary>输出一条问候日志。</summary>
    /// <param name="who">问候对象。</param>
    public void Greet(string who) => logger.LogInformation("Hello {Who}", who);
}
