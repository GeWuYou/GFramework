using GFramework.Core.Abstractions.Pause;
using GFramework.Core.Pause;

namespace GFramework.Core.Tests.Pause;

/// <summary>
/// 暂停栈管理器单元测试
/// </summary>
[TestFixture]
public class PauseStackManagerTests
{
    /// <summary>
    /// 在每个测试方法执行前设置测试环境
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _manager = new PauseStackManager();
    }

    /// <summary>
    /// 在每个测试方法执行后清理资源
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _manager.DestroyAsync();
    }

    private PauseStackManager _manager = null!;

    /// <summary>
    /// 验证Push方法返回有效的令牌
    /// </summary>
    [Test]
    public void Push_Should_ReturnValidToken()
    {
        var token = _manager.Push("Test pause");

        Assert.That(token.IsValid, Is.True);
        Assert.That(token.Id, Is.Not.EqualTo(Guid.Empty));
    }

    /// <summary>
    /// 验证Push方法设置暂停状态
    /// </summary>
    [Test]
    public void Push_Should_SetPausedState()
    {
        Assert.That(_manager.IsPaused(), Is.False);

        _manager.Push("Test pause");

        Assert.That(_manager.IsPaused(), Is.True);
    }

    /// <summary>
    /// 验证Pop方法清除暂停状态
    /// </summary>
    [Test]
    public void Pop_Should_ClearPausedState()
    {
        var token = _manager.Push("Test pause");
        Assert.That(_manager.IsPaused(), Is.True);

        _manager.Pop(token);

        Assert.That(_manager.IsPaused(), Is.False);
    }

    /// <summary>
    /// 验证Pop无效令牌返回false
    /// </summary>
    [Test]
    public void Pop_WithInvalidToken_Should_ReturnFalse()
    {
        var result = _manager.Pop(PauseToken.Invalid);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// 验证Pop过期令牌返回false
    /// </summary>
    [Test]
    public void Pop_WithExpiredToken_Should_ReturnFalse()
    {
        var token = _manager.Push("Test pause");
        _manager.Pop(token);

        // 尝试再次 Pop 同一个 Token
        var result = _manager.Pop(token);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// 验证多次Push增加深度
    /// </summary>
    [Test]
    public void MultiplePush_Should_IncreaseDepth()
    {
        _manager.Push("First");
        _manager.Push("Second");
        _manager.Push("Third");

        Assert.That(_manager.GetPauseDepth(), Is.EqualTo(3));
    }

    /// <summary>
    /// 验证嵌套暂停需要所有Pop才能恢复
    /// </summary>
    [Test]
    public void NestedPause_Should_RequireAllPops()
    {
        var token1 = _manager.Push("First");
        var token2 = _manager.Push("Second");

        _manager.Pop(token1);
        Assert.That(_manager.IsPaused(), Is.True, "Should still be paused after first pop");

        _manager.Pop(token2);
        Assert.That(_manager.IsPaused(), Is.False, "Should be unpaused after all pops");
    }

    /// <summary>
    /// 验证Pop非栈顶令牌可以正常工作
    /// </summary>
    [Test]
    public void Pop_WithNonTopToken_Should_Work()
    {
        var token1 = _manager.Push("First");
        var token2 = _manager.Push("Second");
        var token3 = _manager.Push("Third");

        // Pop 中间的 token
        var result = _manager.Pop(token2);

        Assert.That(result, Is.True);
        Assert.That(_manager.GetPauseDepth(), Is.EqualTo(2));
        Assert.That(_manager.IsPaused(), Is.True);

        // 验证剩余的令牌仍然有效
        Assert.That(_manager.Pop(token1), Is.True);
        Assert.That(_manager.Pop(token3), Is.True);
        Assert.That(_manager.IsPaused(), Is.False);
    }

    /// <summary>
    /// 验证不同组独立工作
    /// </summary>
    [Test]
    public void DifferentGroups_Should_BeIndependent()
    {
        _manager.Push("Global pause", PauseGroup.Global);
        _manager.Push("Gameplay pause", PauseGroup.Gameplay);

        Assert.That(_manager.IsPaused(PauseGroup.Global), Is.True);
        Assert.That(_manager.IsPaused(PauseGroup.Gameplay), Is.True);
        Assert.That(_manager.IsPaused(PauseGroup.Audio), Is.False);
    }

    /// <summary>
    /// 验证Pop只影响正确的组
    /// </summary>
    [Test]
    public void Pop_Should_OnlyAffectCorrectGroup()
    {
        var globalToken = _manager.Push("Global");
        var gameplayToken = _manager.Push("Gameplay", PauseGroup.Gameplay);

        _manager.Pop(globalToken);

        Assert.That(_manager.IsPaused(), Is.False);
        Assert.That(_manager.IsPaused(PauseGroup.Gameplay), Is.True);

        // 验证 gameplayToken 仍然有效并且可以被正常弹出
        Assert.That(_manager.Pop(gameplayToken), Is.True);
        Assert.That(_manager.IsPaused(PauseGroup.Gameplay), Is.False);
    }


    /// <summary>
    /// 验证GetPauseReasons返回所有原因
    /// </summary>
    [Test]
    public void GetPauseReasons_Should_ReturnAllReasons()
    {
        _manager.Push("Menu opened");
        _manager.Push("Dialog shown");
        _manager.Push("Inventory opened");

        var reasons = _manager.GetPauseReasons();

        Assert.That(reasons.Count, Is.EqualTo(3));
        Assert.That(reasons, Does.Contain("Menu opened"));
        Assert.That(reasons, Does.Contain("Dialog shown"));
        Assert.That(reasons, Does.Contain("Inventory opened"));
    }

    /// <summary>
    /// 验证空栈GetPauseReasons返回空列表
    /// </summary>
    [Test]
    public void GetPauseReasons_WithEmptyStack_Should_ReturnEmptyList()
    {
        var reasons = _manager.GetPauseReasons();

        Assert.That(reasons, Is.Empty);
    }

    /// <summary>
    /// 验证Push触发状态变化事件
    /// </summary>
    [Test]
    public void Push_Should_TriggerEventWhenStateChanges()
    {
        bool eventTriggered = false;
        PauseGroup? eventGroup = null;
        bool? eventIsPaused = null;

        _manager.OnPauseStateChanged += (_, e) =>
        {
            eventTriggered = true;
            eventGroup = e.Group;
            eventIsPaused = e.IsPaused;
        };

        _manager.Push("Test", PauseGroup.Gameplay);

        Assert.That(eventTriggered, Is.True);
        Assert.That(eventGroup, Is.EqualTo(PauseGroup.Gameplay));
        Assert.That(eventIsPaused, Is.True);
    }

    /// <summary>
    /// 验证Pop在栈变空时触发事件
    /// </summary>
    [Test]
    public void Pop_Should_TriggerEventWhenStackBecomesEmpty()
    {
        var token = _manager.Push("Test");

        bool eventTriggered = false;
        _manager.OnPauseStateChanged += (_, e) =>
        {
            eventTriggered = true;
            Assert.That(e.IsPaused, Is.False);
        };

        _manager.Pop(token);
        Assert.That(eventTriggered, Is.True);
    }

    /// <summary>
    /// 验证多次Push只触发一次事件
    /// </summary>
    [Test]
    public void MultiplePush_Should_OnlyTriggerEventOnce()
    {
        int eventCount = 0;
        _manager.OnPauseStateChanged += (_, _) => eventCount++;

        _manager.Push("First");
        _manager.Push("Second");

        Assert.That(eventCount, Is.EqualTo(1));
    }

    /// <summary>
    /// 验证处理器在状态变化时被通知
    /// </summary>
    [Test]
    public void Handler_Should_BeNotifiedOnStateChange()
    {
        var mockHandler = new MockPauseHandler();
        _manager.RegisterHandler(mockHandler);

        _manager.Push("Test", PauseGroup.Global);

        Assert.That(mockHandler.CallCount, Is.EqualTo(1));
        Assert.That(mockHandler.LastGroup, Is.EqualTo(PauseGroup.Global));
        Assert.That(mockHandler.LastIsPaused, Is.True);
    }

    /// <summary>
    /// 验证处理器在恢复时被通知
    /// </summary>
    [Test]
    public void Handler_Should_BeNotifiedOnResume()
    {
        var mockHandler = new MockPauseHandler();
        _manager.RegisterHandler(mockHandler);

        var token = _manager.Push("Test");
        mockHandler.Reset();

        _manager.Pop(token);

        Assert.That(mockHandler.CallCount, Is.EqualTo(1));
        Assert.That(mockHandler.LastIsPaused, Is.False);
    }

    /// <summary>
    /// 验证多个处理器按优先级顺序调用
    /// </summary>
    [Test]
    public void MultipleHandlers_Should_BeCalledInPriorityOrder()
    {
        var calls = new List<int>();

        var handler1 = new MockPauseHandler { Priority = 10 };
        var handler2 = new MockPauseHandler { Priority = 5 };
        var handler3 = new MockPauseHandler { Priority = 15 };

        handler1.OnCall = () => calls.Add(1);
        handler2.OnCall = () => calls.Add(2);
        handler3.OnCall = () => calls.Add(3);

        _manager.RegisterHandler(handler1);
        _manager.RegisterHandler(handler2);
        _manager.RegisterHandler(handler3);

        _manager.Push("Test");

        Assert.That(calls, Is.EqualTo(new[] { 2, 1, 3 }));
    }

    /// <summary>
    /// 验证PauseScope在Dispose时自动恢复
    /// </summary>
    [Test]
    public void PauseScope_Should_AutoResumeOnDispose()
    {
        using (_manager.PauseScope("Test"))
        {
            Assert.That(_manager.IsPaused(), Is.True);
        }

        Assert.That(_manager.IsPaused(), Is.False);
    }

    /// <summary>
    /// 验证嵌套PauseScope正常工作
    /// </summary>
    [Test]
    public void NestedPauseScope_Should_Work()
    {
        using (_manager.PauseScope("Outer"))
        {
            Assert.That(_manager.GetPauseDepth(), Is.EqualTo(1));

            using (_manager.PauseScope("Inner"))
            {
                Assert.That(_manager.GetPauseDepth(), Is.EqualTo(2));
            }

            Assert.That(_manager.GetPauseDepth(), Is.EqualTo(1));
        }

        Assert.That(_manager.GetPauseDepth(), Is.EqualTo(0));
    }

    /// <summary>
    /// 验证ClearGroup移除指定组的所有暂停
    /// </summary>
    [Test]
    public void ClearGroup_Should_RemoveAllPausesForGroup()
    {
        _manager.Push("First", PauseGroup.Gameplay);
        _manager.Push("Second", PauseGroup.Gameplay);
        _manager.Push("Third", PauseGroup.Audio);

        _manager.ClearGroup(PauseGroup.Gameplay);

        Assert.That(_manager.IsPaused(PauseGroup.Gameplay), Is.False);
        Assert.That(_manager.IsPaused(PauseGroup.Audio), Is.True);
    }

    /// <summary>
    /// 验证ClearAll移除所有暂停
    /// </summary>
    [Test]
    public void ClearAll_Should_RemoveAllPauses()
    {
        _manager.Push("First", PauseGroup.Global);
        _manager.Push("Second", PauseGroup.Gameplay);
        _manager.Push("Third", PauseGroup.Audio);

        _manager.ClearAll();

        Assert.That(_manager.IsPaused(PauseGroup.Global), Is.False);
        Assert.That(_manager.IsPaused(PauseGroup.Gameplay), Is.False);
        Assert.That(_manager.IsPaused(PauseGroup.Audio), Is.False);
    }

    /// <summary>
    /// 验证销毁时会向所有仍暂停的组补发恢复通知。
    /// </summary>
    [Test]
    public async Task DestroyAsync_Should_NotifyResumedGroups()
    {
        var resumedGroups = new List<PauseGroup>();
        var mockHandler = new MockPauseHandler();

        _manager.RegisterHandler(mockHandler);
        _manager.OnPauseStateChanged += (_, e) =>
        {
            if (!e.IsPaused)
            {
                resumedGroups.Add(e.Group);
            }
        };

        _manager.Push("Global", PauseGroup.Global);
        _manager.Push("Gameplay", PauseGroup.Gameplay);
        mockHandler.Reset();

        await _manager.DestroyAsync();

        Assert.That(mockHandler.CallCount, Is.EqualTo(2));
        Assert.That(mockHandler.LastIsPaused, Is.False);
        Assert.That(resumedGroups, Is.EquivalentTo(new[] { PauseGroup.Global, PauseGroup.Gameplay }));
    }

    /// <summary>
    /// 验证并发Push是线程安全的
    /// </summary>
    [Test]
    public void ConcurrentPush_Should_BeThreadSafe()
    {
        var tasks = new List<Task>();
        var tokens = new List<PauseToken>();
        var lockObj = new object();

        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var token = _manager.Push($"Pause {index}");
                lock (lockObj)
                {
                    tokens.Add(token);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.That(_manager.GetPauseDepth(), Is.EqualTo(100));
        Assert.That(tokens.Count, Is.EqualTo(100));
    }

    /// <summary>
    /// 验证并发Pop是线程安全的
    /// </summary>
    [Test]
    public void ConcurrentPop_Should_BeThreadSafe()
    {
        var tokens = new List<PauseToken>();
        for (int i = 0; i < 100; i++)
        {
            tokens.Add(_manager.Push($"Pause {i}"));
        }

        var tasks = tokens.Select(token => Task.Run(() => _manager.Pop(token))).ToList();

        Task.WaitAll(tasks.ToArray());

        Assert.That(_manager.GetPauseDepth(), Is.EqualTo(0));
        Assert.That(_manager.IsPaused(), Is.False);
    }

    /// <summary>
    /// 测试用的暂停处理器实现
    /// </summary>
    private class MockPauseHandler : IPauseHandler
    {
        public int CallCount { get; private set; }
        public PauseGroup? LastGroup { get; private set; }
        public bool? LastIsPaused { get; private set; }
        public Action? OnCall { get; set; }
        public int Priority { get; set; } = 0;

        public void OnPauseStateChanged(PauseGroup group, bool isPaused)
        {
            CallCount++;
            LastGroup = group;
            LastIsPaused = isPaused;
            OnCall?.Invoke();
        }

        public void Reset()
        {
            CallCount = 0;
            LastGroup = null;
            LastIsPaused = null;
        }
    }
}
