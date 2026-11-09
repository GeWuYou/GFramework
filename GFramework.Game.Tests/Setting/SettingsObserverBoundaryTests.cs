// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using GFramework.Game.Abstractions.Setting;
using GFramework.Game.Setting.Events;
using GFramework.Game.Setting;
using Microsoft.Extensions.Logging.Abstractions;

namespace GFramework.Game.Tests.Setting;

/// <summary>Separates required settings application failures from optional observation failures.</summary>
[TestFixture]
public sealed class SettingsObserverBoundaryTests
{
    /// <summary>Throwing observers never manufacture failed application results or stop independent applicators.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task ApplicationResultsReflectApplicatorsRatherThanObservers(bool failFirst)
    {
        var first = new Mock<IResetApplyAbleSettings>();
        var second = new Mock<IResetApplyAbleSettings>();
        var failure = new InvalidOperationException("applicator failed");
        first.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>()))
            .Returns(failFirst ? Task.FromException(failure) : Task.CompletedTask);
        second.Setup(value => value.ApplyAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var model = new Mock<ISettingsModel>();
        model.Setup(value => value.AllApplicators()).Returns([first.Object, second.Object]);
        var results = new List<bool>();
        Action<object> publisher = _ => throw new InvalidOperationException("observer failed");
        publisher += value =>
        {
            if (value is SettingsAppliedEvent<ISettingsSection> result)
                results.Add(result.Success);
        };
        var system = new SettingsSystem(model.Object, publisher, observerLogger: NullLogger.Instance);
        if (failFirst)
        {
            var error = await Assert.ThrowsAsync<AggregateException>(() => system.ApplyAllAsync());
            Assert.That(error!.InnerException, Is.SameAs(failure));
        }
        else
        {
            await system.ApplyAllAsync();
        }
        bool[] expected = [!failFirst, true];
        Assert.That(results, Is.EqualTo(expected));
        second.Verify(value => value.ApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
