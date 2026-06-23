// Copyright (c) OPC Foundation and contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace OpcPlc.CompanionSpecs.WotCon;

using Opc.Ua;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Internal model for a managed WoT-Con asset. Holds the per-asset address-space NodeIds,
/// the type-method → instance-method remap table the <c>Call</c> override consults, and the
/// open File API buffers.
/// </summary>
internal sealed class WotAsset
{
    public string Name { get; set; }

    public NodeId AssetId { get; set; }

    public string ThingDescription { get; set; }

    /// <summary>
    /// Per-asset WoTAssetFileType instance NodeId (HasComponent child of the asset).
    /// </summary>
    public NodeId FileNodeId { get; set; }

    /// <summary>
    /// Type-method NodeId → per-asset instance-method NodeId. Used by the Call override
    /// to rewrite NS=0 FileType_Open/Close/Read/Write/GetPosition/SetPosition and the
    /// WoT-Con CloseAndUpdate type-method (ns=WotCon;i=111) onto this asset's instance
    /// methods.
    /// </summary>
    public Dictionary<NodeId, NodeId> FileMethodMap { get; } = new();

    /// <summary>
    /// Active upload buffers keyed by file handle returned from Open.
    /// </summary>
    public Dictionary<uint, MemoryStream> FileBuffers { get; } = new();

    public object FileLock { get; } = new();

    public uint NextFileHandle { get; set; } = 1;

    /// <summary>
    /// Most recent payload finalized via CloseAndUpdate. Kept for diagnostics and to
    /// support TD materialization later in the plan.
    /// </summary>
    public byte[] LastFinalizedPayload { get; set; }
}
