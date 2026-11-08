// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Threading;
using GFramework.Core.Architectures;
using GFramework.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Cqrs.Tests.Cqrs;

/// <summary>
///     验证 generated request invoker provider 的 registrar 接线与 dispatcher 消费语义。
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class CqrsGeneratedRequestInvokerProviderTests
{
    /// <summary>
    ///     在每个用例前重置 registrar / dispatcher 的静态缓存，避免跨用例共享状态影响断言。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        GeneratedRequestPipelineTrackingBehavior.InvocationCount = 0;
        GeneratedStreamPipelineTrackingBehavior.InvocationCount = 0;
        ClearRegistrarCaches();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     在每个用例后清理静态缓存。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GeneratedRequestPipelineTrackingBehavior.InvocationCount = 0;
        GeneratedStreamPipelineTrackingBehavior.InvocationCount = 0;
        ClearRegistrarCaches();
        ClearDispatcherCaches();
    }

    /// <summary>
    ///     验证 registrar 激活 generated registry 后，会把 request invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Request_Invoker_Provider()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        container.Freeze();
        var providers = container.GetAll<ICqrsRequestInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(GeneratedRequestInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 request handler interface 仍可直接表达时，
    ///     registrar 仍会把 generated request invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Request_Invoker_Provider_For_Hidden_Implementation()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedRequestInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        container.Freeze();
        var providers = container.GetAll<ICqrsRequestInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(HiddenImplementationGeneratedRequestInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证 registrar 激活 generated registry 后，会把 stream invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Stream_Invoker_Provider()
    {
        var generatedAssembly = CreateGeneratedStreamInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        container.Freeze();
        var providers = container.GetAll<ICqrsStreamInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(GeneratedStreamInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证 direct generated-registry 激活入口只会接入指定 registry，而不会顺手把同一测试程序集里的其他 registry 一并注册。
    /// </summary>
    [Test]
    public void RegisterGeneratedRegistry_Should_Register_Only_The_Selected_Provider()
    {
        var container = new ServiceTestHost();
        var logger = NullLogger<CqrsGeneratedRequestInvokerProviderTests>.Instance;

        CqrsHandlerRegistrar.RegisterGeneratedRegistry(
            container.Services,
            typeof(GeneratedRequestInvokerProviderRegistry),
            logger);

        container.Freeze();
        var requestProviders = container.GetAll<ICqrsRequestInvokerProvider>();
        var streamProviders = container.GetAll<ICqrsStreamInvokerProvider>();

        Assert.Multiple(() =>
        {
            Assert.That(
                requestProviders.Select(static provider => provider.GetType()),
                Is.EqualTo([typeof(GeneratedRequestInvokerProviderRegistry)]));
            Assert.That(streamProviders, Is.Empty);
        });
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 stream handler interface 仍可直接表达时，
    ///     registrar 仍会把 generated stream invoker provider 注册到容器中。
    /// </summary>
    [Test]
    public void RegisterHandlers_Should_Register_Generated_Stream_Invoker_Provider_For_Hidden_Implementation()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedStreamInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);

        container.Freeze();
        var providers = container.GetAll<ICqrsStreamInvokerProvider>();

        Assert.That(
            providers.Select(static provider => provider.GetType()),
            Is.EqualTo([typeof(HiddenImplementationGeneratedStreamInvokerProviderRegistry)]));
    }

    /// <summary>
    ///     验证 dispatcher 在首次创建 request binding 时，会优先消费 generated request invoker provider。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Generated_Request_Invoker_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload"));
        Assert.That(response, Is.EqualTo("generated:payload"));
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 request handler interface 仍可直接表达时，
    ///     dispatcher 仍会消费 generated request invoker descriptor。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Generated_Request_Invoker_For_Hidden_Implementation_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedRequestInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(
            new HiddenImplementationRequestInvokerContainer.VisibleRequest("payload"));
        Assert.That(response, Is.EqualTo("generated-hidden:payload"));
    }

    /// <summary>
    ///     验证 generated request invoker 与 request pipeline 行为同时存在时，
    ///     dispatcher 仍会保持 generated invoker 优先，并正确复用现有 request 执行链。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Generated_Request_Invoker_Inside_Request_Pipeline()
    {
        var generatedAssembly = CreateGeneratedRequestInvokerAssembly();
        var container = new ServiceTestHost();
        container.Services.AddCqrsPipelineBehavior<GeneratedRequestPipelineTrackingBehavior>();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.EqualTo("generated:payload"));
            Assert.That(GeneratedRequestPipelineTrackingBehavior.InvocationCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证 dispatcher 在首次创建 stream binding 时，会优先消费 generated stream invoker provider。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_Generated_Stream_Invoker_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateGeneratedStreamInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3)));
        Assert.That(results, Is.EqualTo([30, 31]));
    }

    /// <summary>
    ///     验证 generated stream invoker 与 stream pipeline 行为同时存在时，
    ///     dispatcher 仍会保持 generated invoker 优先，并正确包裹到行为链内。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_Generated_Stream_Invoker_Inside_Stream_Pipeline()
    {
        var generatedAssembly = CreateGeneratedStreamInvokerAssembly();
        var container = new ServiceTestHost();
        container.Services.AddCqrsStreamPipelineBehavior<GeneratedStreamPipelineTrackingBehavior>();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(results, Is.EqualTo([30, 31]));
            Assert.That(GeneratedStreamPipelineTrackingBehavior.InvocationCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证 generated stream binding 与对应的 pipeline executor 在首次建流后会被缓存并复用，
    ///     同时保持 generated invoker 的结果与行为执行语义不变。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Reuse_Cached_Generated_Stream_Binding_And_Pipeline_Executor()
    {
        var generatedAssembly = CreateGeneratedStreamInvokerAssembly();
        var container = new ServiceTestHost();
        container.Services.AddCqrsStreamPipelineBehavior<GeneratedStreamPipelineTrackingBehavior>();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var streamBindings = GetDispatcherCacheField("StreamDispatchBindings");
        var requestType = typeof(GeneratedStreamInvokerRequest);
        var responseType = typeof(int);

        Assert.That(
            GetStreamPipelineExecutorValue(streamBindings, requestType, responseType, 1),
            Is.Null);

        var context = new CqrsTestHost(container);
        var firstResults = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        var bindingAfterFirstDispatch = GetPairCacheValue(streamBindings, requestType, responseType);
        var executorAfterFirstDispatch = GetStreamPipelineExecutorValue(streamBindings, requestType, responseType, 1);

        var secondResults = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        var bindingAfterSecondDispatch = GetPairCacheValue(streamBindings, requestType, responseType);
        var executorAfterSecondDispatch = GetStreamPipelineExecutorValue(streamBindings, requestType, responseType, 1);

        Assert.Multiple(() =>
        {
            Assert.That(firstResults, Is.EqualTo([30, 31]));
            Assert.That(secondResults, Is.EqualTo([30, 31]));
            Assert.That(bindingAfterFirstDispatch, Is.Not.Null);
            Assert.That(bindingAfterSecondDispatch, Is.SameAs(bindingAfterFirstDispatch));
            Assert.That(executorAfterFirstDispatch, Is.Not.Null);
            Assert.That(executorAfterSecondDispatch, Is.SameAs(executorAfterFirstDispatch));
            Assert.That(GeneratedStreamPipelineTrackingBehavior.InvocationCount, Is.EqualTo(2));
        });
    }

    /// <summary>
    ///     验证当实现类型隐藏、但 stream handler interface 仍可直接表达时，
    ///     dispatcher 仍会消费 generated stream invoker descriptor。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_Generated_Stream_Invoker_For_Hidden_Implementation_When_Provider_Is_Registered()
    {
        var generatedAssembly = CreateHiddenImplementationGeneratedStreamInvokerAssembly();
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(
            context.CreateStream(new HiddenImplementationStreamInvokerContainer.VisibleStreamRequest(3)));
        Assert.That(results, Is.EqualTo([300, 301]));
    }

    /// <summary>
    ///     验证当 registry 只暴露 request invoker provider 接口、但不提供可枚举描述符契约时，
    ///     dispatcher 仍会回退到既有反射路径，而不是错误依赖未预热的 generated metadata。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Request_Provider_Does_Not_Enumerate_Descriptors()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 registry 只暴露 stream invoker provider 接口、但不提供可枚举描述符契约时，
    ///     dispatcher 仍会回退到既有流式反射路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Stream_Provider_Does_Not_Enumerate_Descriptors()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 暴露实例方法时，
    ///     registrar 会放弃该 generated registry 并回退到运行时反射路径。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Generated_Request_Invoker_Is_Not_Static()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonStaticRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonStaticRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 返回与 dispatcher 委托签名不兼容的方法时，
    ///     dispatcher 会显式抛出契约错误。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Throw_When_Generated_Request_Invoker_Is_Incompatible()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(IncompatibleRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.IncompatibleRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("incompatible invoker"));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 暴露实例方法时，
    ///     registrar 会放弃该 generated registry 并回退到运行时反射路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Generated_Stream_Invoker_Is_Not_Static()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(NonStaticStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.NonStaticStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 返回与 dispatcher 委托签名不兼容的方法时，
    ///     dispatcher 会显式抛出契约错误。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Throw_When_Generated_Stream_Invoker_Is_Incompatible()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(IncompatibleStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.IncompatibleStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("incompatible invoker"));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 实现枚举契约、但返回空描述符集合时，
    ///     dispatcher 仍会回退到既有反射路径。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Request_Descriptor_Enumeration_Is_Empty()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(EmptyEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.EmptyEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 实现枚举契约、但返回空描述符集合时，
    ///     dispatcher 仍会回退到既有流式反射路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Stream_Descriptor_Enumeration_Is_Empty()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(EmptyEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.EmptyEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当 generated request invoker provider 的 descriptor 枚举抛出异常时，
    ///     registrar 会跳过 generated descriptor 预热并回退到反射路径。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Request_Descriptor_Enumeration_Throws()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(ThrowingEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.ThrowingEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当 generated stream invoker provider 的 descriptor 枚举抛出异常时，
    ///     registrar 会跳过 generated descriptor 预热并回退到反射建流路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Stream_Descriptor_Enumeration_Throws()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(ThrowingEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.ThrowingEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当 request descriptor 枚举返回重复 request-response pair 时，
    ///     registrar 会稳定保留首个有效描述符，并忽略后续重复项。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_First_Generated_Request_Descriptor_When_Duplicates_Are_Enumerated()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(DuplicateEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.DuplicateEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("generated:payload"));
    }

    /// <summary>
    ///     验证当 stream descriptor 枚举返回重复 request-response pair 时，
    ///     registrar 会稳定保留首个有效描述符，并忽略后续重复项。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_First_Generated_Stream_Descriptor_When_Duplicates_Are_Enumerated()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(DuplicateEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.DuplicateEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([30, 31]));
    }

    /// <summary>
    ///     验证当 request descriptor 枚举项与 provider 的 TryGetDescriptor 结果不一致时，
    ///     registrar 会忽略该坏 descriptor，并继续回退到反射路径。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Fall_Back_To_Runtime_Path_When_Enumerated_Request_Descriptor_Does_Not_Match_Provider()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(MismatchedEnumeratingRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.MismatchedEnumeratingRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("runtime:payload"));
    }

    /// <summary>
    ///     验证当首个 request descriptor 无效、后续同键 descriptor 有效时，
    ///     registrar 不会因为过早去重而丢掉本可注册的 generated descriptor。
    /// </summary>
    [Test]
    public async Task SendAsync_Should_Use_Later_Valid_Generated_Request_Descriptor_When_First_Duplicate_Is_Invalid()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(InvalidThenValidDuplicateRequestInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.InvalidThenValidDuplicateRequestInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var response = await context.SendRequestAsync(new GeneratedRequestInvokerRequest("payload")).ConfigureAwait(false);
        Assert.That(response, Is.EqualTo("generated:payload"));
    }

    /// <summary>
    ///     验证当 stream descriptor 枚举项与 provider 的 TryGetDescriptor 结果不一致时，
    ///     registrar 会忽略该坏 descriptor，并继续回退到反射建流路径。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Fall_Back_To_Runtime_Path_When_Enumerated_Stream_Descriptor_Does_Not_Match_Provider()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(MismatchedEnumeratingStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.MismatchedEnumeratingStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([3, 4]));
    }

    /// <summary>
    ///     验证当首个 stream descriptor 无效、后续同键 descriptor 有效时，
    ///     registrar 不会因为过早去重而丢掉本可注册的 generated descriptor。
    /// </summary>
    [Test]
    public async Task CreateStream_Should_Use_Later_Valid_Generated_Stream_Descriptor_When_First_Duplicate_Is_Invalid()
    {
        var generatedAssembly = CreateGeneratedAssembly(
            typeof(InvalidThenValidDuplicateStreamInvokerProviderRegistry),
            "GFramework.Cqrs.Tests.Cqrs.InvalidThenValidDuplicateStreamInvokerAssembly, Version=1.0.0.0");
        var container = new ServiceTestHost();

        CqrsTestRuntime.RegisterHandlers(container, generatedAssembly.Object);
        container.Freeze();

        var context = new CqrsTestHost(container);
        var results = await DrainAsync(context.CreateStream(new GeneratedStreamInvokerRequest(3))).ConfigureAwait(false);
        Assert.That(results, Is.EqualTo([30, 31]));
    }

    /// <summary>
    ///     记录 generated request invoker 与 request pipeline 行为组合时的命中次数。
    /// </summary>
    private sealed class GeneratedRequestPipelineTrackingBehavior
        : IPipelineBehavior<GeneratedRequestInvokerRequest, string>
    {
        private static int _invocationCount;

        /// <summary>
        ///     获取或重置当前测试进程中的行为触发次数。
        /// </summary>
        public static int InvocationCount
        {
            get => Volatile.Read(ref _invocationCount);
            set => Volatile.Write(ref _invocationCount, value);
        }

        /// <summary>
        ///     记录一次行为执行，然后继续执行 generated request invoker。
        /// </summary>
        /// <param name="message">当前请求消息。</param>
        /// <param name="next">下一个处理阶段。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>下游处理阶段返回的响应。</returns>
        public ValueTask<string> Handle(
            GeneratedRequestInvokerRequest message,
            MessageHandlerDelegate<GeneratedRequestInvokerRequest, string> next,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _invocationCount);
            return next(message, cancellationToken);
        }
    }

    /// <summary>
    ///     模拟返回实例 request invoker 方法的 generated registry。
    /// </summary>
    private sealed class NonStaticRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = new CqrsRequestInvokerDescriptor(
                    typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                    typeof(NonStaticRequestInvokerProviderRegistry).GetMethod(
                        nameof(InvokeGenerated),
                        BindingFlags.NonPublic | BindingFlags.Instance)!);
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsRequestInvokerDescriptorEntry(
                    typeof(GeneratedRequestInvokerRequest),
                    typeof(string),
                    new CqrsRequestInvokerDescriptor(
                        typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                        typeof(NonStaticRequestInvokerProviderRegistry).GetMethod(
                            nameof(InvokeGenerated),
                            BindingFlags.NonPublic | BindingFlags.Instance)!)),
            ];
        }

        private static ValueTask<string> InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return ValueTask.FromResult(string.Empty);
        }
    }

    /// <summary>
    ///     模拟返回不兼容 request invoker 方法的 generated registry。
    /// </summary>
    private sealed class IncompatibleRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptorEntry DescriptorEntry = new(
            typeof(GeneratedRequestInvokerRequest),
            typeof(string),
            new CqrsRequestInvokerDescriptor(
                typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
                typeof(IncompatibleRequestInvokerProviderRegistry).GetMethod(
                    nameof(InvokeGenerated),
                    BindingFlags.NonPublic | BindingFlags.Static)!));

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = DescriptorEntry.Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return [DescriptorEntry];
        }

        private static string InvokeGenerated(object handler, object request)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            return string.Empty;
        }
    }

    /// <summary>
    ///     模拟返回实例 stream invoker 方法的 generated registry。
    /// </summary>
    private sealed class NonStaticStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = new CqrsStreamInvokerDescriptor(
                    typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                    typeof(NonStaticStreamInvokerProviderRegistry).GetMethod(
                        nameof(InvokeGenerated),
                        BindingFlags.NonPublic | BindingFlags.Instance)!);
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsStreamInvokerDescriptorEntry(
                    typeof(GeneratedStreamInvokerRequest),
                    typeof(int),
                    new CqrsStreamInvokerDescriptor(
                        typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                        typeof(NonStaticStreamInvokerProviderRegistry).GetMethod(
                            nameof(InvokeGenerated),
                            BindingFlags.NonPublic | BindingFlags.Instance)!)),
            ];
        }

        private static object InvokeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return Array.Empty<int>().ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     模拟返回不兼容 stream invoker 方法的 generated registry。
    /// </summary>
    private sealed class IncompatibleStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsStreamInvokerDescriptorEntry DescriptorEntry = new(
            typeof(GeneratedStreamInvokerRequest),
            typeof(int),
            new CqrsStreamInvokerDescriptor(
                typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
                typeof(IncompatibleStreamInvokerProviderRegistry).GetMethod(
                    nameof(InvokeGenerated),
                    BindingFlags.NonPublic | BindingFlags.Static)!));

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = DescriptorEntry.Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return [DescriptorEntry];
        }

        private static object InvokeGenerated(object handler, object request)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            return Array.Empty<int>().ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     模拟只暴露 request provider 接口、但不暴露描述符枚举契约的 generated registry。
    /// </summary>
    private sealed class NonEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider
    {
        private static readonly CqrsRequestInvokerDescriptor Descriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }
    }

    /// <summary>
    ///     模拟只暴露 stream provider 接口、但不暴露描述符枚举契约的 generated registry。
    /// </summary>
    private sealed class NonEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider
    {
        private static readonly CqrsStreamInvokerDescriptor Descriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }
    }

    /// <summary>
    ///     模拟实现 request descriptor 枚举契约、但当前不暴露任何 descriptor 的 generated registry。
    /// </summary>
    private sealed class EmptyEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return [];
        }
    }

    /// <summary>
    ///     模拟实现 stream descriptor 枚举契约、但当前不暴露任何 descriptor 的 generated registry。
    /// </summary>
    private sealed class EmptyEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return [];
        }
    }

    /// <summary>
    ///     模拟 descriptor 枚举阶段抛出异常的 request invoker provider。
    /// </summary>
    private sealed class ThrowingEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptor Descriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            throw new InvalidOperationException("request descriptors failed");
        }
    }

    /// <summary>
    ///     模拟 descriptor 枚举阶段抛出异常的 stream invoker provider。
    /// </summary>
    private sealed class ThrowingEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsStreamInvokerDescriptor Descriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = Descriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            throw new InvalidOperationException("stream descriptors failed");
        }
    }

    /// <summary>
    ///     模拟返回重复 request descriptor 条目的 generated registry。
    /// </summary>
    private sealed class DuplicateEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptor PrimaryDescriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        private static readonly CqrsRequestInvokerDescriptor SecondaryDescriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(DuplicateEnumeratingRequestInvokerProviderRegistry).GetMethod(
                nameof(InvokeAlternativeGenerated),
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = PrimaryDescriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsRequestInvokerDescriptorEntry(typeof(GeneratedRequestInvokerRequest), typeof(string), PrimaryDescriptor),
                new CqrsRequestInvokerDescriptorEntry(typeof(GeneratedRequestInvokerRequest), typeof(string), SecondaryDescriptor),
            ];
        }

        private static ValueTask<string> InvokeAlternativeGenerated(
            object handler,
            object request,
            CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return ValueTask.FromResult("duplicate:payload");
        }
    }

    /// <summary>
    ///     模拟返回重复 stream descriptor 条目的 generated registry。
    /// </summary>
    private sealed class DuplicateEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsStreamInvokerDescriptor PrimaryDescriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        private static readonly CqrsStreamInvokerDescriptor SecondaryDescriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(DuplicateEnumeratingStreamInvokerProviderRegistry).GetMethod(
                nameof(InvokeAlternativeGenerated),
                BindingFlags.NonPublic | BindingFlags.Static)!);
        private static readonly int[] _sourceArray = [900, 901];

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = PrimaryDescriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsStreamInvokerDescriptorEntry(typeof(GeneratedStreamInvokerRequest), typeof(int), PrimaryDescriptor),
                new CqrsStreamInvokerDescriptorEntry(typeof(GeneratedStreamInvokerRequest), typeof(int), SecondaryDescriptor),
            ];
        }

        private static object InvokeAlternativeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return _sourceArray.ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     模拟枚举出的 request descriptor 与 provider 显式查询结果不一致的 generated registry。
    /// </summary>
    private sealed class MismatchedEnumeratingRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptor ProviderDescriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        private static readonly CqrsRequestInvokerDescriptor EnumeratedDescriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(MismatchedEnumeratingRequestInvokerProviderRegistry).GetMethod(
                nameof(InvokeAlternativeGenerated),
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = ProviderDescriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsRequestInvokerDescriptorEntry(
                    typeof(GeneratedRequestInvokerRequest),
                    typeof(string),
                    EnumeratedDescriptor),
            ];
        }

        private static ValueTask<string> InvokeAlternativeGenerated(
            object handler,
            object request,
            CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return ValueTask.FromResult("mismatched:payload");
        }
    }

    /// <summary>
    ///     模拟首个 request descriptor 无效、后续同键 descriptor 有效的 generated registry。
    /// </summary>
    private sealed class InvalidThenValidDuplicateRequestInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsRequestInvokerProvider,
        IEnumeratesCqrsRequestInvokerDescriptors
    {
        private static readonly CqrsRequestInvokerDescriptor InvalidDescriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(InvalidThenValidDuplicateRequestInvokerProviderRegistry).GetMethod(
                nameof(InvokeAlternativeGenerated),
                BindingFlags.NonPublic | BindingFlags.Static)!);

        private static readonly CqrsRequestInvokerDescriptor ValidDescriptor = new(
            typeof(IRequestHandler<GeneratedRequestInvokerRequest, string>),
            typeof(GeneratedRequestInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IRequestHandler<GeneratedRequestInvokerRequest, string>, GeneratedRequestInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsRequestInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedRequestInvokerRequest) && responseType == typeof(string))
            {
                descriptor = ValidDescriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsRequestInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsRequestInvokerDescriptorEntry(
                    typeof(GeneratedRequestInvokerRequest),
                    typeof(string),
                    InvalidDescriptor),
                new CqrsRequestInvokerDescriptorEntry(
                    typeof(GeneratedRequestInvokerRequest),
                    typeof(string),
                    ValidDescriptor),
            ];
        }

        private static ValueTask<string> InvokeAlternativeGenerated(
            object handler,
            object request,
            CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return ValueTask.FromResult("invalid-first:payload");
        }
    }

    /// <summary>
    ///     模拟枚举出的 stream descriptor 与 provider 显式查询结果不一致的 generated registry。
    /// </summary>
    private sealed class MismatchedEnumeratingStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsStreamInvokerDescriptor ProviderDescriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);

        private static readonly CqrsStreamInvokerDescriptor EnumeratedDescriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(MismatchedEnumeratingStreamInvokerProviderRegistry).GetMethod(
                nameof(InvokeAlternativeGenerated),
                BindingFlags.NonPublic | BindingFlags.Static)!);
        private static readonly int[] _sourceArray = [700, 701];

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = ProviderDescriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsStreamInvokerDescriptorEntry(
                    typeof(GeneratedStreamInvokerRequest),
                    typeof(int),
                    EnumeratedDescriptor),
            ];
        }

        private static object InvokeAlternativeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return _sourceArray.ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     模拟首个 stream descriptor 无效、后续同键 descriptor 有效的 generated registry。
    /// </summary>
    private sealed class InvalidThenValidDuplicateStreamInvokerProviderRegistry :
        ICqrsHandlerRegistry,
        ICqrsStreamInvokerProvider,
        IEnumeratesCqrsStreamInvokerDescriptors
    {
        private static readonly CqrsStreamInvokerDescriptor InvalidDescriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(InvalidThenValidDuplicateStreamInvokerProviderRegistry).GetMethod(
                nameof(InvokeAlternativeGenerated),
                BindingFlags.NonPublic | BindingFlags.Static)!);

        private static readonly CqrsStreamInvokerDescriptor ValidDescriptor = new(
            typeof(IStreamRequestHandler<GeneratedStreamInvokerRequest, int>),
            typeof(GeneratedStreamInvokerProviderRegistry).GetMethod(
                "InvokeGenerated",
                BindingFlags.NonPublic | BindingFlags.Static)!);
        private static readonly int[] _sourceArray = [800, 801];

        /// <inheritdoc />
        public void Register(IServiceCollection services, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(logger);

            services.AddTransient<IStreamRequestHandler<GeneratedStreamInvokerRequest, int>, GeneratedStreamInvokerRequestHandler>();
        }

        /// <inheritdoc />
        public bool TryGetDescriptor(
            Type requestType,
            Type responseType,
            out CqrsStreamInvokerDescriptor? descriptor)
        {
            ArgumentNullException.ThrowIfNull(requestType);
            ArgumentNullException.ThrowIfNull(responseType);

            if (requestType == typeof(GeneratedStreamInvokerRequest) && responseType == typeof(int))
            {
                descriptor = ValidDescriptor;
                return true;
            }

            descriptor = null;
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CqrsStreamInvokerDescriptorEntry> GetDescriptors()
        {
            return
            [
                new CqrsStreamInvokerDescriptorEntry(
                    typeof(GeneratedStreamInvokerRequest),
                    typeof(int),
                    InvalidDescriptor),
                new CqrsStreamInvokerDescriptorEntry(
                    typeof(GeneratedStreamInvokerRequest),
                    typeof(int),
                    ValidDescriptor),
            ];
        }

        private static object InvokeAlternativeGenerated(object handler, object request, CancellationToken cancellationToken)
        {
            // Keep the reflection/delegate contract even when this probe does not consume an input.
            _ = handler;
            _ = request;
            _ = cancellationToken;
            return _sourceArray.ToAsyncEnumerable();
        }
    }

    /// <summary>
    ///     创建带有 generated request invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateGeneratedRequestInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.GeneratedRequestInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(GeneratedRequestInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有 generated stream invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateGeneratedStreamInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.GeneratedStreamInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(GeneratedStreamInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有指定 generated registry 元数据的程序集替身。
    /// </summary>
    /// <param name="registryType">测试 registry 类型。</param>
    /// <param name="assemblyFullName">模拟程序集全名。</param>
    /// <returns>可用于 registrar 注册流程的程序集替身。</returns>
    private static Mock<Assembly> CreateGeneratedAssembly(Type registryType, string assemblyFullName)
    {
        ArgumentNullException.ThrowIfNull(registryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyFullName);

        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns(assemblyFullName);
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false))
            .Returns([new CqrsHandlerRegistryAttribute(registryType)]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有 hidden implementation request invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateHiddenImplementationGeneratedRequestInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.HiddenGeneratedRequestInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(HiddenImplementationGeneratedRequestInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     创建带有 hidden implementation stream invoker registry 元数据的程序集替身。
    /// </summary>
    private static Mock<Assembly> CreateHiddenImplementationGeneratedStreamInvokerAssembly()
    {
        var generatedAssembly = new Mock<Assembly>();
        generatedAssembly
            .SetupGet(static assembly => assembly.FullName)
            .Returns("GFramework.Cqrs.Tests.Cqrs.HiddenGeneratedStreamInvokerAssembly, Version=1.0.0.0");
        generatedAssembly
            .Setup(static assembly => assembly.GetCustomAttributes(typeof(CqrsHandlerRegistryAttribute), inherit: false))
            .Returns([new CqrsHandlerRegistryAttribute(typeof(HiddenImplementationGeneratedStreamInvokerProviderRegistry))]);
        return generatedAssembly;
    }

    /// <summary>
    ///     清空 registrar 静态缓存。
    /// </summary>
    private static void ClearRegistrarCaches()
    {
        ClearCache(GetRegistrarCacheField("AssemblyMetadataCache"));
        ClearCache(GetRegistrarCacheField("RegistryActivationMetadataCache"));
        ClearCache(GetRegistrarCacheField("LoadableTypesCache"));
        ClearCache(GetRegistrarCacheField("SupportedHandlerInterfacesCache"));
    }

    /// <summary>
    ///     清空 dispatcher 静态缓存。
    /// </summary>
    private static void ClearDispatcherCaches()
    {
        ClearCache(GetDispatcherCacheField("NotificationDispatchBindings"));
        ClearCache(GetDispatcherCacheField("RequestDispatchBindings"));
        ClearCache(GetDispatcherCacheField("StreamDispatchBindings"));
        ClearCache(GetDispatcherCacheField("GeneratedRequestInvokers"));
        ClearCache(GetDispatcherCacheField("GeneratedStreamInvokers"));
    }

    /// <summary>
    ///     通过反射读取 registrar 的静态缓存字段。
    /// </summary>
    private static object GetRegistrarCacheField(string fieldName)
    {
        var field = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsHandlerRegistrar", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing registrar cache field {fieldName}.");
        return field!.GetValue(null)
               ?? throw new InvalidOperationException($"Registrar cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     通过反射读取 dispatcher 的静态缓存字段。
    /// </summary>
    private static object GetDispatcherCacheField(string fieldName)
    {
        var field = typeof(CqrsReflectionFallbackAttribute).Assembly
            .GetType("GFramework.Cqrs.Internal.CqrsDispatcher", throwOnError: true)!
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, $"Missing dispatcher cache field {fieldName}.");
        return field!.GetValue(null)
               ?? throw new InvalidOperationException($"Dispatcher cache field {fieldName} returned null.");
    }

    /// <summary>
    ///     清空目标缓存实例。
    /// </summary>
    private static void ClearCache(object cache)
    {
        _ = cache.GetType()
            .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .Invoke(cache, []);
    }

    /// <summary>
    ///     读取双键缓存中当前保存的对象。
    /// </summary>
    private static object? GetPairCacheValue(object cache, Type primaryType, Type secondaryType)
    {
        return InvokeInstanceMethod(cache, "GetValueOrDefaultForTesting", primaryType, secondaryType);
    }

    /// <summary>
    ///     读取指定 stream dispatch binding 中当前缓存的 pipeline executor。
    /// </summary>
    private static object? GetStreamPipelineExecutorValue(
        object streamBindings,
        Type requestType,
        Type responseType,
        int behaviorCount)
    {
        var binding = GetStreamDispatchBindingValue(streamBindings, requestType, responseType);
        return binding is null
            ? null
            : InvokeInstanceMethod(binding, "GetPipelineExecutorForTesting", behaviorCount);
    }

    /// <summary>
    ///     读取指定流式请求/响应类型对对应的强类型 stream dispatch binding。
    /// </summary>
    private static object? GetStreamDispatchBindingValue(object streamBindings, Type requestType, Type responseType)
    {
        var bindingBox = GetPairCacheValue(streamBindings, requestType, responseType);
        if (bindingBox is null)
        {
            return null;
        }

        var method = bindingBox.GetType().GetMethod(
            "Get",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing stream binding accessor on {bindingBox.GetType().FullName}.");

        return method!
            .MakeGenericMethod(responseType)
            .Invoke(bindingBox, []);
    }

    /// <summary>
    ///     调用目标对象上的实例方法。
    /// </summary>
    private static object? InvokeInstanceMethod(object target, string methodName, params object[] arguments)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing method {target.GetType().FullName}.{methodName}.");

        return method!.Invoke(target, arguments);
    }

    /// <summary>
    ///     枚举并收集当前异步流中的全部元素，便于断言 generated stream invoker 的输出。
    /// </summary>
    /// <typeparam name="TItem">流元素类型。</typeparam>
    /// <param name="stream">待消耗的异步流。</param>
    /// <returns>按产出顺序收集得到的元素列表。</returns>
    private static async Task<IReadOnlyList<TItem>> DrainAsync<TItem>(IAsyncEnumerable<TItem> stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var items = new List<TItem>();
        await foreach (var item in stream.ConfigureAwait(false))
        {
            items.Add(item);
        }

        return items;
    }
}
