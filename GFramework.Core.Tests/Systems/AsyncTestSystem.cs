// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

using GFramework.Core.Abstractions.Architectures;
using GFramework.Core.Abstractions.Enums;
using GFramework.Core.Abstractions.Lifecycle;
using GFramework.Core.Abstractions.Systems;

namespace GFramework.Core.Tests.Systems;

/// <summary>
///     异步测试系统，实现 ISystem 和 IAsyncInitializable
/// </summary>
public sealed class AsyncTestSystem : ISystem, IAsyncInitializable
{
    private IArchitectureContext _context = null!;
    public bool Initialized { get; private set; }
    public bool DestroyCalled { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Delay(10).ConfigureAwait(false);
        Initialized = true;
    }

    public void SetContext(IArchitectureContext context)
    {
        _context = context;
    }

    public IArchitectureContext GetContext()
    {
        return _context;
    }

    public void Initialize()
    {
        // 同步 OnInitialize 不应该被调用
        throw new InvalidOperationException("Sync OnInitialize should not be called");
    }

    public void Destroy()
    {
        DestroyCalled = true;
    }

    public void OnArchitecturePhase(ArchitecturePhase phase)
    {
    }
}
