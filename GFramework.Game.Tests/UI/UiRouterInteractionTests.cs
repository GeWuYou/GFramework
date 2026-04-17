// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace GFramework.Game.Tests.UI;

/// <summary>
///     验证 UI 路由输入语义、层级排序与显示恢复生命周期的回归测试。
/// </summary>
[TestFixture]
public class UiRouterInteractionTests
{
    /// <summary>
    ///     验证模态层和顶层共享同一套阻塞型默认交互配置。
    /// </summary>
    [Test]
    public void CreateDefault_ForModalAndTopmost_ReturnsBlockingCancelProfile()
    {
        // Arrange
        var modal = UiInteractionProfiles.CreateDefault(UiLayer.Modal);
        var topmost = UiInteractionProfiles.CreateDefault(UiLayer.Topmost);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(modal.CapturedActions, Is.EqualTo(UiInputActionMask.Cancel));
            Assert.That(modal.BlocksWorldPointerInput, Is.True);
            Assert.That(modal.BlocksWorldActionInput, Is.True);
            Assert.That(topmost.CapturedActions, Is.EqualTo(UiInputActionMask.Cancel));
            Assert.That(topmost.BlocksWorldPointerInput, Is.True);
            Assert.That(topmost.BlocksWorldActionInput, Is.True);
        });
    }

    /// <summary>
    ///     验证只要动作被页面捕获，路由分发就会返回成功，即使页面没有显式消费该动作。
    /// </summary>
    [Test]
    public void TryDispatchUiAction_WhenCapturedButUnhandled_ReturnsTrue()
    {
        // Arrange
        var router = CreateRouter();
        var page = new TestUiPage("capturing-page", UiLayer.Topmost)
        {
            InteractionProfile = new UiInteractionProfile
            {
                CapturedActions = UiInputActionMask.Cancel
            },
            TryHandleUiActionResult = false
        };

        router.Show(page, UiLayer.Topmost);

        // Act
        var dispatched = router.TryDispatchUiAction(UiInputAction.Cancel);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dispatched, Is.True);
            Assert.That(page.TryHandleUiActionCallCount, Is.EqualTo(1));
        });
    }

    /// <summary>
    ///     验证层级页面排序使用实例自增序号，而不是依赖固定宽度的字符串顺序。
    /// </summary>
    [Test]
    public void GetUiActionOwner_WhenInstanceIdWidthOverflows_UsesNumericOrder()
    {
        // Arrange
        var router = CreateRouter();
        SetInstanceCounter(router, 999998);

        var olderPage = new TestUiPage("older", UiLayer.Topmost)
        {
            InteractionProfile = new UiInteractionProfile
            {
                CapturedActions = UiInputActionMask.Cancel
            }
        };
        var newerPage = new TestUiPage("newer", UiLayer.Topmost)
        {
            InteractionProfile = new UiInteractionProfile
            {
                CapturedActions = UiInputActionMask.Cancel
            }
        };

        router.Show(olderPage, UiLayer.Topmost);
        router.Show(newerPage, UiLayer.Topmost);

        // Act
        var owner = router.GetUiActionOwner(UiInputAction.Cancel);

        // Assert
        Assert.That(owner, Is.SameAs(newerPage));
    }

    /// <summary>
    ///     验证恢复挂起的层级页面时，不会再对依赖 OnShow 触发恢复的页面重复调用 OnResume。
    /// </summary>
    [Test]
    public void Resume_WhenPageResumesDuringShow_DoesNotCallResumeTwice()
    {
        // Arrange
        var router = CreateRouter();
        var page = new TestUiPage("resumable-layer-page", UiLayer.Overlay)
        {
            ResumeFromShow = true
        };

        var handle = router.Show(page, UiLayer.Overlay);
        router.Hide(handle, UiLayer.Overlay);
        var resumeCountBeforeResume = page.OnResumeCallCount;

        // Act
        router.Resume(handle, UiLayer.Overlay);

        // Assert
        Assert.That(page.OnResumeCallCount, Is.EqualTo(resumeCountBeforeResume + 1));
    }

    /// <summary>
    ///     验证弹出栈顶页面后，恢复下层页面时不会重复触发恢复逻辑。
    /// </summary>
    /// <returns>表示异步测试执行过程的任务。</returns>
    [Test]
    public async Task PopAsync_WhenPageResumesDuringShow_DoesNotCallResumeTwice()
    {
        // Arrange
        var router = CreateRouter();
        var underlyingPage = new TestUiPage("underlying-page", UiLayer.Page)
        {
            ResumeFromShow = true
        };
        var topPage = new TestUiPage("top-page", UiLayer.Page);

        await router.PushAsync(underlyingPage);
        await router.PushAsync(topPage);
        var resumeCountBeforePop = underlyingPage.OnResumeCallCount;

        // Act
        await router.PopAsync(UiPopPolicy.Destroy);

        // Assert
        Assert.That(underlyingPage.OnResumeCallCount, Is.EqualTo(resumeCountBeforePop + 1));
    }

    /// <summary>
    ///     创建带有测试根节点的 UI 路由器。
    /// </summary>
    /// <returns>已绑定测试根节点的路由器实例。</returns>
    private static TestUiRouter CreateRouter()
    {
        var router = new TestUiRouter();
        router.BindRoot(new TestUiRoot());
        router.InitializeForTests();
        return router;
    }

    /// <summary>
    ///     把实例计数器调整到指定值，以便覆盖实例标识符宽度溢出的排序回归。
    /// </summary>
    /// <param name="router">目标路由器。</param>
    /// <param name="value">要写入的计数器值。</param>
    private static void SetInstanceCounter(UiRouterBase router, int value)
    {
        var field = typeof(UiRouterBase).GetField("_instanceCounter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "UiRouterBase._instanceCounter 字段未找到，可能发生了内部重构。");
        Assert.That(field!.FieldType, Is.EqualTo(typeof(int)), "_instanceCounter 字段类型已变化，请同步调整测试。");

        field.SetValue(router, value);
    }

    /// <summary>
    ///     测试用 UI 路由器实现。
    /// </summary>
    private sealed class TestUiRouter : UiRouterBase
    {
        /// <summary>
        ///     以测试专用的最小依赖集合执行路由器初始化。
        /// </summary>
        public void InitializeForTests()
        {
            Initialize();
        }

        /// <summary>
        ///     以测试最小依赖完成初始化，避免把测试绑定到完整的架构 Utility 配置上。
        /// </summary>
        protected override void OnInit()
        {
            RegisterHandlers();
        }

        /// <summary>
        ///     注册处理器。
        /// </summary>
        protected override void RegisterHandlers()
        {
        }
    }

    /// <summary>
    ///     测试用 UI 根节点，占位记录添加/移除操作即可。
    /// </summary>
    private sealed class TestUiRoot : IUiRoot
    {
        /// <summary>
        ///     记录当前挂载的页面集合。
        /// </summary>
        private readonly List<IUiPageBehavior> _children = new();

        /// <inheritdoc />
        public void AddUiPage(IUiPageBehavior child)
        {
            _children.Add(child);
        }

        /// <inheritdoc />
        public void AddUiPage(IUiPageBehavior child, UiLayer layer, int orderInLayer = 0)
        {
            _children.Add(child);
        }

        /// <inheritdoc />
        public void RemoveUiPage(IUiPageBehavior child)
        {
            _children.Remove(child);
        }
    }

    /// <summary>
    ///     可配置的测试页面，用于模拟路由器在不同交互语义下的可观察行为。
    /// </summary>
    private sealed class TestUiPage : IUiPageBehavior
    {
        /// <summary>
        ///     初始化测试页面实例。
        /// </summary>
        /// <param name="key">页面键。</param>
        /// <param name="layer">页面层级。</param>
        public TestUiPage(string key, UiLayer layer)
        {
            Key = key;
            Layer = layer;
            InteractionProfile = UiInteractionProfiles.Default;
            IsAlive = true;
        }

        /// <summary>
        ///     获取或设置一个值，指示 <see cref="OnShow" /> 是否要模拟 `CanvasItemUiPageBehaviorBase` 那样触发恢复逻辑。
        /// </summary>
        public bool ResumeFromShow { get; init; }

        /// <summary>
        ///     获取或设置页面处理动作时返回的结果。
        /// </summary>
        public bool TryHandleUiActionResult { get; init; } = true;

        /// <summary>
        ///     记录恢复回调触发次数。
        /// </summary>
        public int OnResumeCallCount { get; private set; }

        /// <summary>
        ///     记录动作处理方法调用次数。
        /// </summary>
        public int TryHandleUiActionCallCount { get; private set; }

        /// <inheritdoc />
        public UiHandle? Handle { get; set; }

        /// <inheritdoc />
        public UiLayer Layer { get; }

        /// <inheritdoc />
        public bool IsReentrant { get; init; } = true;

        /// <inheritdoc />
        public object View => this;

        /// <inheritdoc />
        public bool IsAlive { get; private set; }

        /// <inheritdoc />
        public bool IsVisible { get; private set; }

        /// <inheritdoc />
        public bool IsModal => Layer == UiLayer.Modal;

        /// <inheritdoc />
        public bool BlocksInput { get; init; }

        /// <inheritdoc />
        public UiInteractionProfile InteractionProfile { get; init; }

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public void OnEnter(IUiPageEnterParam? param)
        {
        }

        /// <inheritdoc />
        public void OnExit()
        {
            IsAlive = false;
            IsVisible = false;
        }

        /// <inheritdoc />
        public void OnPause()
        {
        }

        /// <inheritdoc />
        public void OnResume()
        {
            OnResumeCallCount++;
        }

        /// <inheritdoc />
        public void OnHide()
        {
            IsVisible = false;
        }

        /// <inheritdoc />
        public void OnShow()
        {
            IsVisible = true;

            // The Godot page behavior resumes from OnShow(), so the router must not call OnResume() again on top.
            if (ResumeFromShow)
            {
                OnResume();
            }
        }

        /// <inheritdoc />
        public bool TryHandleUiAction(UiInputAction action)
        {
            TryHandleUiActionCallCount++;
            return TryHandleUiActionResult;
        }
    }
}
