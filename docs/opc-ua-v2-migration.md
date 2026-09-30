# OPC UA 2.0 migration

Status as of **2026-09-30**: OPC PLC **2.16.0** uses exact **2.0.0-preview.6** SDK packages.
Package-mode Release and Debug builds pass locally. The full Release suite passes **902 tests,
0 failed, 0 skipped**. Hosted CI and current container qualification remain outstanding.

## Constraints

- Preserve CLI behavior, application identity, namespace URIs and NodeIds, simulations, authentication,
  certificate persistence, reverse connect, and deployment modes. Document intentional behavior changes.
- Use native SDK APIs, not compatibility shims. The SDK-facing C# surface changes, including plugin
  registration and value types; this is not a binary-compatible replacement for the 1.5 SDK.
- Change SDK source only after a minimal reproduction demonstrates a stack bug. Record expected and
  actual behavior and a regression test before proposing an upstream fix.
- Keep identity, encoding, method-signature, security, and throughput assertions strict. Update only
  specifically verified model-baseline differences; do not suppress failures or dependency auditing.
- Keep credentials, PKI, caches, and generated validation artifacts out of source control.

## Build and test

Use the .NET 10 SDK and run commands from the repository root. Nerdbank.GitVersioning needs full Git
history. [Directory.Packages.props](../Directory.Packages.props) pins all seven direct OPC runtime
and analyzer packages; the validated dependency graphs contain only preview-6 OPC packages.

Preview 6 is public on nuget.org and does not require a GitHub Packages token. This local workflow
selects the official feed for one restore without changing persistent NuGet settings:

```powershell
dotnet restore opcplc.sln --source https://api.nuget.org/v3/index.json `
  -p:Configuration=Release -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet build opcplc.sln -c Release --no-restore
dotnet build opcplc.sln -c Debug --no-restore
dotnet test tests/opc-plc-tests.csproj -c Release --no-build --no-restore
```

There is no root NuGet configuration. Ordinary build/test commands perform an implicit restore using
the environment's configured sources. Multiple inherited sources without source mapping can cause
`NU1507` under central package management. Use approved source mapping or a single permitted source,
not warning suppression. An Azure Artifacts feed or package proxy must expose the entire pinned SDK
set; successful nuget.org restore does not establish availability through another feed or satisfy
the hosted supply-chain policy. Do not disable that policy to make restore pass.

### Local SDK debugging

Package mode is the default. [Directory.Build.targets](../Directory.Build.targets) retains an explicit
source-mode switch for debugging a compatible SDK checkout; the default location is the sibling
`../UA-.NETStandard` directory. Runtime projects and the source generator must come from the same
checkout. To select another location:

```powershell
dotnet test tests/opc-plc-tests.csproj -c Release `
  -p:UseLocalOpcUaStack=true -p:OpcUaSourceRoot=C:\src\UA-.NETStandard `
  -p:GeneratePackageOnBuild=false -p:NuGetAudit=true -p:NuGetAuditMode=all
```

Source mode disables package generation and substitutes SDK project references. Restore and rebuild
when switching modes; do not reuse `--no-restore` results from a different SDK graph. A successful
source-mode run is not evidence that the selected NuGet packages work.

## Architecture

- Native async node managers await plugin `AddToAddressSpaceAsync` registration in order. The
  `OnAddressSpaceReady` hook runs after SDK runtime codecs are available.
- Production loads NodeSet2 XML and uses server-scoped runtime codecs. OPC boundaries use `Variant`,
  `ArrayOf<T>`, `ByteString`, and typed accessors. Simulation state and mocked time remain separate
  from transport and node registration.
- [RuntimeModelIds.cs](../src/RuntimeModelIds.cs) contains only the identifiers needed by runtime
  bindings. Tests compare them with generated reference models and retained NodeSet identities.
- Model generation is test-only. The [BoilerModel1 project](../models/BoilerModel1/BoilerModel1.csproj)
  uses authored ModelDesign XML and CSV to preserve its reference identifiers. It isolates Boiler1
  from Boiler2, which shares its model URI. Neither the project nor its assembly is a server runtime
  dependency. Retained generated source files remain available as test baselines, not compiled into
  the application.
- [PlcServerHost](../src/PlcServerHost.cs) and the configuration provider manage startup, background
  faults, cancellation, restart, and disposal. Authentication and certificate stores use supported
  injected providers and the SDK's certificate ownership contracts.
- The [application project](../src/opc-plc.csproj) declares ten runtime assets: eight NodeSet2 files,
  the stacklight HTML, and the node configuration JSON. The
  [package target](../src/Microsoft.IoTEdge.OpcPlc.targets) preserves their relative paths for consumer
  build and publish without repacking them. Test models, scripts, loose generated C#, and PKI are not
  runtime assets.

### Preview-6 compatibility

The [configuration factory](../src/Configuration/OpcUaAppConfigFactory.cs) sets
`MaxChannelCount = MaxSessionCount + 3`. Balanced resource isolation reserves one bootstrap and one
reconnect channel; the shared pool must still hold all sessions plus an ordinary reconnect channel.
Session counts must be between 1 and `int.MaxValue - 3`; invalid values fail before certificate-store
side effects. The SDK isolation policy remains enabled.

The model baseline no longer permits 29 identifiers to be absent when preview 6 exports them. Their
numeric and expanded NodeIds must match the retained CSVs. Tests also require exact retained method
child sets and signatures, rather than allowing synthetic empty argument properties. Negative tests
reject undeclared argument properties, including empty arrays and namespace-qualified names.

### Upstream fixes

All 12 resolved OPC packages identify source commit
[`d5092e9207816ba26aa18841032e148d19ace6a7`](https://github.com/OPCFoundation/UA-.NETStandard/commit/d5092e9207816ba26aa18841032e148d19ace6a7).
Its ancestry includes the fixes contributed during this migration:

| Merged PR | Behavior repaired |
| --- | --- |
| [#4544](https://github.com/OPCFoundation/UA-.NETStandard/pull/4544) | Mandatory node initialization and copied-child ownership |
| [#4546](https://github.com/OPCFoundation/UA-.NETStandard/pull/4546) | Scoped certificate-store resolution, key lifecycle, CSR, and trust-list consumers |
| [#4547](https://github.com/OPCFoundation/UA-.NETStandard/pull/4547) | Partial runtime type loading and imported structure metadata |
| [#4548](https://github.com/OPCFoundation/UA-.NETStandard/pull/4548) | Draining queued publish messages without an idle delay |
| [#4549](https://github.com/OPCFoundation/UA-.NETStandard/pull/4549) | Client transport connection-failure normalization |

No SDK source was modified to accommodate preview 6. Package ancestry establishes inclusion of the
merged fixes; the PLC regression suite supplies separate behavioral evidence.

## Validation

The following are local results for preview 6, not hosted pipeline results:

| Check | Verified result |
| --- | --- |
| Audited cold restore from nuget.org | Fresh external package/fallback directories; 7 application, 12 test, and 8 reference-model OPC dependencies, all exactly preview 6 |
| Release and Debug builds | All three projects build successfully |
| Capacity and live boiler checks | 15 passed |
| Generated model equivalence | 220 passed, including five identifier tables |
| Full Release suite after compatibility fixes | 902 passed, 0 failed, 0 skipped; reported duration 7m 2s |
| Simplified CI commands | Normal implicit-restore Release/Debug builds and 15 focused tests passed |
| Inspected OPC PLC NuGet archive | Ten runtime assets and exact preview-6 dependencies |

Focused tests overlap with the full suite and are not additive. Replacing 28 obsolete allowance
tests with eight stricter cases and adding eight capacity cases changed the suite count; no runtime
tests were skipped. The simplified CI commands were checked after the full-suite run; the full suite
was not repeated for that CI-only cleanup. An earlier canceled run is not counted as validation.

Local evidence is under the ignored `tests/TestResults/stack-v2-migration/` directory:

- `20260930-preview6-capacity-reserved.trx`
- `20260930-preview6-identifiers.trx`
- `20260930-preview6-model-compatibility.trx`
- `20260930-preview6-release-full.trx` and `.log`
- `20260930-simplified-ci-focused.trx`

Earlier SDK checkpoints passed independent 1.5.378.176-client interoperability and Linux/amd64
non-root image checks. Those results do not qualify the current preview-6 binaries or Docker changes.
Superseded experiments, earlier failure details, and older evidence paths remain in this document's
Git history rather than serving as current setup instructions.

## CI and containers

[CI](../tools/templates/ci.yml) uses normal build/test commands with implicit restore, explicitly
enables auditing of all dependencies, and retains warnings as errors and build-time package generation.
Full-history checkouts support versioning. The migration-only cold-restore wrapper and GitHub package
secret requirements have been removed; production CI must still pass its supply-chain checks.

The [Release Dockerfile](../Dockerfile.release) and [Debug Dockerfile](../Dockerfile.debug) restore
with matching architecture, configuration, self-contained, and runtime-patch settings before publishing
with `--no-restore`. They do not copy the test-only models or require a package-feed secret.
[.dockerignore](../.dockerignore) excludes host build outputs, test artifacts, and PKI.

```powershell
docker build -f Dockerfile.release -t opcplc-local:release .
docker build -f Dockerfile.debug -t opcplc-local:debug .
```

The [image pipeline](../tools/templates/acrbuild.yml) builds PR and non-release images without
publishing and checks for UID 1654. Only non-PR `main` and `release/*` branches enter the existing ACR
publishing path. Whether development branches should publish is a separate policy decision; this
migration's CI simplification did not expand publishing or change registry authentication.

## Remaining qualification

- [ ] Rerun hosted restore/build/test and image jobs with the simplified configuration and required
  feed policy. Build `183499378` failed before restore on the now-removed GitHub credential guard;
  its preparation, CodeQL, and CLA checks passed, but that is not a successful post-fix pipeline run.
- [ ] Run the full Debug suite, including enum-conversion behavior. The production heater already
  uses typed Int32 access; the earlier generic-enum Debug assertion is not separately qualified here.
- [ ] Rebuild and exercise current Release/Debug Linux images, including ARM64. Local Docker validation
  was blocked by the unavailable Linux engine. Build/UID checks alone do not prove server health.
- [ ] Repeat independent 1.5.x-client interoperability against the current binaries, including
  cross-version GDS and X509 user authentication. Include trust/revocation rejection, invalid
  credentials, same-session ApplyChanges, and persistent container PKI, not only auto-accepted certs.
- [ ] Validate automatic imports, dependencies, build/publish assets, and execution from the actual
  OPC PLC package in a clean consumer; update and run the sample against that package. Earlier offline
  consumer checks and archive inspection do not cover the complete consumption workflow.
- [ ] Complete deployment and representative performance qualification. Preserve the previous
  deployable image and securely back up configuration and PKI for rollback. Deploy from fresh
  package/publish outputs, not a recursively copied, previously used build directory.