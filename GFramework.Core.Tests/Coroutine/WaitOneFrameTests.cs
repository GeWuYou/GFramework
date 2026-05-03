// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Coroutine.Instructions;
using NUnit.Framework;

namespace GFramework.Core.Tests.Coroutine
{
    [TestFixture]
    public class WaitOneFrameTests
    {
        [Test]
        public void Constructor_CreatesInstance()
        {
            // Act
            var waitOneFrame = new WaitOneFrame();

            // Assert
            Assert.That(waitOneFrame.IsDone, Is.False);
        }

        [Test]
        public void Update_MakesItDone()
        {
            // Arrange
            var waitOneFrame = new WaitOneFrame();

            // Act
            waitOneFrame.Update(0.1);

            // Assert
            Assert.That(waitOneFrame.IsDone, Is.True);
        }

        [Test]
        public void Update_MultipleTimes_RemainsDone()
        {
            // Arrange
            var waitOneFrame = new WaitOneFrame();

            // Act
            waitOneFrame.Update(0.1);
            waitOneFrame.Update(0.1);

            // Assert
            Assert.That(waitOneFrame.IsDone, Is.True);
        }
    }
}