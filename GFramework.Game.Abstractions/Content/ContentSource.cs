// Copyright (c) 2025-2026 GeWuYou
// SPDX-License-Identifier: Apache-2.0

namespace GFramework.Game.Abstractions.Content;

/// <summary>Identifies one operation or field contribution without relying on exception message parsing.</summary>
/// <param name="PackId">Contributing package.</param>
/// <param name="PackVersion">Its declared version.</param>
/// <param name="FilePath">Operation source file.</param>
/// <param name="OperationIndex">Zero-based index within the file.</param>
public sealed record ContentSource(string PackId, string PackVersion, string FilePath, int OperationIndex);
