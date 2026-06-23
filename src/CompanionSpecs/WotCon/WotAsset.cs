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
    /// Parsed form of <see cref="ThingDescription"/> populated by <c>CloseAndUpdate</c>
    /// once the upload validates. Consumed by the Variable / Method materialization
    /// steps in later plan items.
    /// </summary>
    public ThingDescriptionInfo ParsedThingDescription { get; set; }

    /// <summary>
    /// Asset endpoint URI derived from the TD's top-level <c>base</c>. Surfaced on the
    /// asset's <c>AssetEndpoint</c> Property (<c>IWoTAssetType</c>, OPC 10100-1 §6.3.8)
    /// and enumerated by <c>DiscoverAssets</c> (§6.3.4). <c>null</c> when the TD omits
    /// <c>base</c>; in that case the Property is not materialized and the asset is not
    /// listed by <c>DiscoverAssets</c>.
    /// </summary>
    public string AssetEndpoint { get; set; }

    /// <summary>
    /// NodeId of the materialized <c>AssetEndpoint</c> Property when one is present.
    /// Tracked so re-uploads can drop the previous generation before a new one is
    /// created.
    /// </summary>
    public NodeId AssetEndpointNodeId { get; set; }

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

    /// <summary>
    /// TD-property-name → materialized OPC UA Variable NodeId. Populated by the
    /// MaterializeAssetProperties pass so re-uploads can drop the previous generation
    /// of nodes before writing the new one.
    /// </summary>
    public Dictionary<string, NodeId> MaterializedPropertyNodeIds { get; } = new();

    /// <summary>
    /// TD-action-name → materialized OPC UA Method NodeId. Populated by the
    /// MaterializeAssetActions pass so re-uploads can drop the previous generation
    /// of methods before writing the new one.
    /// </summary>
    public Dictionary<string, NodeId> MaterializedActionNodeIds { get; } = new();
}
