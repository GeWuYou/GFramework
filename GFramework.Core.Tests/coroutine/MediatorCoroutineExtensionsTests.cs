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

using GFramework.Core.Abstractions.architecture;
using GFramework.Core.Abstractions.coroutine;
using GFramework.Core.Abstractions.rule;
using GFramework.Core.coroutine.extensions;
using Mediator;
using Moq;
using NUnit.Framework;

namespace GFramework.Core.Tests.coroutine;

/// <summary>
///     MediatorCoroutineExtensions的单元测试类
///     测试Mediator模式与协程集成的扩展方法
///     注意：由于 Mediator 使用源生成器，本测试类主要验证接口和参数验证
/// </summary>
[TestFixture]
public class MediatorCoroutineExtensionsTests
{
    /// <summary>
    ///     测试用的简单命令类
    /// </summary>
    private class TestCommand : IRequest<Unit>
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    ///     测试用的简单事件类
    /// </summary>
    private class TestEvent
    {
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    ///     上下文感知基类的模拟实现
    /// </summary>
    private class TestContextAware : IContextAware
    {
        public readonly Mock<IArchitectureContext> _mockContext = new();

        public IArchitectureContext GetContext()
        {
            return _mockContext.Object;
        }

        public void SetContext(IArchitectureContext context)
        {
        }
    }

    /// <summary>
    ///     验证SendCommandCoroutine应该返回IEnumerator<IYieldInstruction>
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Return_IEnumerator_Of_YieldInstruction()
    {
        var command = new TestCommand { Data = "Test" };
        var contextAware = new TestContextAware();

        // 创建 mediator 模拟
        var mediatorMock = new Mock<IMediator>();
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IMediator>())
            .Returns(mediatorMock.Object);

        var coroutine = MediatorCoroutineExtensions.SendCommandCoroutine(contextAware, command);

        Assert.That(coroutine, Is.InstanceOf<IEnumerator<IYieldInstruction>>());
    }

    /// <summary>
    ///     验证SendCommandCoroutine应该在mediator为null时抛出NullReferenceException
    /// </summary>
    [Test]
    public void SendCommandCoroutine_Should_Throw_When_Mediator_Null()
    {
        var command = new TestCommand { Data = "Test" };
        var contextAware = new TestContextAware();

        // 设置上下文服务以返回null mediator
        contextAware._mockContext
            .Setup(ctx => ctx.GetService<IMediator>())
            .Returns((IMediator?)null);

        // 创建协程
        var coroutine = MediatorCoroutineExtensions.SendCommandCoroutine(contextAware, command);

        // 调用 MoveNext 时应该抛出 NullReferenceException
        Assert.Throws<NullReferenceException>(() => coroutine.MoveNext());
    }
}