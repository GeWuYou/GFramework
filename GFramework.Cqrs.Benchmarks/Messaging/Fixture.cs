// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using BenchmarkDotNet.Loggers;
using System;
using System.Globalization;

namespace GFramework.Cqrs.Benchmarks.Messaging;

/// <summary>
///     为 CQRS benchmark 运行打印并验证当前场景配置，避免矩阵配置与实际运行环境漂移。
/// </summary>
internal static class Fixture
{
    /// <summary>
    ///     输出当前 benchmark 配置并验证关键环境变量。
    /// </summary>
    /// <param name="scenario">当前 benchmark 场景名称。</param>
    /// <param name="handlerCount">当前场景的处理器数量。</param>
    /// <param name="pipelineCount">当前场景的 pipeline 行为数量。</param>
    public static void Setup(string scenario, int handlerCount, int pipelineCount)
    {
        ConsoleLogger.Default.WriteLineHeader("GFramework.Cqrs benchmark config");
        ConsoleLogger.Default.WriteLineInfo($"Scenario      = {scenario}");
        ConsoleLogger.Default.WriteLineInfo(string.Create(CultureInfo.InvariantCulture, $"HandlerCount  = {handlerCount}"));
        ConsoleLogger.Default.WriteLineInfo(string.Create(CultureInfo.InvariantCulture, $"PipelineCount = {pipelineCount}"));

        var environmentScenario = Environment.GetEnvironmentVariable("GFRAMEWORK_CQRS_BENCHMARK_SCENARIO");
        if (!string.IsNullOrWhiteSpace(environmentScenario) &&
            !string.Equals(environmentScenario, scenario, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Scenario mismatch. Expected '{environmentScenario}', actual '{scenario}'.");
        }
    }
}
