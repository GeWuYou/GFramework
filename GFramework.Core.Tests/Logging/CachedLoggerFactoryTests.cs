// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using GFramework.Core.Abstractions.Logging;
using GFramework.Core.Logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.Logging;

/// <summary>
///     测试 CachedLoggerFactory 的功能和行为
/// </summary>
[TestFixture]
public class CachedLoggerFactoryTests
{
    [Test]
    public void Constructor_WithNullInnerFactory_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CachedLoggerFactory(null!));
    }

    [Test]
    public void GetLogger_WithSameNameAndLevel_ShouldReturnSameInstance()
    {
        var innerFactory = new ConsoleLoggerFactory();
        var cachedFactory = new CachedLoggerFactory(innerFactory);

        var logger1 = cachedFactory.GetLogger("TestLogger", LogLevel.Info);
        var logger2 = cachedFactory.GetLogger("TestLogger", LogLevel.Info);

        Assert.That(logger1, Is.SameAs(logger2));
    }

    [Test]
    public void GetLogger_WithDifferentNames_ShouldReturnDifferentInstances()
    {
        var innerFactory = new ConsoleLoggerFactory();
        var cachedFactory = new CachedLoggerFactory(innerFactory);

        var logger1 = cachedFactory.GetLogger("Logger1", LogLevel.Info);
        var logger2 = cachedFactory.GetLogger("Logger2", LogLevel.Info);

        Assert.That(logger1, Is.Not.SameAs(logger2));
    }

    [Test]
    public void GetLogger_WithDifferentLevels_ShouldReturnDifferentInstances()
    {
        var innerFactory = new ConsoleLoggerFactory();
        var cachedFactory = new CachedLoggerFactory(innerFactory);

        var logger1 = cachedFactory.GetLogger("TestLogger", LogLevel.Info);
        var logger2 = cachedFactory.GetLogger("TestLogger", LogLevel.Debug);

        Assert.That(logger1, Is.Not.SameAs(logger2));
    }

    [Test]
    public void GetLogger_MultipleCalls_ShouldOnlyCreateOnce()
    {
        var trackingFactory = new TrackingLoggerFactory();
        var cachedFactory = new CachedLoggerFactory(trackingFactory);

        cachedFactory.GetLogger("TestLogger", LogLevel.Info);
        cachedFactory.GetLogger("TestLogger", LogLevel.Info);
        cachedFactory.GetLogger("TestLogger", LogLevel.Info);

        Assert.That(trackingFactory.CreateCount, Is.EqualTo(1));
    }

    [Test]
    public void GetLogger_WithMultipleNamesAndLevels_ShouldCacheCorrectly()
    {
        var trackingFactory = new TrackingLoggerFactory();
        var cachedFactory = new CachedLoggerFactory(trackingFactory);

        cachedFactory.GetLogger("Logger1", LogLevel.Info);
        cachedFactory.GetLogger("Logger1", LogLevel.Debug);
        cachedFactory.GetLogger("Logger2", LogLevel.Info);
        cachedFactory.GetLogger("Logger1", LogLevel.Info); // 缓存命中
        cachedFactory.GetLogger("Logger2", LogLevel.Info); // 缓存命中

        Assert.That(trackingFactory.CreateCount, Is.EqualTo(3));
    }

    // 辅助测试类
    private class TrackingLoggerFactory : ILoggerFactory
    {
        public int CreateCount { get; private set; }

        public ILogger GetLogger(string name, LogLevel minLevel = LogLevel.Info)
        {
            CreateCount++;
            return new ConsoleLogger(name, minLevel, new StringWriter(), false);
        }
    }
}