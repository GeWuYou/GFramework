using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.logging;
using GFramework.Core.Abstractions.properties;
using GFramework.Core.architecture;
using GFramework.Core.logging;
using NUnit.Framework;

namespace GFramework.Core.Tests.architecture;

[TestFixture]
public class ArchitectureConfigurationTests
{
    private ArchitectureConfiguration? _configuration;

    [SetUp]
    public void SetUp()
    {
        _configuration = new ArchitectureConfiguration();
    }

    [Test]
    public void Constructor_Should_Initialize_LoggerProperties_With_Default_Values()
    {
        Assert.That(_configuration, Is.Not.Null);
        Assert.That(_configuration!.LoggerProperties, Is.Not.Null);
    }

    [Test]
    public void LoggerProperties_Should_Use_ConsoleLoggerFactoryProvider_By_Default()
    {
        Assert.That(_configuration!.LoggerProperties.LoggerFactoryProvider, 
            Is.InstanceOf<ConsoleLoggerFactoryProvider>());
    }

    [Test]
    public void LoggerProperties_Should_Use_Info_LogLevel_By_Default()
    {
        var consoleProvider = _configuration!.LoggerProperties.LoggerFactoryProvider 
            as ConsoleLoggerFactoryProvider;
        
        Assert.That(consoleProvider, Is.Not.Null);
        Assert.That(consoleProvider!.MinLevel, Is.EqualTo(LogLevel.Info));
    }

    [Test]
    public void ArchitectureProperties_Should_Have_AllowLateRegistration_Set_To_False_By_Default()
    {
        Assert.That(_configuration!.ArchitectureProperties.AllowLateRegistration, 
            Is.False);
    }

    [Test]
    public void ArchitectureProperties_Should_Have_StrictPhaseValidation_Set_To_True_By_Default()
    {
        Assert.That(_configuration!.ArchitectureProperties.StrictPhaseValidation, 
            Is.True);
    }

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

    [Test]
    public void ArchitectureConfiguration_Should_Implement_IArchitectureConfiguration_Interface()
    {
        Assert.That(_configuration, Is.InstanceOf<IArchitectureConfiguration>());
    }

    [Test]
    public void New_Instance_Should_Not_Share_LoggerProperties_With_Other_Instance()
    {
        var config1 = new ArchitectureConfiguration();
        var config2 = new ArchitectureConfiguration();
        
        Assert.That(config1.LoggerProperties, Is.Not.SameAs(config2.LoggerProperties));
    }

    [Test]
    public void New_Instance_Should_Not_Share_ArchitectureProperties_With_Other_Instance()
    {
        var config1 = new ArchitectureConfiguration();
        var config2 = new ArchitectureConfiguration();
        
        Assert.That(config1.ArchitectureProperties, 
            Is.Not.SameAs(config2.ArchitectureProperties));
    }
}
