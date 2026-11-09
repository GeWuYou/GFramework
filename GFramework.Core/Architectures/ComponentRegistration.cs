// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Core.Architectures;

/// <summary>References the one owning DI key; it never creates a second alias or resource owner.</summary>
/// <param name="Descriptor">The original owning binding; later replacement is rejected.</param>
/// <param name="Order">Startup order; equal values preserve registration order.</param>
internal sealed record ComponentRegistration(ServiceDescriptor Descriptor, int Order)
{
    /// <summary>The original canonical DI service key.</summary>
    internal Type ServiceType => Descriptor.ServiceType;
}
