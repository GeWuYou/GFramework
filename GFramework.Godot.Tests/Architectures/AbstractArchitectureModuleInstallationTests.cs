using GFramework.Core.Abstractions.Architectures;
using GFramework.Godot.Architectures;

namespace GFramework.Godot.Tests.Architectures;

/// <summary>
///     验证 Godot 架构在模块安装前会先检查锚点状态，避免未绑定场景树时留下半安装副作用。
/// </summary>
[TestFixture]
public sealed class AbstractArchitectureModuleInstallationTests
{
    /// <summary>
    ///     验证当锚点尚未初始化时，安装流程会直接失败，且不会执行模块安装逻辑。
    /// </summary>
    /// <returns>表示异步断言完成的任务。</returns>
    [Test]
    public async Task InstallGodotModuleAsync_ShouldThrowBeforeInvokingModuleInstall_WhenAnchorIsMissing()
    {
        var architecture = new TestArchitecture();
        var module = new RecordingGodotModule();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            architecture.InstallGodotModuleForTestAsync(module));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Is.EqualTo("Anchor not initialized"));
            Assert.That(module.InstallCalled, Is.False);
        });
    }

    private sealed class TestArchitecture : AbstractArchitecture
    {
        protected override void InstallModules()
        {
        }

        public Task InstallGodotModuleForTestAsync(RecordingGodotModule module)
        {
            return InstallGodotModule(module);
        }
    }

    private sealed class RecordingGodotModule : IGodotModule
    {
        public bool InstallCalled { get; private set; }

        public global::Godot.Node Node => null!;

        public void Install(IArchitecture architecture)
        {
            InstallCalled = true;
        }

        public void OnAttach(GFramework.Core.Architectures.Architecture architecture)
        {
        }

        public void OnDetach()
        {
        }
    }
}
