// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Properties;
using GFramework.Core.Architectures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     ArchitectureConfiguration类的单元测试
///     测试内容包括：
///     - 构造函数默认值
///     - LoggerProperties默认配置（LoggerFactory为null，由架构创建默认工厂）
///     - ArchitectureProperties默认配置（StrictPhaseValidation=true）
///     - 自定义配置替换
///     - LoggerProperties独立修改
///     - ArchitectureProperties独立修改
///     - IArchitectureConfiguration接口实现验证
/// </summary>
[TestFixture]
public class ArchitectureConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        _configuration = new ArchitectureConfiguration();
    }

    private ArchitectureConfiguration? _configuration;

    /// <summary>
    ///     测试构造函数是否正确初始化LoggerProperties
    /// </summary>
    [Test]
    public void Constructor_Should_Initialize_LoggerProperties_With_Default_Values()
    {
        Assert.That(_configuration, Is.Not.Null);
        Assert.That(_configuration!.LoggerProperties, Is.Not.Null);
    }

    /// <summary>
    ///     测试LoggerProperties默认不携带外部工厂（架构为每个实例创建自己的默认工厂）
    /// </summary>
    [Test]
    public void LoggerProperties_Should_Default_To_Null_LoggerFactory()
    {
        Assert.That(_configuration!.LoggerProperties.LoggerFactory, Is.Null);
    }

    /// <summary>
    ///     测试ArchitectureProperties的StrictPhaseValidation默认为true
    /// </summary>
    [Test]
    public void ArchitectureProperties_Should_Have_StrictPhaseValidation_Set_To_True_By_Default()
    {
        Assert.That(_configuration!.ArchitectureProperties.StrictPhaseValidation,
            Is.True);
    }

    /// <summary>
    ///     测试LoggerProperties可以被自定义配置替换
    /// </summary>
    [Test]
    public void LoggerProperties_Should_Be_Replaced_With_Custom_Configuration()
    {
        var customFactory = NullLoggerFactory.Instance;
        var customLoggerProperties = new LoggerProperties
        {
            LoggerFactory = customFactory
        };

        _configuration!.LoggerProperties = customLoggerProperties;

        Assert.Multiple(() =>
        {
            Assert.That(_configuration.LoggerProperties, Is.SameAs(customLoggerProperties));
            Assert.That(_configuration.LoggerProperties.LoggerFactory, Is.SameAs(customFactory));
        });
    }

    /// <summary>
    ///     测试ArchitectureProperties可以被自定义配置替换
    /// </summary>
    [Test]
    public void ArchitectureProperties_Should_Be_Replaced_With_Custom_Configuration()
    {
        var customProperties = new ArchitectureProperties
        {
            StrictPhaseValidation = false
        };

        _configuration!.ArchitectureProperties = customProperties;

        Assert.That(_configuration.ArchitectureProperties, Is.SameAs(customProperties));
        Assert.That(_configuration.ArchitectureProperties.StrictPhaseValidation,
            Is.False);
    }

    /// <summary>
    ///     测试LoggerProperties可以独立修改
    /// </summary>
    [Test]
    public void LoggerProperties_Should_Be_Modifiable_Independently()
    {
        var factory = NullLoggerFactory.Instance;

        _configuration!.LoggerProperties.LoggerFactory = factory;

        Assert.That(_configuration.LoggerProperties.LoggerFactory, Is.SameAs(factory));
    }

    /// <summary>
    ///     测试ArchitectureProperties可以独立修改
    /// </summary>
    [Test]
    public void ArchitectureProperties_Should_Be_Modifiable_Independently()
    {
        _configuration!.ArchitectureProperties.StrictPhaseValidation = false;

        Assert.That(_configuration.ArchitectureProperties.StrictPhaseValidation,
            Is.False);
    }

    /// <summary>
    ///     测试ArchitectureConfiguration实现IArchitectureConfiguration接口
    /// </summary>
    [Test]
    public void ArchitectureConfiguration_Should_Implement_IArchitectureConfiguration_Interface()
    {
        Assert.That(_configuration, Is.InstanceOf<IArchitectureConfiguration>());
    }

    /// <summary>
    ///     测试新实例不与其他实例共享LoggerProperties
    /// </summary>
    [Test]
    public void New_Instance_Should_Not_Share_LoggerProperties_With_Other_Instance()
    {
        var config1 = new ArchitectureConfiguration();
        var config2 = new ArchitectureConfiguration();

        Assert.That(config1.LoggerProperties, Is.Not.SameAs(config2.LoggerProperties));
    }

    /// <summary>
    ///     测试新实例不与其他实例共享ArchitectureProperties
    /// </summary>
    [Test]
    public void New_Instance_Should_Not_Share_ArchitectureProperties_With_Other_Instance()
    {
        var config1 = new ArchitectureConfiguration();
        var config2 = new ArchitectureConfiguration();

        Assert.That(config1.ArchitectureProperties,
            Is.Not.SameAs(config2.ArchitectureProperties));
    }
}
