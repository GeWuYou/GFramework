using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Utility;
using GFramework.Core.Architectures;
using GFramework.Core.Logging;
using GFramework.Cqrs.Abstractions.Cqrs;

namespace GFramework.Core.Tests.Architectures;

/// <summary>
///     验证 Architecture 通过 <c>ArchitectureModules</c> 暴露出的模块安装与 CQRS 行为注册能力。
///     这些测试覆盖模块安装回调和请求管道行为接入，确保模块管理器仍然保持可观察行为不变。
/// </summary>
[TestFixture]
public class ArchitectureModulesBehaviorTests
{
    /// <summary>
    ///     初始化日志工厂和全局上下文状态。
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        LoggerFactoryResolver.Provider = new ConsoleLoggerFactoryProvider();
        GameContext.Clear();
        TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount = 0;
    }

    /// <summary>
    ///     清理测试过程中写入的全局上下文状态。
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        GameContext.Clear();
        TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount = 0;
    }

    /// <summary>
    ///     验证安装模块时会把当前架构实例传给模块，并允许模块在安装阶段注册组件。
    /// </summary>
    [Test]
    public async Task InstallModule_Should_Invoke_Module_Install_With_Current_Architecture()
    {
        var module = new TrackingArchitectureModule();
        var architecture = new ModuleTestArchitecture(target => target.InstallModule(module));

        await architecture.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(module.InstalledArchitecture, Is.SameAs(architecture));
            Assert.That(module.InstallCallCount, Is.EqualTo(1));
            Assert.That(architecture.Context.GetUtility<InstalledByModuleUtility>(), Is.Not.Null);
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证注册的 CQRS 行为会参与请求管道执行。
    /// </summary>
    [Test]
    public async Task RegisterCqrsPipelineBehavior_Should_Apply_Pipeline_Behavior_To_Request()
    {
        var architecture = new ModuleTestArchitecture(target =>
            target.RegisterCqrsPipelineBehavior<TrackingPipelineBehavior<ModuleBehaviorRequest, string>>());

        await architecture.InitializeAsync();

        var response = await architecture.Context.SendRequestAsync(new ModuleBehaviorRequest());

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.EqualTo("handled"));
            Assert.That(TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount, Is.EqualTo(1));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     验证兼容别名 <c>RegisterMediatorBehavior</c> 仍会把 CQRS 行为接入请求管道。
    /// </summary>
    [Test]
    public async Task RegisterMediatorBehavior_Should_Apply_Pipeline_Behavior_To_Request()
    {
        var architecture = new ModuleTestArchitecture(target =>
            target.RegisterMediatorBehavior<TrackingPipelineBehavior<ModuleBehaviorRequest, string>>());

        await architecture.InitializeAsync();

        var response = await architecture.Context.SendRequestAsync(new ModuleBehaviorRequest());

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.EqualTo("handled"));
            Assert.That(TrackingPipelineBehavior<ModuleBehaviorRequest, string>.InvocationCount, Is.EqualTo(1));
        });

        await architecture.DestroyAsync();
    }

    /// <summary>
    ///     用于测试模块行为的最小架构实现。
    /// </summary>
    private sealed class ModuleTestArchitecture(Action<ModuleTestArchitecture> registrationAction) : Architecture
    {
        /// <summary>
        ///     在初始化阶段执行测试注入的模块注册逻辑。
        /// </summary>
        protected override void OnInitialize()
        {
            registrationAction(this);
        }
    }

    /// <summary>
    ///     记录模块安装调用情况的测试模块。
    /// </summary>
    private sealed class TrackingArchitectureModule : IArchitectureModule
    {
        /// <summary>
        ///     获取模块安装调用次数。
        /// </summary>
        public int InstallCallCount { get; private set; }

        /// <summary>
        ///     获取最近一次接收到的架构实例。
        /// </summary>
        public IArchitecture? InstalledArchitecture { get; private set; }

        /// <summary>
        ///     记录安装调用，并在安装阶段注册一个工具验证调用链可用。
        /// </summary>
        /// <param name="architecture">目标架构实例。</param>
        public void Install(IArchitecture architecture)
        {
            InstallCallCount++;
            InstalledArchitecture = architecture;
            architecture.RegisterUtility(new InstalledByModuleUtility());
        }
    }

    /// <summary>
    ///     由测试模块安装时注册的简单工具。
    /// </summary>
    private sealed class InstalledByModuleUtility : IUtility
    {
    }
}

/// <summary>
///     用于验证管道行为注册是否生效的测试请求。
/// </summary>
public sealed class ModuleBehaviorRequest : IRequest<string>
{
}

/// <summary>
///     处理测试请求的处理器。
/// </summary>
public sealed class ModuleBehaviorRequestHandler : IRequestHandler<ModuleBehaviorRequest, string>
{
    /// <summary>
    ///     返回固定结果，便于聚焦验证管道行为是否执行。
    /// </summary>
    /// <param name="request">请求实例。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>固定响应内容。</returns>
    public ValueTask<string> Handle(ModuleBehaviorRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult("handled");
    }
}

/// <summary>
///     记录请求通过管道次数的测试行为。
/// </summary>
/// <typeparam name="TRequest">请求类型。</typeparam>
/// <typeparam name="TResponse">响应类型。</typeparam>
public sealed class TrackingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     获取当前测试进程中该请求类型对应的行为触发次数。
    /// </summary>
    public static int InvocationCount { get; set; }

    /// <summary>
    ///     记录一次行为执行，然后继续执行下一个处理器。
    /// </summary>
    /// <param name="message">当前请求消息。</param>
    /// <param name="next">下一个处理委托。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>下游处理器的响应结果。</returns>
    public async ValueTask<TResponse> Handle(
        TRequest message, MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        InvocationCount++;
        return await next(message, cancellationToken);
    }
}
