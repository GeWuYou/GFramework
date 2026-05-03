// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Coroutine;
using GFramework.Core.Coroutine.Instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Coroutine
{
    [TestFixture]
    public class WaitForCoroutineTests
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            // Arrange
            var coroutine = CreateDummyCoroutine();

            // Act
            var waitForCoroutine = new WaitForCoroutine(coroutine);

            // Assert
            Assert.That(waitForCoroutine.IsDone, Is.False);
        }

        [Test]
        public void Update_DoesNotChangeState()
        {
            // Arrange
            var coroutine = CreateDummyCoroutine();
            var waitForCoroutine = new WaitForCoroutine(coroutine);

            // Act
            waitForCoroutine.Update(1.0);

            // Assert
            Assert.That(waitForCoroutine.IsDone, Is.False);
        }

        private static IEnumerator<IYieldInstruction> CreateDummyCoroutine()
        {
            return new List<IYieldInstruction>().GetEnumerator();
        }
    }
}