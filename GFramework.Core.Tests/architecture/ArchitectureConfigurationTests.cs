using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.architecture;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.architecture;

/// <summary>
///     ArchitectureConfiguration类的单元测试
///     测试内容包括：
///     - 构造函数默认值
///     - LoggerProperties默认配置（ConsoleLoggerFactoryProvider + Info级别）
///     - ArchitectureProperties默认配置（AllowLateRegistration=false, StrictPhaseValidation=true）
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
    ///     测试LoggerProperties默认使用ConsoleLoggerFactoryProvider
    /// </summary>
    [Test]
    public void LoggerProperties_Should_Use_ConsoleLoggerFactoryProvider_By_Default()
    {
        Assert.That(_configuration!.LoggerProperties.LoggerFactoryProvider,
            Is.InstanceOf<ConsoleLoggerFactoryProvider>());
    }

    /// <summary>
    ///     测试LoggerProperties默认使用Info日志级别
    /// </summary>
    [Test]
    public void LoggerProperties_Should_Use_Info_LogLevel_By_Default()
    {
        var consoleProvider = _configuration!.LoggerProperties.LoggerFactoryProvider
            as ConsoleLoggerFactoryProvider;

        Assert.That(consoleProvider, Is.Not.Null);
        Assert.That(consoleProvider!.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    /// <summary>
    ///     测试ArchitectureProperties的AllowLateRegistration默认为false
    /// </summary>
    [Test]
    public void ArchitectureProperties_Should_Have_AllowLateRegistration_Set_To_False_By_Default()
    {
        Assert.That(_configuration!.ArchitectureProperties.AllowLateRegistration,
            Is.False);
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
        var customProvider = new ConsoleLoggerFactoryProvider { MinLevel = LogLevel.Debug };
        var customLoggerProperties = new LoggerProperties
        {
            LoggerFactoryProvider = customProvider
        };

        _configuration!.LoggerProperties = customLoggerProperties;

        Assert.That(_configuration.LoggerProperties, Is.SameAs(customLoggerProperties));
        Assert.That(_configuration.LoggerProperties.LoggerFactoryProvider,
            Is.InstanceOf<ConsoleLoggerFactoryProvider>());
        var currentProvider = _configuration.LoggerProperties.LoggerFactoryProvider
            as ConsoleLoggerFactoryProvider;
        Assert.That(currentProvider!.MinLevel, Is.EqualTo(LogLevel.Debug));
    }

    /// <summary>
    ///     测试ArchitectureProperties可以被自定义配置替换
    /// </summary>
    [Test]
    public void ArchitectureProperties_Should_Be_Replaced_With_Custom_Configuration()
    {
        var customProperties = new ArchitectureProperties
        {
            AllowLateRegistration = true,
            StrictPhaseValidation = false
        };

        _configuration!.ArchitectureProperties = customProperties;

        Assert.That(_configuration.ArchitectureProperties, Is.SameAs(customProperties));
        Assert.That(_configuration.ArchitectureProperties.AllowLateRegistration,
            Is.True);
        Assert.That(_configuration.ArchitectureProperties.StrictPhaseValidation,
            Is.False);
    }

    /// <summary>
    ///     测试LoggerProperties可以独立修改
    /// </summary>
    [Test]
    public void LoggerProperties_Should_Be_Modifiable_Independently()
    {
        var originalProvider = _configuration!.LoggerProperties.LoggerFactoryProvider
            as ConsoleLoggerFactoryProvider;

        originalProvider!.MinLevel = LogLevel.Debug;

        var modifiedProvider = _configuration.LoggerProperties.LoggerFactoryProvider
            as ConsoleLoggerFactoryProvider;
        Assert.That(modifiedProvider!.MinLevel, Is.EqualTo(LogLevel.Debug));
    }

    /// <summary>
    ///     测试ArchitectureProperties可以独立修改
    /// </summary>
    [Test]
    public void ArchitectureProperties_Should_Be_Modifiable_Independently()
    {
        _configuration!.ArchitectureProperties.AllowLateRegistration = true;
        _configuration.ArchitectureProperties.StrictPhaseValidation = false;

        Assert.That(_configuration.ArchitectureProperties.AllowLateRegistration,
            Is.True);
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