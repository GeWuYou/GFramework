// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Architectures;

/// <summary>References the one owning DI key; it never creates a second alias or resource owner.</summary>
/// <param name="ServiceType">Canonical registered key.</param>
/// <param name="Order">Startup order; equal values preserve registration order.</param>
internal sealed record ComponentRegistration(Type ServiceType, int Order);
