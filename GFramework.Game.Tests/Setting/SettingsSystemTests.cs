// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Events;
using GFramework.Core.Abstractions.Rule;
using GFramework.Core.Architectures;
using GFramework.Core.Events;
using GFramework.Core.Ioc;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Setting;
using GFramework.Game.Setting.Events;

namespace GFramework.Game.Tests.Setting;

/// <summary>
///     覆盖 <see cref="SettingsSystem" /> 的系统层语义，确保系统对模型编排、事件发送和重置流程保持稳定。
/// </summary>
[TestFixture]
public sealed class SettingsSystemTests
{
    /// <summary>
    ///     验证 <see cref="SettingsSystem.ApplyAll" /> 会尝试应用全部 applicator，并为成功与失败结果分别发送事件。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task ApplyAll_Should_Apply_All_Applicators_And_Publish_Result_Events()
    {
        var successfulApplicator = new PrimaryTestSettings();
        var failingApplicator = new SecondaryTestSettings(throwOnApply: true);
        var model = new FakeSettingsModel(successfulApplicator, failingApplicator);
        var context = CreateContext(model);
        var system = CreateSystem(context);

        var applyingEventCount = 0;
        var appliedEventCount = 0;
        var failedEventCount = 0;

        context.RegisterEvent<SettingsApplyingEvent<ISettingsSection>>(_ => applyingEventCount++);
        context.RegisterEvent<SettingsAppliedEvent<ISettingsSection>>(eventData =>
        {
            appliedEventCount++;
            if (!eventData.Success)
            {
                failedEventCount++;
            }
        });

        await system.ApplyAll();

        Assert.Multiple(() =>
        {
            Assert.That(successfulApplicator.ApplyCount, Is.EqualTo(1));
            Assert.That(failingApplicator.ApplyCount, Is.EqualTo(1));
            Assert.That(applyingEventCount, Is.EqualTo(2));
            Assert.That(appliedEventCount, Is.EqualTo(2));
            Assert.That(failedEventCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证 <see cref="SettingsSystem.SaveAll" /> 会直接委托给模型层统一保存。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task SaveAll_Should_Delegate_To_Model()
    {
        var model = new FakeSettingsModel(new PrimaryTestSettings());
        var system = CreateSystem(CreateContext(model));

        await system.SaveAll();

        Assert.That(model.SaveAllCallCount, Is.EqualTo(1));
    }

    /// <summary>
    ///     验证 <see cref="SettingsSystem.ResetAll" /> 会先委托模型统一重置，再重新应用全部 applicator。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task ResetAll_Should_Reset_Model_And_Reapply_All_Applicators()
    {
        var primaryApplicator = new PrimaryTestSettings();
        var secondaryApplicator = new SecondaryTestSettings();
        var model = new FakeSettingsModel(primaryApplicator, secondaryApplicator);
        var system = CreateSystem(CreateContext(model));

        await system.ResetAll();

        Assert.Multiple(() =>
        {
            Assert.That(model.ResetAllCallCount, Is.EqualTo(1));
            Assert.That(primaryApplicator.ApplyCount, Is.EqualTo(1));
            Assert.That(secondaryApplicator.ApplyCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证 <see cref="SettingsSystem.Reset{T}" /> 会重置目标数据类型，并只重新应用对应的 applicator。
    /// </summary>
    /// <returns>表示异步测试完成的任务。</returns>
    [Test]
    public async Task Reset_Should_Reset_Target_Data_And_Reapply_Target_Applicator()
    {
        var primaryApplicator = new PrimaryTestSettings();
        var secondaryApplicator = new SecondaryTestSettings();
        var model = new FakeSettingsModel(primaryApplicator, secondaryApplicator);
        var system = CreateSystem(CreateContext(model));

        await system.Reset<PrimaryTestSettings>();

        Assert.Multiple(() =>
        {
            Assert.That(model.ResetTypes, Is.EquivalentTo(new[] { typeof(PrimaryTestSettings) }));
            Assert.That(primaryApplicator.ApplyCount, Is.EqualTo(1));
            Assert.That(secondaryApplicator.ApplyCount, Is.Zero);
        });
    }

    /// <summary>
    ///     创建带事件总线和设置模型的真实架构上下文。
    /// </summary>
    /// <param name="model">测试使用的设置模型。</param>
    /// <returns>可供系统解析依赖与发送事件的上下文。</returns>
    private static ArchitectureContext CreateContext(ISettingsModel model)
    {
        var container = new MicrosoftDiContainer();
        container.Register<IEventBus>(new EventBus());
        container.Register<ISettingsModel>(model);
        container.Freeze();
        return new ArchitectureContext(container);
    }

    /// <summary>
    ///     创建并初始化绑定到指定上下文的设置系统。
    /// </summary>
    /// <param name="context">系统运行所需的架构上下文。</param>
    /// <returns>已完成初始化的设置系统实例。</returns>
    private static SettingsSystem CreateSystem(IArchitectureContext context)
    {
        var system = new SettingsSystem();
        ((IContextAware)system).SetContext(context);
        system.Initialize();
        return system;
    }

    /// <summary>
    ///     用于系统层测试的简化设置模型，记录系统对模型的调用行为。
    /// </summary>
    private sealed class FakeSettingsModel : ISettingsModel
    {
        private readonly IReadOnlyDictionary<Type, IResetApplyAbleSettings> _applicators;

        /// <summary>
        ///     初始化测试模型，并注册参与测试的 applicator 集合。
        /// </summary>
        /// <param name="applicators">测试使用的 applicator。</param>
        public FakeSettingsModel(params IResetApplyAbleSettings[] applicators)
        {
            _applicators = applicators.ToDictionary(applicator => applicator.GetType());
        }

        /// <summary>
        ///     获取保存全量设置的调用次数。
        /// </summary>
        public int SaveAllCallCount { get; private set; }

        /// <summary>
        ///     获取重置全部设置的调用次数。
        /// </summary>
        public int ResetAllCallCount { get; private set; }

        /// <summary>
        ///     获取被请求重置的设置数据类型列表。
        /// </summary>
        public List<Type> ResetTypes { get; } = [];

        /// <inheritdoc />
        public bool IsInitialized => true;

        /// <inheritdoc />
        public void SetContext(IArchitectureContext context)
        {
        }

        /// <inheritdoc />
        public IArchitectureContext GetContext()
        {
            throw new NotSupportedException("Fake settings model does not expose a context.");
        }

        /// <inheritdoc />
        public void OnArchitecturePhase(ArchitecturePhase phase)
        {
        }

        /// <inheritdoc />
        public void Initialize()
        {
        }

        /// <inheritdoc />
        public T GetData<T>() where T : class, ISettingsData, new()
        {
            return new T();
        }

        /// <inheritdoc />
        public IEnumerable<ISettingsData> AllData()
        {
            return [];
        }

        /// <inheritdoc />
        public ISettingsModel RegisterApplicator<T>(T applicator) where T : class, IResetApplyAbleSettings
        {
            return this;
        }

        /// <inheritdoc />
        public T? GetApplicator<T>() where T : class, IResetApplyAbleSettings
        {
            return _applicators.TryGetValue(typeof(T), out var applicator) ? (T)applicator : null;
        }

        /// <inheritdoc />
        public IEnumerable<IResetApplyAbleSettings> AllApplicators()
        {
            return _applicators.Values;
        }

        /// <inheritdoc />
        public ISettingsModel RegisterMigration(ISettingsMigration migration)
        {
            return this;
        }

        /// <inheritdoc />
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SaveAllAsync()
        {
            SaveAllCallCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ApplyAllAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Reset<T>() where T : class, ISettingsData, new()
        {
            ResetTypes.Add(typeof(T));
        }

        /// <inheritdoc />
        public void ResetAll()
        {
            ResetAllCallCount++;
        }
    }

    /// <summary>
    ///     为系统层测试提供的最小设置数据实现。
    /// </summary>
    private abstract class TestSettingsBase : ISettingsData, IResetApplyAbleSettings
    {
        /// <inheritdoc />
        public int Version { get; set; } = 1;

        /// <inheritdoc />
        public DateTime LastModified { get; } = DateTime.UtcNow;

        /// <summary>
        ///     获取测试用的数值字段，用于确认重置与加载行为。
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        ///     获取应用操作被调用的次数。
        /// </summary>
        public int ApplyCount { get; private set; }

        /// <inheritdoc />
        public ISettingsData Data => this;

        /// <inheritdoc />
        public Type DataType => GetType();

        /// <summary>
        ///     获取或设置是否在应用时抛出异常。
        /// </summary>
        protected bool ThrowOnApply { get; init; }

        /// <inheritdoc />
        public void Reset()
        {
            Value = 0;
        }

        /// <inheritdoc />
        public void LoadFrom(ISettingsData source)
        {
            if (source is TestSettingsBase data)
            {
                Value = data.Value;
                Version = data.Version;
            }
        }

        /// <inheritdoc />
        public async Task ApplyAsync()
        {
            ApplyCount++;

            await Task.Yield();

            if (ThrowOnApply)
            {
                throw new InvalidOperationException("Test applicator failed.");
            }
        }
    }

    /// <summary>
    ///     代表主设置分支的测试设置对象。
    /// </summary>
    private sealed class PrimaryTestSettings : TestSettingsBase
    {
    }

    /// <summary>
    ///     代表第二个设置分支的测试设置对象，可选择在应用时失败。
    /// </summary>
    private sealed class SecondaryTestSettings : TestSettingsBase
    {
        /// <summary>
        ///     初始化第二个测试设置对象。
        /// </summary>
        /// <param name="throwOnApply">是否在应用时抛出异常。</param>
        public SecondaryTestSettings(bool throwOnApply = false)
        {
            ThrowOnApply = throwOnApply;
        }
    }
}
