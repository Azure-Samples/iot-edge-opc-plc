# WoT-Con (OPC 10100-1) for OPC PLC

A mock-only, server-side implementation of the **OPC UA Web of Things Connectivity
(WoT-Con) companion specification** ([OPC 10100-1 v1.02](https://reference.opcfoundation.org/specs/OPC-10100-1/v1.02/6.3)).
It exposes a `WoTAssetConnectionManagement` entry point on the OPC PLC server,
accepts W3C [Thing Description](https://www.w3.org/TR/wot-thing-description11/)
JSON-LD uploads over the File API, and materializes each TD's properties /
actions as OPC UA Variables / Methods linked by `HasWoTComponent`.

This folder is **mock-only**: there is no real southbound protocol binding —
materialized Variables carry seeded values from
[`WotMockValueGenerator`](./WotMockValueGenerator.cs) and action handlers return
canned outputs. Real Modbus / HTTP / OPC DA / etc. translation is out of scope;
the goal is to give consumers (Commander, dashboards, integration tests) a
spec-compliant address space to drive against.

The companion spec is gated behind the `--wotcon` CLI flag and is **off by
default**.

## Spec mapping

| OPC 10100-1 §            | Member                                  | Status              | Source                                                                                                                  | Tests                                                                                            |
| ------------------------ | --------------------------------------- | ------------------- | ----------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| §6.3.1                   | `WoTAssetConnectionManagement` (i=31)   | Implemented         | [`WotConNodeManager.cs`](./WotConNodeManager.cs)                                                                        | [`WotConTests.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.cs)                          |
| §6.3.1 `SupportedWoTBindings` | Property (i=37, Optional)          | Implemented         | [`WotConNodeManager.OptionalMembers.cs`](./WotConNodeManager.OptionalMembers.cs), [`WotConBindings.cs`](./WotConBindings.cs) | [`WotConTests.OptionalMembers.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.OptionalMembers.cs) |
| §6.3.2                   | `CreateAsset` (i=32 type / i=34 inst.)  | Implemented         | [`WotConNodeManager.cs`](./WotConNodeManager.cs)                                                                        | [`WotConTests.CreateAsset.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.CreateAsset.cs)  |
| §6.3.3                   | `DeleteAsset` (i=29 type / i=35 inst.)  | Implemented         | [`WotConNodeManager.cs`](./WotConNodeManager.cs)                                                                        | [`WotConTests.DeleteAsset.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.DeleteAsset.cs)  |
| §6.3.4                   | `DiscoverAssets`                        | Implemented (TD `base`-driven) | [`WotConNodeManager.OptionalMembers.cs`](./WotConNodeManager.OptionalMembers.cs)                            | [`WotConTests.DiscoverAssets.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.DiscoverAssets.cs) |
| §6.3.5                   | `CreateAssetForEndpoint`                | Implemented         | [`WotConNodeManager.OptionalMembers.cs`](./WotConNodeManager.OptionalMembers.cs)                                         | [`WotConTests.OptionalMembers.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.OptionalMembers.cs) |
| §6.3.6                   | `ConnectionTest`                        | Implemented (mock)  | [`WotConNodeManager.OptionalMembers.cs`](./WotConNodeManager.OptionalMembers.cs)                                         | [`WotConTests.OptionalMembers.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.OptionalMembers.cs) |
| §6.3.7                   | `Configuration` / `License`             | Implemented (SPDX `MIT`) | [`WotConNodeManager.OptionalMembers.cs`](./WotConNodeManager.OptionalMembers.cs)                                    | [`WotConTests.OptionalMembers.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.OptionalMembers.cs) |
| §6.3.8                   | `IWoTAssetType` (i=42) + Property → Variable mapping | Implemented (primitives) | [`WotConNodeManager.cs`](./WotConNodeManager.cs), [`ThingDescriptionParser.cs`](./ThingDescriptionParser.cs)  | [`WotConTests.PropertyMaterialization.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.PropertyMaterialization.cs) |
| §6.3.9                   | Action → Method mapping                 | Implemented (mock handlers) | [`WotConNodeManager.cs`](./WotConNodeManager.cs), [`ThingDescriptionParser.cs`](./ThingDescriptionParser.cs)         | [`WotConTests.ActionMaterialization.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.ActionMaterialization.cs) |
| §6.3.10                  | Per-asset `WoTAssetFileType` + `CloseAndUpdate` | Implemented | [`WotConNodeManager.cs`](./WotConNodeManager.cs)                                                                        | [`WotConTests.CloseAndUpdate.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.CloseAndUpdate.cs), [`WotConTests.WoTFile.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.WoTFile.cs) |
| §6.3.11                  | `HasWoTComponent` reference type        | Implemented         | [`Opc.Ua.WotCon.NodeSet2.xml`](./Opc.Ua.WotCon.NodeSet2.xml), [`WotConNodeManager.cs`](./WotConNodeManager.cs)           | [`WotConTests.HasWoTComponent.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.HasWoTComponent.cs) |

## Files

### `src/CompanionSpecs/WotCon/`

| File                                                                              | Role                                                                                                                                                                                                                                              |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`Opc.Ua.WotCon.NodeSet2.xml`](./Opc.Ua.WotCon.NodeSet2.xml)                      | Bundled WoT-Con NodeSet2 declaring `WoTAssetConnectionManagementType`, `IWoTAssetType`, `WoTAssetFileType`, `CloseAndUpdate`, `HasWoTComponent`, and the management `WoTAssetConnectionManagement` instance. Loaded via `LoadPredefinedNodes`.     |
| [`WotConNodeManager.cs`](./WotConNodeManager.cs)                                  | Main `CustomNodeManager2` partial: NodeSet load, `Call` override (NodeId remap for type-method calls), `CreateAsset` / `DeleteAsset` handlers, per-asset `WoTAssetFileType` wiring, `CloseAndUpdate` handler, property / action materialization. |
| [`WotConNodeManager.OptionalMembers.cs`](./WotConNodeManager.OptionalMembers.cs)  | Materializes the optional members of `WoTAssetConnectionManagementType` that the NodeSet importer drops (modelling rule `Optional`): `SupportedWoTBindings`, `Configuration` / `License`, `DiscoverAssets`, `CreateAssetForEndpoint`, `ConnectionTest`. Also hosts `ValidateThingDescriptionBindings`. |
| [`ThingDescriptionParser.cs`](./ThingDescriptionParser.cs)                        | Pure JSON-LD parser. Extracts TD `title`, `base`, `@context`, properties, and actions; maps JSON Schema primitives to OPC UA built-in types and value ranks.                                                                                       |
| [`WotConBindings.cs`](./WotConBindings.cs)                                        | Catalog of WoT protocol bindings the server understands. `SupportedBindings` is surfaced on `SupportedWoTBindings`; `KnownBindings` lists W3C binding URIs the validator recognises as binding declarations.                                       |
| [`WotMockValueGenerator.cs`](./WotMockValueGenerator.cs)                          | Seeds initial values for materialized Variables. Static fixed values today; the per-tick simulation engine is deferred (see [Deferred](#deferred--pending-commander-support)).                                                                      |
| [`WotAsset.cs`](./WotAsset.cs)                                                    | Internal runtime model for one managed asset: NodeIds, parsed TD, type-method → instance-method remap table, open file handles, materialized property / action / endpoint NodeIds.                                                                |

### `tests/CompanionSpecs/WotCon/`

| File                                                                                                                                | Coverage                                                                                                              |
| ----------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| [`WotConTests.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.cs)                                                             | Test fixture base — server setup, namespace constants, helper accessors.                                              |
| [`WotConTests.Helpers.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.Helpers.cs)                                             | Shared call / browse / file helpers (`CallAsync`, `CreateAssetAndResolveFileAsync`, `TranslateBrowsePathsToNodeIdsAsync`). |
| [`WotConTests.Discovery.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.Discovery.cs)                                         | NodeSet load + namespace registration + management object browseability.                                              |
| [`WotConTests.CreateAsset.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.CreateAsset.cs)                                     | §6.3.2 — return AssetId, browseable, duplicate-name rejection, `IWoTAssetType` interface, per-asset file isolation.   |
| [`WotConTests.DeleteAsset.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.DeleteAsset.cs)                                     | §6.3.3 — removes subtree + organizes ref, `Bad_NotFound` on unknown.                                                  |
| [`WotConTests.CloseAndUpdate.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.CloseAndUpdate.cs)                               | §6.3.10 File API round-trip, TD parsing gate, binding validation, re-upload semantics.                                |
| [`WotConTests.WoTFile.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.WoTFile.cs)                                             | Address-space audit of the per-asset `WoTAssetFileType` (full `FileType` layout + `CloseAndUpdate`).                  |
| [`WotConTests.PropertyMaterialization.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.PropertyMaterialization.cs)             | §6.3.8 — TD properties → Variables, `readOnly` / `writeOnly`, `unit` → `EngineeringUnits`, re-upload replaces.        |
| [`WotConTests.ActionMaterialization.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.ActionMaterialization.cs)                 | §6.3.9 — TD actions → Methods, input/output `Argument` synthesis, mock invocation, re-upload replaces.                |
| [`WotConTests.HasWoTComponent.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.HasWoTComponent.cs)                             | §6.3.11 — materialized properties/actions on `HasWoTComponent`; per-asset `WoTFile` stays on plain `HasComponent`.    |
| [`WotConTests.DiscoverAssets.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.DiscoverAssets.cs)                               | §6.3.4 — endpoint surface sourced from TD `base`, dedup, `AssetEndpoint` Property.                                    |
| [`WotConTests.OptionalMembers.cs`](../../../tests/CompanionSpecs/WotCon/WotConTests.OptionalMembers.cs)                             | §6.3.1 / §6.3.5 / §6.3.6 / §6.3.7 — `SupportedWoTBindings`, `CreateAssetForEndpoint`, `ConnectionTest`, `Configuration / License`. |

## Architecture

```
WoTAssetConnectionManagement   (ns=WotCon;i=31)   <-- standard instance, browseable entry point
  ├─ HasComponent → CreateAsset                  (i=34, instance)        [§6.3.2]
  ├─ HasComponent → DeleteAsset                  (i=35, instance)        [§6.3.3]
  ├─ HasComponent → SupportedWoTBindings         (runtime NodeId)        [§6.3.1, Optional, materialized]
  ├─ HasComponent → DiscoverAssets               (runtime NodeId)        [§6.3.4, Optional, materialized]
  ├─ HasComponent → CreateAssetForEndpoint       (runtime NodeId)        [§6.3.5, Optional, materialized]
  ├─ HasComponent → ConnectionTest               (runtime NodeId)        [§6.3.6, Optional, materialized]
  ├─ HasComponent → Configuration                (runtime NodeId)        [§6.3.7, Optional, materialized]
  │                   └─ HasProperty → License   ("MIT", SPDX)
  └─ Organizes    → <asset> : BaseObjectType     (runtime NodeId, per CreateAsset)
                         ├─ HasInterface     → IWoTAssetType             (ns=WotCon;i=42)         [§6.3.8]
                         ├─ HasComponent     → WoTFile : WoTAssetFileType (runtime, per-asset)    [§6.3.10]
                         │                       ├─ HasProperty  → Size, Writable, UserWritable,
                         │                       │                 OpenCount, MimeType,
                         │                       │                 MaxByteStringLength,
                         │                       │                 LastModifiedTime              (OPC 10000-5 §10)
                         │                       ├─ HasComponent → Open, Close, Read, Write,
                         │                       │                 GetPosition, SetPosition       (NS=0)
                         │                       └─ HasComponent → CloseAndUpdate                 (ns=WotCon)
                         ├─ HasComponent     → AssetEndpoint (String, optional, from TD `base`)
                         ├─ HasWoTComponent  → <TD property> : BaseDataVariableType (one per)     [§6.3.8 / §6.3.11]
                         └─ HasWoTComponent  → <TD action>   : MethodState         (one per)      [§6.3.9 / §6.3.11]
```

**Method-call routing.** Clients may invoke methods either on the materialized
instance NodeIds or on the type-side `<MethodId>` they discovered via NodeSet
import. The `Call` override remaps every known type-method NodeId
(`CreateAsset` i=26, `DeleteAsset` i=27, optional methods i=41 / 49 / 75,
`WoTAssetFileType` inherited methods, and `CloseAndUpdate` i=111) onto the
runtime-allocated instance NodeId for the target object, so both call shapes
work.

## Design choices worth remembering

- **Per-asset `WoTAssetFileType` instance, not a singleton.** Each
  `CreateAsset` mints a fresh `FileState` as a `HasComponent` child of the
  asset, so concurrent uploads to different assets don't share a file handle
  table. The previous singleton-`WoTFile` design was retired.
- **NodeSet-importer workaround for `Argument` arrays.** The SDK's NodeSet
  importer (1.5.378) drops `InputArguments` / `OutputArguments` from
  `MethodState`s when they're declared via `HasProperty` on the type side.
  We rehydrate them onto the strongly-typed `MethodState.InputArguments` at
  startup and remap type-method NodeIds onto instance NodeIds in the `Call`
  override.
- **TD validation gate today = "well-formed JSON + non-empty `title`".** Full
  JSON Schema validation against [Annex A.2](https://www.w3.org/TR/wot-thing-description11/#json-schema-validation)
  is deferred. The gate is sufficient for mock-mode round-trips and avoids
  over-engineering ahead of a real consumer. Binding compatibility is
  additionally checked against `WotConBindings.KnownBindings` /
  `SupportedBindings`.
- **Re-upload semantics.** `CloseAndUpdate` on an existing asset drops the
  previous generation of materialized property / action / endpoint nodes via
  `DeleteNode` before creating the new generation. The asset's visible
  address space always reflects the most recently uploaded TD.
- **Strict duplicate-name policy on `CreateAsset`.** Per §6.3.2, the second
  call with the same `AssetName` returns `Bad_BrowseNameDuplicated`
  (`0x806B0000`). Clients that want re-create semantics call `DeleteAsset`
  first.
- **`HasWoTComponent` vs plain `HasComponent`.** Materialized TD properties
  and actions link from the asset via `HasWoTComponent` (a `HasComponent`
  subtype declared in the NodeSet, §6.3.11). The per-asset `WoTFile` and
  inherited `CreateFile` stay on plain `HasComponent` — they're OPC 10000-5
  `FileType` plumbing, not WoT affordances.
- **Binding validator walks `@context`, not `Forms`.** §6.3.1 talks about TD
  **Forms** carrying binding URIs; the current validator instead checks
  entries in the TD's top-level `@context` against
  `WotConBindings.KnownBindings` / `SupportedBindings`. That keeps the test
  surface simple and works for TDs that declare their binding via
  `@context`, but it is a layering shortcut — when a TD with a per-Form
  binding (e.g. a Modbus Form using the W3C Modbus binding) shows up, the
  validator will need to walk Forms too. Rejection uses `Bad_NotSupported`
  rather than `Bad_InvalidArgument`: the request is well-formed, the
  simulator categorically just doesn't speak Modbus / HTTP / MQTT / etc.

## Running it

The companion spec is **off by default**. Enable it via `--wotcon`:

```powershell
dotnet run --project src/opc-plc.csproj -- --pn=50000 --autoaccept --wotcon
```

In the Helm chart [`microsoft-opc-plc`](https://msazure.visualstudio.com/One/_git/iotcat?path=/aio-connectors-tooling/distrib/helm/microsoft-opc-plc),
the `--wotcon` flag is passed to the `opcplc-opc-plc-wot-000000` deployment
which the E2E suite drives.

Once running, browse the entry point at:

| NodeId                  | Member                          |
| ----------------------- | ------------------------------- |
| `ns=<WotCon>;i=31`      | `WoTAssetConnectionManagement`  |
| `ns=<WotCon>;i=34`      | `CreateAsset` (instance method) |
| `ns=<WotCon>;i=35`      | `DeleteAsset` (instance method) |

`<WotCon>` is the namespace index for `http://opcfoundation.org/UA/WoT-Con/`,
allocated at startup by the SDK.

## Links

- [OPC 10100-1 v1.02 — WoT Connectivity](https://reference.opcfoundation.org/specs/OPC-10100-1/v1.02/6.3)
- [W3C WoT Thing Description 1.1](https://www.w3.org/TR/wot-thing-description11/)
- [W3C WoT Binding Templates](https://www.w3.org/TR/wot-binding-templates/)
- [OPC 10000-5 `FileType` (§10)](https://reference.opcfoundation.org/Core/Part5/v105/docs/10) — the base type for `WoTAssetFileType`
- [`microsoft-opc-plc` Helm chart](https://msazure.visualstudio.com/One/_git/iotcat?path=/aio-connectors-tooling/distrib/helm/microsoft-opc-plc)

## Deferred — pending Commander support

These items are deliberately out of scope for the current pass. They will be
picked up once OPC UA Commander exercises them end-to-end; until then the
current behaviour (JSON-as-String fallback for nested schemas; anonymous
access for `CreateAsset` / `DeleteAsset`; static seed values) is sufficient
for mock-mode round-trips and avoids over-engineering ahead of a real
consumer.

### Mock simulation engine for materialized Variables

A per-tick updater (hooked into the existing `TimeService` / `PlcSimulation`
loop) that mutates materialized Variable values so OPC UA subscriptions see
changes:

- Numeric → sine / ramp / random walk (configurable per property via a TD
  `oc:simulation` extension, optional).
- Boolean → toggle on interval.
- String → rotate through a small fixed list.
- Respect `oc:simulation.period` if present; default 1 s.

Deferred because constants (seeded once at materialization via
`WotMockValueGenerator`) are sufficient to prove the read / browse / subscribe
plumbing end-to-end. Adding live drift means plumbing `TimeService` into
`WotConNodeManager`, synchronising timer mutations against
`CreateAsset` / `DeleteAsset` / `CloseAndUpdate` re-materialization, and a
threading model nobody is asking for yet. When this lands, also add a
"subscription on a materialized Variable fires within 2 simulator ticks"
test — it was scoped out of the address-space audit pass because there is no
value drift to subscribe to today.

### Complex / structured types (stretch)

§6.3.8 Table 14 covers primitives only in v1.02. For TDs that use nested objects:

- Generate a Structured DataType in the WotCon namespace from the JSON Schema
  and expose it via `DataTypeDictionary` so clients can decode it.
- Fall back to JSON-as-String when generation fails (already the current
  behaviour).

This is the largest task and not required for spec conformance — implement
only once Commander uploads TDs exercising it.

### Authorization stubs

Spec lists `Bad_UserAccessDenied` for `CreateAsset` / `DeleteAsset`. Add a
RolePermissions hook (default: allow `Anonymous`) so the deny path is at
least reachable from tests. Defer until Commander has a story for
authenticated asset onboarding.

## Explicitly out of scope

- **Real southbound protocol bindings** — no Modbus, HTTP, OPC DA, S7, etc.
  All values stay simulated.
- **WoT Event affordances** — v1.02 does not define a mapping; revisit when a
  future revision of OPC 10100-1 ships one.
- **OPC 10000-9 Alarms & Conditions for WoT** — same reason.
- **TD authoring / TDD federation** — clients upload finished TDs; we don't
  host a Thing Description Directory.
- **Cross-asset relationships** (e.g. ISA-95 hierarchies). Each asset is an
  independent root under `WoTAssetConnectionManagement`.
