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

using GFramework.Game.Abstractions.Routing;
using GFramework.Game.Routing;

namespace GFramework.Game.Tests.Routing;

/// <summary>
/// RouterBase 单元测试
/// </summary>
[TestFixture]
public class RouterBaseTests
{
    /// <summary>
    /// 测试用路由项
    /// </summary>
    private class TestRoute : IRoute
    {
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// 测试用路由上下文
    /// </summary>
    private class TestContext : IRouteContext
    {
        public string? Data { get; set; }
    }

    /// <summary>
    /// 测试用路由守卫
    /// </summary>
    private class TestGuard : IRouteGuard<TestRoute>
    {
        public Func<string, IRouteContext?, ValueTask<bool>>? EnterFunc { get; set; }
        public Func<string, ValueTask<bool>>? LeaveFunc { get; set; }
        public int Priority { get; set; }
        public bool CanInterrupt { get; set; }

        public ValueTask<bool> CanEnterAsync(string routeKey, IRouteContext? context)
        {
            return EnterFunc?.Invoke(routeKey, context) ?? ValueTask.FromResult(true);
        }

        public ValueTask<bool> CanLeaveAsync(string routeKey)
        {
            return LeaveFunc?.Invoke(routeKey) ?? ValueTask.FromResult(true);
        }
    }

    /// <summary>
    /// 测试用路由器实现
    /// </summary>
    private class TestRouter : RouterBase<TestRoute, TestContext>
    {
        public bool HandlersRegistered { get; private set; }

        // 暴露 Stack 用于测试
        public new Stack<TestRoute> Stack => base.Stack;

        protected override void OnInit()
        {
            // 测试用路由器不需要初始化逻辑
        }

        protected override void RegisterHandlers()
        {
            HandlersRegistered = true;
        }

        // 暴露 protected 方法用于测试
        public new Task<bool> ExecuteEnterGuardsAsync(string routeKey, TestContext? context)
        {
            return base.ExecuteEnterGuardsAsync(routeKey, context);
        }

        public new Task<bool> ExecuteLeaveGuardsAsync(string routeKey)
        {
            return base.ExecuteLeaveGuardsAsync(routeKey);
        }
    }

    [Test]
    public void AddGuard_ShouldAddGuardToList()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard { Priority = 10 };

        // Act
        router.AddGuard(guard);

        // Assert - 通过尝试添加相同守卫来验证
        Assert.DoesNotThrow(() => router.AddGuard(guard));
    }

    [Test]
    public void AddGuard_ShouldSortByPriority()
    {
        // Arrange
        var router = new TestRouter();
        var guard1 = new TestGuard { Priority = 20 };
        var guard2 = new TestGuard { Priority = 10 };
        var guard3 = new TestGuard { Priority = 30 };

        // Act
        router.AddGuard(guard1);
        router.AddGuard(guard2);
        router.AddGuard(guard3);

        // Assert - 通过执行守卫来验证顺序
        var executionOrder = new List<int>();
        guard1.EnterFunc = (_, _) =>
        {
            executionOrder.Add(1);
            return ValueTask.FromResult(true);
        };
        guard2.EnterFunc = (_, _) =>
        {
            executionOrder.Add(2);
            return ValueTask.FromResult(true);
        };
        guard3.EnterFunc = (_, _) =>
        {
            executionOrder.Add(3);
            return ValueTask.FromResult(true);
        };

        router.ExecuteEnterGuardsAsync("test", null).Wait();

        Assert.That(executionOrder, Is.EqualTo(new[] { 2, 1, 3 }));
    }

    [Test]
    public void AddGuard_WithGeneric_ShouldCreateAndAddGuard()
    {
        // Arrange
        var router = new TestRouter();

        // Act & Assert
        Assert.DoesNotThrow(() => router.AddGuard<TestGuard>());
    }

    [Test]
    public void AddGuard_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var router = new TestRouter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => router.AddGuard(null!));
    }

    [Test]
    public void RemoveGuard_ShouldRemoveGuardFromList()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard { Priority = 10 };
        router.AddGuard(guard);

        // Act
        router.RemoveGuard(guard);

        // Assert - 守卫应该被移除,不会再执行
        var executed = false;
        guard.EnterFunc = (_, _) =>
        {
            executed = true;
            return ValueTask.FromResult(true);
        };

        router.ExecuteEnterGuardsAsync("test", null).Wait();

        Assert.That(executed, Is.False);
    }

    [Test]
    public void RemoveGuard_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var router = new TestRouter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => router.RemoveGuard(null!));
    }

    [Test]
    public async Task ExecuteEnterGuardsAsync_WithNoGuards_ShouldReturnTrue()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = await router.ExecuteEnterGuardsAsync("test", null);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExecuteEnterGuardsAsync_WithAllowingGuard_ShouldReturnTrue()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard
        {
            Priority = 10,
            EnterFunc = (_, _) => ValueTask.FromResult(true)
        };
        router.AddGuard(guard);

        // Act
        var result = await router.ExecuteEnterGuardsAsync("test", null);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExecuteEnterGuardsAsync_WithBlockingGuard_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard
        {
            Priority = 10,
            EnterFunc = (_, _) => ValueTask.FromResult(false)
        };
        router.AddGuard(guard);

        // Act
        var result = await router.ExecuteEnterGuardsAsync("test", null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ExecuteEnterGuardsAsync_WithInterruptingGuard_ShouldStopExecution()
    {
        // Arrange
        var router = new TestRouter();
        var guard1 = new TestGuard
        {
            Priority = 10,
            CanInterrupt = true,
            EnterFunc = (_, _) => ValueTask.FromResult(true)
        };
        var guard2Executed = false;
        var guard2 = new TestGuard
        {
            Priority = 20,
            EnterFunc = (_, _) =>
            {
                guard2Executed = true;
                return ValueTask.FromResult(true);
            }
        };
        router.AddGuard(guard1);
        router.AddGuard(guard2);

        // Act
        var result = await router.ExecuteEnterGuardsAsync("test", null);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(guard2Executed, Is.False);
    }

    [Test]
    public async Task ExecuteEnterGuardsAsync_WithThrowingGuard_ShouldContinueIfNotInterrupting()
    {
        // Arrange
        var router = new TestRouter();
        var guard1 = new TestGuard
        {
            Priority = 10,
            CanInterrupt = false,
            EnterFunc = (_, _) => throw new InvalidOperationException("Test exception")
        };
        var guard2Executed = false;
        var guard2 = new TestGuard
        {
            Priority = 20,
            EnterFunc = (_, _) =>
            {
                guard2Executed = true;
                return ValueTask.FromResult(true);
            }
        };
        router.AddGuard(guard1);
        router.AddGuard(guard2);

        // Act
        var result = await router.ExecuteEnterGuardsAsync("test", null);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(guard2Executed, Is.True);
    }

    [Test]
    public async Task ExecuteEnterGuardsAsync_WithThrowingInterruptingGuard_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard
        {
            Priority = 10,
            CanInterrupt = true,
            EnterFunc = (_, _) => throw new InvalidOperationException("Test exception")
        };
        router.AddGuard(guard);

        // Act
        var result = await router.ExecuteEnterGuardsAsync("test", null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ExecuteLeaveGuardsAsync_WithNoGuards_ShouldReturnTrue()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = await router.ExecuteLeaveGuardsAsync("test");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExecuteLeaveGuardsAsync_WithAllowingGuard_ShouldReturnTrue()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard
        {
            Priority = 10,
            LeaveFunc = _ => ValueTask.FromResult(true)
        };
        router.AddGuard(guard);

        // Act
        var result = await router.ExecuteLeaveGuardsAsync("test");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExecuteLeaveGuardsAsync_WithBlockingGuard_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();
        var guard = new TestGuard
        {
            Priority = 10,
            LeaveFunc = _ => ValueTask.FromResult(false)
        };
        router.AddGuard(guard);

        // Act
        var result = await router.ExecuteLeaveGuardsAsync("test");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_WithEmptyStack_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = router.Contains("test");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_WithMatchingRoute_ShouldReturnTrue()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test" };
        router.Stack.Push(route);

        // Act
        var result = router.Contains("test");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_WithNonMatchingRoute_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test1" };
        router.Stack.Push(route);

        // Act
        var result = router.Contains("test2");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void PeekKey_WithEmptyStack_ShouldReturnEmptyString()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = router.PeekKey();

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void PeekKey_WithRoute_ShouldReturnRouteKey()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test" };
        router.Stack.Push(route);

        // Act
        var result = router.PeekKey();

        // Assert
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void IsTop_WithEmptyStack_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = router.IsTop("test");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsTop_WithMatchingRoute_ShouldReturnTrue()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test" };
        router.Stack.Push(route);

        // Act
        var result = router.IsTop("test");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsTop_WithNonMatchingRoute_ShouldReturnFalse()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test1" };
        router.Stack.Push(route);

        // Act
        var result = router.IsTop("test2");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Current_WithEmptyStack_ShouldReturnNull()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = router.Current;

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Current_WithRoute_ShouldReturnTopRoute()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test" };
        router.Stack.Push(route);

        // Act
        var result = router.Current;

        // Assert
        Assert.That(result, Is.EqualTo(route));
    }

    [Test]
    public void CurrentKey_WithEmptyStack_ShouldReturnNull()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = router.CurrentKey;

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void CurrentKey_WithRoute_ShouldReturnRouteKey()
    {
        // Arrange
        var router = new TestRouter();
        var route = new TestRoute { Key = "test" };
        router.Stack.Push(route);

        // Act
        var result = router.CurrentKey;

        // Assert
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void Count_WithEmptyStack_ShouldReturnZero()
    {
        // Arrange
        var router = new TestRouter();

        // Act
        var result = router.Count;

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Count_WithRoutes_ShouldReturnCorrectCount()
    {
        // Arrange
        var router = new TestRouter();
        router.Stack.Push(new TestRoute { Key = "test1" });
        router.Stack.Push(new TestRoute { Key = "test2" });

        // Act
        var result = router.Count;

        // Assert
        Assert.That(result, Is.EqualTo(2));
    }
}