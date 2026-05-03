// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Cqrs.Abstractions.Cqrs;
using GFramework.Cqrs.Abstractions.Cqrs.Command;
using GFramework.Cqrs.Abstractions.Cqrs.Notification;
using GFramework.Cqrs.Abstractions.Cqrs.Query;
using GFramework.Cqrs.Abstractions.Cqrs.Request;
using GFramework.Cqrs.Command;
using GFramework.Cqrs.Notification;
using GFramework.Cqrs.Query;
using GFramework.Cqrs.Request;

namespace GFramework.Core.Tests.Cqrs;

/// <summary>
///     锁定 CQRS 基础消息类型在 runtime 拆分后的公开命名空间与程序集兼容性。
/// </summary>
[TestFixture]
public sealed class CqrsPublicNamespaceCompatibilityTests
{
    /// <summary>
    ///     验证基础消息类型继续暴露在历史公开 CQRS 命名空间（GFramework.Cqrs.*），同时由独立 runtime 程序集承载实现。
    /// </summary>
    [Test]
    public void Base_Message_Types_Should_Live_In_Cqrs_Namespaces_And_Runtime_Assembly()
    {
        Assert.Multiple(() =>
        {
            AssertLegacyType(typeof(CommandBase<TestCommandInput, Unit>), "GFramework.Cqrs.Command");
            AssertLegacyType(typeof(QueryBase<TestQueryInput, string>), "GFramework.Cqrs.Query");
            AssertLegacyType(typeof(RequestBase<TestRequestInput, string>), "GFramework.Cqrs.Request");
            AssertLegacyType(typeof(NotificationBase<TestNotificationInput>), "GFramework.Cqrs.Notification");
        });
    }

    /// <summary>
    ///     验证旧的 GFramework.Core 程序集限定名仍可解析到迁移后的 runtime 实现类型。
    /// </summary>
    [Test]
    public void Type_Forwarding_Should_Resolve_Cqrs_Types_From_Core_Assembly()
    {
        Assert.Multiple(() =>
        {
            AssertForwardedType("GFramework.Cqrs.Command.CommandBase`2, GFramework.Core");
            AssertForwardedType("GFramework.Cqrs.Query.QueryBase`2, GFramework.Core");
            AssertForwardedType("GFramework.Cqrs.Request.RequestBase`2, GFramework.Core");
            AssertForwardedType("GFramework.Cqrs.Notification.NotificationBase`1, GFramework.Core");
        });
    }

    private static void AssertLegacyType(Type type, string expectedNamespace)
    {
        Assert.Multiple(() =>
        {
            Assert.That(type.Namespace, Is.EqualTo(expectedNamespace));
            Assert.That(type.Assembly.GetName().Name, Is.EqualTo("GFramework.Cqrs"));
        });
    }

    private static void AssertForwardedType(string assemblyQualifiedTypeName)
    {
        var resolvedType = Type.GetType(assemblyQualifiedTypeName, throwOnError: false);

        Assert.Multiple(() =>
        {
            Assert.That(resolvedType, Is.Not.Null);
            Assert.That(resolvedType!.Assembly.GetName().Name, Is.EqualTo("GFramework.Cqrs"));
        });
    }

    private sealed record TestCommandInput : ICommandInput;

    private sealed record TestQueryInput : IQueryInput;

    private sealed record TestRequestInput : IRequestInput;

    private sealed record TestNotificationInput : INotificationInput;
}
