# OPC UA 2.0 migration

Status as of **2026-10-01**: OPC PLC **2.16.0** uses exact **2.0.0-preview.6** SDK packages.
Debug builds automatically select the SDK's **`.Debug` packages**, including the model generator;
Release keeps the ordinary package IDs. Test clients now use native runtime type loading and managed
sessions instead of the Reflection.Emit package and raw sessions. Full Release and Debug suites each
pass **903 tests, 0 failed, 0 skipped**. Hosted CI and containers still need current qualification.

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

The ordinary Release packages are public on nuget.org and need no GitHub Packages token. This
Release workflow selects the official feed without changing persistent NuGet settings:

```powershell
dotnet restore opcplc.sln --source https://api.nuget.org/v3/index.json `
  -p:Configuration=Release -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet build opcplc.sln -c Release --no-restore
dotnet test tests/opc-plc-tests.csproj -c Release --no-build --no-restore
```

The matching preview-6 `.Debug` packages are published upstream to
`https://nuget.pkg.github.com/OPCFoundation/index.json`, not nuget.org. The complete set is also mirrored
unchanged to the approved `aio-brokers` Azure Artifacts feed used by CI:
`https://pkgs.dev.azure.com/msazure/One/_packaging/aio-brokers/nuget/v3/index.json`.
Configure authenticated access outside the repository, or use an approved mirror containing the complete set.
When using GitHub alongside a general feed, map `OPCFoundation.NetStandard.Opc.Ua.*` to GitHub and
`*` to the general feed. Supply credentials securely through the environment or a credential provider;
never commit them. With those sources configured:

```powershell
dotnet restore opcplc.sln -p:Configuration=Debug -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet build opcplc.sln -c Debug --no-restore
dotnet test tests/opc-plc-tests.csproj -c Debug --no-build --no-restore
```

Restore again whenever switching Debug/Release: their package IDs differ, and the projects share
restore assets. Do not use `--no-restore` with assets restored for the other configuration.

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
- Model generation is test-only and runs in the test assembly for Boiler2, DI, WoT and SimpleEvents.
  The separate BoilerModel1 project has been removed. Boiler1 tests use discovered runtime structures
  and retained CSV/NodeSet declarations instead of generated CLR types. Its production NodeSet and
  authored model files remain unchanged. Retained generated source is test baseline data, not compiled
  into either the application or a separate model assembly.
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

The pinned preview-6 package provenance was verified against source commit
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

### Main integration (2026-10-05)

Merged `main` through `776a6c7122983056596d793900555a7c0f4d4e4d` (2.15.9), retaining this branch's
2.16.0 version and exact preview-6 package pins. The CodeQL action update is retained. The merge was
reviewed against the pinned stack's
[session/subscription migration guide](https://github.com/OPCFoundation/UA-.NETStandard/blob/d5092e9207816ba26aa18841032e148d19ace6a7/docs/migrate/2.0.x/sessions-subscriptions.md)
and its `StandardServer` implementation.

- Boiler2 now queues overlapping timer callbacks with `WaitAsync` instead of skipping ticks. Callback
  exceptions remain logged, the lock is released in `finally`, and queued callbacks check the stopped
  flag before touching simulation state. The incoming contention regression retains its temperature
  assertions and uses the existing typed read helper.
- The 1.5.378 `OnRequestComplete` override is intentionally omitted: in preview 6, disposing
  `OperationContext` completes the request, and `RequestManager.RequestCompleted` is obsolete. The
  existing `OnRequestValidatedAsync` hook remains. `CurrentInstance` already uses a non-blocking volatile
  read, so the incoming accessor replacements are unnecessary.
- The incoming server-event regressions remain, covering username and anonymous secure sessions,
  discovery, reads, event/data publishing, and a held server semaphore. They use the managed-session
  fixture, native `ArrayOf<EndpointDescription>` and explicit `QualifiedName` values. The guide explicitly
  supports the classic subscription surface alongside V2; these targeted regressions retain it, while
  shared monitoring/throughput helpers continue to use native V2 APIs.
- Secure endpoint selection preserves cancellation and existing session-factory call sites. Probe
  failures are awaited rather than treated as success, and bounded teardown disposes the managed
  session before stopping the server, including when close fails.

### Client modernization

The tests reference `OPCFoundation.NetStandard.Opc.Ua.Client` directly, in both build configurations
and local-source mode. `Client.ComplexTypes` is no longer a dependency. Boiler tests use the
`DefaultComplexTypeSystemFactory` from `Client` and the shared runtime codecs in `Core.Schema`;
the CLR namespace remains `Opc.Ua.Client.ComplexTypes`. Namespace loading is awaited and its loader
disposed. Boiler1 tests now read and write `IStructure` directly; no generated Boiler1 codec is registered.
Independent byte-layout assertions and retained schema checks cover identifiers, fields and encodings.

The shared fixture creates `ManagedSession` with its client telemetry and returns `ISession`.
Managed sessions add automatic reconnection and use the SDK's current subscription engine; connection
security settings and notification/fault assertions are unchanged. Session creation accepts cancellation,
and shared teardown awaits session disposal before stopping the server. Throughput subscription cleanup
is also asynchronous. The lifecycle regression keeps the same managed client across a server restart.

The test-only reconnect policy retains the SDK defaults but stops retrying `BadIdentityTokenInvalid`,
`BadIdentityTokenRejected`, and `BadUserAccessDenied`. The fixture supplies fixed credentials, so retrying
those failures only delayed negative-authentication tests by five minutes per attempt. A deadline now
guards that test without replacing the expected service exception with cancellation.

Other stack packages remain necessary, including `Gds.Client` for push-configuration tests and `Server`
for the generated model's fluent node builders. Generated models remain test-only; production already
uses runtime NodeSets, async node managers, and DI hosting.

### Test-helper cleanup

Shared reads use typed `Variant` conversions, with explicit null rejection for value types. Method helpers
pass and return `Variant` collections directly. Boiler2 and notification tests use typed scalar/enum
accessors; event dictionaries retain native array wrappers with explicit time, severity and byte-string
conversions instead of legacy boxing mode.

Shared monitoring and throughput tests use the native V2 `ISubscriptionManager` and notification-handler
APIs. Setup waits for monitored items to be created, with a deadline and immediate status-error checks.
Publishing intervals, priority, minimum lifetime, queue sizes and throughput assertions are preserved.
Subscriptions are disposed asynchronously, and payload pooling must remain disabled while these tests
retain notification values. Dedicated low-level classic-subscription tests and heterogeneous object-valued
comparisons remain separate coverage; this is not a blanket rewrite of all test representations.

### Boiler1 project removal

The solution now contains only the server and test projects. Combining Boiler1 and Boiler2 generator
inputs was checked and rejected by the pinned generator with `MODELGEN003` (duplicate `Boilers`
symbol in their shared model URI). Instead, Boiler1 tests use the existing runtime codec path. The 220
model checks and five live Boiler1 tests remain, with Boiler1-specific generated API checks replaced by
retained CSV/NodeSet, runtime encoding-ID, exact binary-layout and XML/JSON round-trip checks.

Retained initializer/default XML is inspected without a Boiler1 codec so its name-only enum `On` stays
intact. The preview-6 runtime decoder otherwise resolves that name-only XML to zero/`Off`; this limitation
is recorded rather than accepted as the expected default. Live heater values and binary/round-trip enum
values remain checked numerically, and production initializes its heater explicitly. No SDK source or
production model was changed to remove the test project.

## Validation

The following are local results for preview 6, not hosted pipeline results:

| Check | Verified result |
| --- | --- |
| Audited cold restore before client modernization | Fresh external package/fallback directories from nuget.org; 7 application, 12 test, and 8 reference-model OPC dependencies, all exactly preview 6 |
| Release and Debug builds before Debug package selection | All three projects build successfully using ordinary SDK packages |
| Debug-package audited cold restore before client modernization | External cache and mapped GitHub/general feeds; 7 application, 12 test, and 8 reference-model OPC dependencies, all `.Debug` at exact preview 6 |
| Debug-package regressions before client modernization | All three projects build; 367 model, boiler, security, and certificate-store tests passed, 0 failed, 0 skipped |
| Configuration selection before Boiler1 project removal | Default/explicit Debug, explicit Release and local-source mode evaluated across all three projects; audited Release restore/build passes with no `.Debug` dependencies |
| Capacity and live boiler checks | 15 passed |
| Generated model equivalence | 220 passed, including five identifier tables |
| Full Release suite after compatibility fixes | 902 passed, 0 failed, 0 skipped; reported duration 7m 2s |
| Simplified CI commands before Debug package selection | Normal implicit-restore Release/Debug builds and 15 focused tests passed |
| Inspected OPC PLC NuGet archive | Ten runtime assets and exact preview-6 dependencies |
| Native complex-type loader | Release restore/build and all 5 Boiler tests passed without `Client.ComplexTypes` |
| Managed-session initial checks | 10 fault-injection, data-monitoring and throughput tests passed with unchanged assertions |
| Managed identity policy and lifecycle | 13 lifecycle/fault tests passed; rejected identities fail promptly and automatic reconnect remains enabled |
| Release full suite after Boiler1 project removal, before main integration | 903 passed, 0 failed, 0 skipped; 10m 12s; exit code 0 |
| Debug full confirmation after Boiler1 project removal, before main integration | 903 passed, 0 failed, 0 skipped; 10m 32s; exit code 0 |
| Main integration focused regressions | 22 passed, including queued Boiler2 callbacks and both authentication variants of the server-event regressions |
| Current Release full suite after main integration | 908 passed, 0 failed, 0 skipped; 9m 9s; exit code 0 |
| Current Debug full suite after main integration | 908 passed, 0 failed, 0 skipped; 10m 51s; exit code 0 |
| Current package graphs | 7 application and 11 test OPC dependencies at exact preview 6; matching configuration only, no `Client.ComplexTypes` or BoilerModel1 project/assembly reference |
| Clean Release NuGet consumer | External standalone sample, audited isolated restore, build and publish; 3 tests pass from build output and 3 from published output, exit codes 0 |
| Package asset propagation | Automatic package-target import and SHA-256 equality for all 10 runtime assets in both build and publish output; no source-project references |
| Approved Debug mirror | 11 upstream Debug packages mirrored unchanged; SHA-512 equality verified after downloading from Azure Artifacts |
| Cold restore from Azure Artifacts before Boiler1 project removal | Audited empty-cache Debug restore of all three projects passed using only `aio-brokers`, followed by a successful Debug build |
| Debug Linux/amd64 image with mirrored feed | Build and publish passed with a BuildKit token secret; runs as UID 1654; no token in final image metadata; image not pushed |

Current restores kept auditing enabled and builds treated warnings as errors. The main-integration runs
used a command-scoped workstation proxy; Debug reused the previously verified external package cache,
so this is not a new cold-restore result or proof of hosted feed availability. The resolved graphs contained
7 application and 11 test SDK packages, all exact preview 6 and matching the build configuration.
During earlier client
modernization, a generated-version file lock interrupted the first Debug build before tests; a serialized
MSBuild retry passed. The added
managed reconnect regression accounts for the change from 902 to 903 tests. In the client-modernization runs, rejected
credentials completed in about three seconds and restart recovery in about 17 seconds.
Main integration adds one Boiler2 contention case and four server-event cases, bringing the total to 908.

The first Debug full run after project removal had one failure in the unchanged event-monitoring test
(eight notifications instead of six). Its focused rerun with all model/Boiler1 checks passed 226/226,
followed by the full 903/903 confirmation above. The initial failure is retained in the evidence;
no timing thresholds, notification assertions or tests were weakened or skipped.

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
- `20260930-preview6-debug-packages-focused.trx`
- `20260930-native-complex-types.trx`
- `20260930-managed-client-policy.trx` and `.log`
- `20260930-native-client-release-full.trx` and `.log`
- `20260930-native-client-debug-full.trx` and `.log`
- `20260930-package-consumer-build.trx` and `.log`
- `20260930-package-consumer-publish.trx` and `.log`
- `20260930-optional-cleanup-release-full.trx` and `.log`
- `20260930-optional-cleanup-debug-full.trx` and `.log`
- `20260930-aio-debug-image.log`
- `20261001-runtime-boiler-model-equivalence.trx` and `.log`
- `20261001-symbolic-enum-default-limitation.trx`
- `20261001-no-boiler-project-release-full.trx` and `.log`
- `20261001-no-boiler-project-debug-full.trx` and `.log` (initial event-count failure)
- `20261001-no-boiler-project-debug-focused.trx` and `.log`
- `20261001-no-boiler-project-debug-confirmation.trx` and `.log`
- `20261005-main-merge-focused.trx` and `.log`
- `20261005-main-merge-release-full.trx` and `.log`
- `20261005-main-merge-debug-full.trx` and `.log`

Earlier SDK checkpoints passed independent 1.5.378.176-client interoperability and Linux/amd64
non-root image checks. Those results do not qualify the current preview-6 binaries or Docker changes.
Superseded experiments, earlier failure details, and older evidence paths remain in this document's
Git history rather than serving as current setup instructions.

## CI and containers

[CI](../tools/templates/ci.yml) uses normal build/test commands with implicit restore, explicitly
enables auditing of all dependencies, and retains warnings as errors and build-time package generation.
Full-history checkouts support versioning. The migration-only cold-restore wrapper and GitHub package
secret plumbing remain removed. Debug builds use the non-secret `OpcUaDebugNuGetFeed` pipeline variable
to select `aio-brokers` and `NuGetAuthenticate@1` for the existing build identity. Release build/test
sources are unchanged. Both One and Project Collection build-service identities already have feed access;
no permissions were changed. Production CI must still pass its supply-chain checks.

The [Release Dockerfile](../Dockerfile.release) and [Debug Dockerfile](../Dockerfile.debug) restore
with matching architecture, configuration, self-contained, and runtime-patch settings before publishing
with `--no-restore`. They do not copy the test-only models. Release uses the ordinary public SDK
packages. Debug selects the approved mirror and requires the `opcua_nuget_token` BuildKit secret for
restore. Image jobs map their short-lived `System.AccessToken` to `OPCUA_NUGET_TOKEN`; only the Debug
restore mounts it. The multiarchitecture publishing script uses the same secret and fails before registry
operations if it is missing. No token is written to NuGet configuration or image layers. Do not pass
credentials through Docker build arguments; `OPCUA_DEBUG_NUGET_FEED` is a non-secret URL override only.
[.dockerignore](../.dockerignore) excludes host build outputs, test artifacts, and PKI.

```powershell
docker build -f Dockerfile.release -t opcplc-local:release .
docker build -f Dockerfile.debug -t opcplc-local:debug `
  --secret id=opcua_nuget_token,env=OPCUA_NUGET_TOKEN .
```

For the local Debug command, supply a feed-read token securely through `OPCUA_NUGET_TOKEN` first.
The default feed is `aio-brokers`; changing the URL does not grant access to another feed.

The [image pipeline](../tools/templates/acrbuild.yml) builds PR and non-release images without
publishing and checks for UID 1654. Only non-PR `main` and `release/*` branches enter the existing ACR
publishing path. Whether development branches should publish is a separate policy decision; this
migration's CI simplification did not expand publishing or change registry authentication.

## Package consumer

The [sample project](../samples/OpcUaUnitTests.csproj) is a standalone NUnit consumer, not part of the
server solution. It deliberately references the packed server, with no repository project references or
generated test-model assembly. The following PowerShell workflow runs from the repository root and
uses the version from the generated NuGet manifest, since repository versioning supplies the package version.

```powershell
$consumer = Join-Path ([IO.Path]::GetTempPath()) ("opcplc-consumer-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $consumer | Out-Null
Copy-Item samples/OpcUaUnitTests.csproj,samples/OpcPlcBase.cs,samples/OpcUaUnitTests.cs $consumer
Copy-Item samples/nuget.config.sample "$consumer/NuGet.Config"
dotnet pack src/opc-plc.csproj -c Release -o "$consumer/packages" `
  -p:UseLocalOpcUaStack=false -p:GeneratePackageOnBuild=false -p:NuGetAudit=true -p:NuGetAuditMode=all
$package = Get-ChildItem "$consumer/packages/Microsoft.IoTEdge.OpcPlc.*.nupkg" | Select-Object -First 1
$archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
  $entry = $archive.Entries | Where-Object FullName -Like '*.nuspec'
  $reader = [IO.StreamReader]::new($entry.Open())
  try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
  $version = $manifest.package.metadata.version
} finally { $archive.Dispose() }
dotnet restore "$consumer/OpcUaUnitTests.csproj" --configfile "$consumer/NuGet.Config" `
  --packages "$consumer/cache" -p:OpcPlcPackageVersion=$version -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet test "$consumer/OpcUaUnitTests.csproj" -c Release --no-restore -p:OpcPlcPackageVersion=$version
dotnet publish "$consumer/OpcUaUnitTests.csproj" -c Release --no-restore `
  -p:OpcPlcPackageVersion=$version -o "$consumer/publish"
Push-Location "$consumer/publish"
try { dotnet vstest OpcUaUnitTests.dll } finally { Pop-Location }
```

Check each command succeeds before continuing. The sample owns a dynamic TCP port, temporary client/server
certificate stores, and asynchronous shutdown. It accepts otherwise-untrusted certificates only for its
isolated test connection; this is not production PKI qualification. The tests verify SignAndEncrypt,
method calls, write/read-back, discovered runtime structures, restart and all ten packaged asset paths.

Local qualification used the freshly packed `2.16.0-g8daebfd7be` Release package from the working tree,
an external consumer directory and an initially empty dependency cache. The public proxy lacked SDK
preview 6, so the eight previously verified Release SDK NuGet archives were staged in the temporary
local feed. Other dependencies restored from the proxy with auditing enabled. No machine, repository
or pipeline feed settings changed. This validates package consumption, not live upstream availability;
Debug feed configuration was deferred during that consumer check and is now addressed separately above.
Both build and published
outputs passed 3 tests with no failures/skips; all ten asset hashes matched the package archive.

## Remaining qualification

- [ ] Commit/push the mirrored-feed CI changes and rerun hosted validation. Build `183536411` passed
  Release build and Linux tests but failed Debug restore against nuget.org; the new pipeline wiring
  still needs its hosted run. Mirror contents, cold restore, Debug build and Linux/amd64 image build pass locally.
- [ ] Exercise current Release/Debug Linux images, including ARM64. The Debug Linux/amd64 build and
  UID check now pass, but these checks alone do not prove server health or production security.
- [ ] Repeat independent 1.5.x-client interoperability against the current binaries, including
  cross-version GDS and X509 user authentication. Include trust/revocation rejection, invalid
  credentials, same-session ApplyChanges, and persistent container PKI, not only auto-accepted certs.
- [x] Validate the Release package in a clean Windows consumer, including automatic imports,
  dependencies, build/publish assets and execution of the updated sample from both output directories.
- [ ] Complete deployment and representative performance qualification. Preserve the previous
  deployable image and securely back up configuration and PKI for rollback. Deploy from fresh
  package/publish outputs, not a recursively copied, previously used build directory.