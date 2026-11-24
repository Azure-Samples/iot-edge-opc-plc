// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using System;

/// <summary>
/// Bit mask of the <c>mode</c> argument of the OPC 10000-20 §4.2.2 <c>FileType.Open</c> method.
/// Bits 4-7 are reserved and must be zero.
/// </summary>
[Flags]
internal enum FileOpenMode : byte
{
    None = 0,
    Read = 0x01,
    Write = 0x02,
    EraseExisting = 0x04,
    Append = 0x08,
}
