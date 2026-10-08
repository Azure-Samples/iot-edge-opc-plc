# OPC UA 2.0 migration

Status as of **2026-10-08**: OPC PLC **2.16.0** uses exact **2.0.0-preview.6** SDK packages.
Debug and Release builds use the same public SDK packages, including the model generator.
The PLC's Debug configuration and debugger image remain available, but SDK binaries are Release-built.
Test clients now use native runtime type loading and managed
sessions instead of the Reflection.Emit package and raw sessions. Historical head `f8e90ec` includes
client modernization, Boiler1 project removal, and main integration, but predates the public-package
Debug configuration described here. Recorded main-integration Release and
Debug suites each passed **908 tests, 0 failed, 0 skipped**; the earlier 903-test results predate main
integration. A hosted run passed Linux build/tests and image validation for the published
head. Review fixes described below are local follow-up changes, not covered by that hosted run.

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

### Preview release lane

The migration targets `preview`, initially branched from the existing migration baseline
`840e1b6`. After the initial migration merged, main's historian feature was ported deliberately
to the native 2.0 APIs described below.
Stable development and manual stable package publication continue on `main` independently.
Port stable fixes and features to preview deliberately; merge the qualified migration back into
main before publishing a stable `2.16.0`.

Nerdbank.GitVersioning generates `2.16.0-preview.<height>` on `preview`. Other branches and PR builds
also include a commit identifier and are not eligible for preview publication. Preview package
publication is to the externally configured Azure Artifacts feed only, not nuget.org.

Preview builds run the normal build/test and image stages, like main. After build/test succeeds,
Release and Debug images are built and published automatically. PRs and other development branches
validate images without publishing.

To publish the NuGet package, queue the Azure DevOps pipeline manually on the **preview branch** with
**publishPreview** enabled. The default is false. Build/test and image publication must succeed first.
The standalone sample is restored in an isolated package cache using
the exact local Release package and public nuget.org dependencies, without access to the publishing
feed. That package is retained as the `opcplc-release` artifact.
The optional package-publishing stage has no separate approval gate or duplicate image build.
No package is rebuilt in the publishing job.

Configure this variable in the Azure DevOps pipeline's **Variables** settings:

| Variable | Purpose | Required |
| --- | --- | --- |
| `PreviewNuGetPublishFeed` | PLC preview publication destination | Requested preview publication only |
| `BUILD_REGISTRY` | ACR resource name, shared with main's image configuration | Optional; existing registry defaults apply |

Do not enable queue-time overrides for the publication destination. Keep organization, project and feed
identifiers out of the public YAML and Dockerfiles, and do not put credentials in the URLs.
Use an approved Azure Artifacts publication destination. Dependency restoration uses the public
SDK package IDs in both configurations and does not depend on this setting.
Missing or invalid publication configuration fails during build preparation, before image
publication; builds without package publication do not require a publication destination.
Only publication requires `NuGetAuthenticate@1` and the build identity.
The pipeline build identity needs **Feed Publisher (Contributor)** access to the publication feed;
configure this permission outside the repository. Restrict queue permissions to release maintainers
and protect registry/feed resources using ADO permissions. Configure PR validation and branch
protection for `preview` in GitHub/ADO.
PR validation and branch-trigger settings may be managed by the existing ADO pipeline definition.
The existing stable triggers and publishing behavior are not changed by these templates.

Preview images use the `preview/` repository namespace in the registry selected by `BUILD_REGISTRY`
(an ACR resource name, not a login-server URL), defaulting to the existing `industrialiot` registry, and
full `2.16.0-preview.<height>` tags (with `-debug` for Debug images). They do not write stable
repositories or aliases. NuGet versions are immutable: rerunning publication of an already
published version fails explicitly; do not silently skip conflicts. Different commits receive
increasing version heights on the protected preview branch; do not reset its history. Use the package version printed in the build when overriding
`OpcPlcPackageVersion` for a standalone consumer.

For stable promotion, merge preview into main, set a stable version, restore the main/release public
release ref specifications in `version.json`, and revalidate. Never publish
a preview artifact under a rewritten stable version. Until then, keep stable publication manual:
build the stable branch in Release and push its exact package to the approved feed using externally
configured credentials.

Use the .NET 10 SDK and run commands from the repository root. Nerdbank.GitVersioning needs full Git
history. [Directory.Packages.props](../Directory.Packages.props) pins all seven direct OPC runtime
and analyzer packages; the validated dependency graphs contain only preview-6 OPC packages.

The ordinary Release packages are public on nuget.org and need no GitHub Packages token.
Both Release and Debug container restores enable `NuGetAudit=true` and `NuGetAuditMode=all`
to audit direct and transitive dependencies.
This Release workflow selects the official feed without changing persistent NuGet settings:

```powershell
dotnet restore opcplc.sln --source https://api.nuget.org/v3/index.json `
  -p:Configuration=Release -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet build opcplc.sln -c Release --no-restore
dotnet test tests/opc-plc-tests.csproj -c Release --no-build --no-restore
```

Debug uses those same public package IDs and versions, not the SDK's `.Debug` packages.
This preserves Debug compilation of the PLC and tests without requiring authenticated dependency feeds.
For stepping into Debug-built SDK code, use the explicit local-source mode described below.

```powershell
dotnet restore opcplc.sln --source https://api.nuget.org/v3/index.json `
  -p:Configuration=Debug -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet build opcplc.sln -c Debug --no-restore
dotnet test tests/opc-plc-tests.csproj -c Debug --no-build --no-restore
```

Restore again whenever switching build configuration or package/local-source mode so the shared
restore assets match the intended build.

There is no root NuGet configuration. Ordinary build/test commands perform an implicit restore using
the environment's configured sources. Multiple inherited sources without source mapping can cause
`NU1507` under central package management. Use approved source mapping or a single permitted source,
not warning suppression. An Azure Artifacts feed or package proxy must expose the entire pinned SDK
set; successful nuget.org restore does not establish availability through another feed or satisfy
the hosted supply-chain policy. Do not disable that policy to make restore pass.

### Historian integration from main

Main commit `5674286` (historian feature #552) is integrated without reverting the 2.0 migration:

- Keep the opt-in `--historian` / `--hn` interface, existing NodeIds, seeded Int32 archive,
  ten-second updates, 2,000-sample retention, raw range/bounds semantics and 100-value page limit.
- Replace synchronous plugin registration and node-manager history hooks with cancellation-aware
  native async APIs. Use the generated historical configuration factory and optional-child helpers
  instead of legacy construction that leaves mandatory children uninitialized.
- Preserve the custom history route through `HasHistorianProvider`, so startup reconciliation
  retains history advertisement only for the nodes backed by this archive.
- Use immutable `DataValue`, `Variant`, `DateTimeUtc`, `ArrayOf` and `ByteString` APIs.
  Timestamp filtering returns copies; an absent sample uses `DataValue.IsNull`, while a missing
  bound is an explicit null-valued sample with `BadBoundNotFound`.
- Store disposable typed cursors in the session's native continuation-point store. Keep session
  ownership, release semantics and request matching, including timestamp selection.
- Retain `2.16.0-preview.<height>` and preview-only public release refs; do not import main's
  stable `2.15.10` version or its version-height offset.

For raw history reads, use `Source`, `Server` or `Both` timestamps. The pinned SDK rejects
`Neither` with `BadTimestampsToReturnInvalid`; this is tested rather than bypassed.
The existing 28 historian tests are retained and nine migration regression cases cover cancellation,
timestamp/archive immutability, missing bounds and continuation timestamp mismatches.

Local merge qualification against the pinned public packages passed:

| Command | Result |
| --- | --- |
| `dotnet restore opcplc.sln --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json -p:Configuration=Release -p:NuGetAudit=true -p:NuGetAuditMode=all` | Passed |
| `dotnet build opcplc.sln -c Release --no-restore -v minimal` | Passed; 0 warnings/errors |
| `dotnet test tests/opc-plc-tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~OpcPlc.Tests.Historian"` | 37 passed; 0 failed/skipped |
| `dotnet test tests/opc-plc-tests.csproj -c Release --no-build --no-restore` | 989 passed; 0 failed/skipped |
| `dotnet restore opcplc.sln --source https://packagefeedproxy.microsoft.io/nuget/v3/index.json -p:Configuration=Debug -p:NuGetAudit=true -p:NuGetAuditMode=all` | Passed |
| `dotnet build opcplc.sln -c Debug --no-restore -v minimal` | Passed; 0 warnings/errors |
| `dotnet test tests/opc-plc-tests.csproj -c Debug --no-build --no-restore --filter "FullyQualifiedName~OpcPlc.Tests.Historian"` | 37 passed; 0 failed/skipped |

Run the integration suites serially: their server fixtures share TCP port 50001.
No full Debug suite, hosted pipeline or container qualification was run for this merge.

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

### Certificate identity, cancellation, and ownership

New application certificates retain a valid `CN=OpcPlc` distinguished name for preview-6 issuance.
Before configuration creation, the factory resolves persisted identities using the legacy simple
program-name selector and pins the selected certificate's full subject and thumbprint. This preserves
existing identities with additional O/OU/DC attributes without passing an invalid simple name to the
SDK certificate factory. FlatDirectory and KubernetesSecret private-key lookups enforce every supplied
selector, including application URI; URI fallback cannot select an unrelated keypair from a mixed PEM
store. Both stores retain restart and private-key signing regression coverage.

The configuration provider shares one initialization operation. Cancelling a caller cancels only that
waiter; disposing the provider cancels the underlying operation, awaits termination, and disposes the
application and its certificate manager. Cancellation flows through configuration creation, certificate
discovery/management, CSR writes, and diagnostic store reads. PEM enumeration and thumbprint searches
propagate cancellation rather than returning partial success or logging cancellation as a bad
certificate; partially accumulated owning collections are disposed.

Hosted callers let the provider own the application and certificate manager. A successful direct
`OpcUaAppConfigFactory.ConfigureAsync` caller owns the returned configuration's concrete
`Opc.Ua.CertificateManager` and must dispose it after use. Failed or cancelled direct initialization
disposes the internally created application and manager before propagating the error. Owning certificate
collections and entries must also be disposed; retained entries remain usable after manager disposal
until their own references are released.

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
| Release full suite after main integration, before review fixes | 908 passed, 0 failed, 0 skipped; 9m 9s; exit code 0 |
| Debug full suite after main integration, before review fixes | 908 passed, 0 failed, 0 skipped; 10m 51s; exit code 0 |
| Current package graphs | 7 application and 11 test OPC dependencies at exact preview 6; matching configuration only, no `Client.ComplexTypes` or BoilerModel1 project/assembly reference |
| Release NuGet consumer at the `8daebfd` working-tree checkpoint | External standalone sample, audited isolated restore, build and publish; 3 tests pass from build output and 3 from published output, exit codes 0; not rerun for review fixes |
| Package asset propagation | Automatic package-target import and SHA-256 equality for all 10 runtime assets in both build and publish output; no source-project references |
| Approved Debug mirror | 11 upstream Debug packages mirrored unchanged; SHA-512 equality verified after downloading from Azure Artifacts |
| Cold restore from Azure Artifacts before Boiler1 project removal | Audited empty-cache Debug restore of all three projects passed using only the approved mirror, followed by a successful Debug build |
| Debug Linux/amd64 image with mirrored feed | Build and publish passed with a BuildKit token secret; runs as UID 1654; no token in final image metadata; image not pushed |

Recorded main-integration restores kept auditing enabled and builds treated warnings as errors. Those runs
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

### Published-head hosted evidence

A recorded Azure Pipelines run passed Linux build/tests and Release/Debug image validation without
publishing for `f8e90ec`. CodeQL and CLA checks also passed. This supersedes the earlier Debug restore
failure; mirrored-feed CI wiring is already committed and published. Image build/UID validation
does not establish runtime health, interoperability, ARM64 support, or qualification of the local
review fixes.

### Local review-fix validation, 2026-10-07

The local follow-up to `f8e90ec` was validated with matching, already-restored Release assets:

- `dotnet build opcplc.sln -c Release --no-restore`: passed with zero warnings and errors.
- `dotnet test tests\opc-plc-tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~CertificateStoreMigrationTests|FullyQualifiedName~DiSecurityMigrationTests|FullyQualifiedName~OpcUaAppConfigFactoryTests|FullyQualifiedName~KubernetesSecretCertificateStoreTests'`:
  158 passed, zero failed/skipped.
- Full Release suite using `dotnet test tests\opc-plc-tests.csproj -c Release --no-build --no-restore`,
  with TRX logging to the external session-results directory: **923 passed, zero failed/skipped**;
  reported duration **14m 35s**, exit code 0. Evidence: `pr535-fixes-release.trx`.
- IDE diagnostics for modified C# files reported no errors; `git diff --check` passed.

The 15 additional cases cover mixed PEM identity/URI matching, existing richer-DN provider identities,
stalled initialization and in-flight PEM cancellation, direct-factory failure/cancellation cleanup,
and unchanged empty-thumbprint lookup semantics. Focused cases overlap with the full suite.
One earlier full-suite attempt was stopped to finish the empty-thumbprint regression; it is not counted
as validation. No tests, thresholds or assertions were weakened or skipped.

No new restore, Debug run, container run, independent cross-version client run, or clean package-consumer
run was performed for these uncommitted fixes. Historical and published-head results remain separate.

### Historical validation artifacts

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
- Debug image validation log (retained externally)
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
Full-history checkouts support versioning. Debug and Release use the same public SDK packages.
Dependency restore requires no Debug-specific feed, token, or authentication task. Only the optional
preview publication job authenticates to its externally configured destination.
No permissions were changed. Production CI must still pass its supply-chain checks.

The [Release Dockerfile](../Dockerfile.release) and [Debug Dockerfile](../Dockerfile.debug) restore
with matching architecture, configuration, self-contained, and runtime-patch settings before publishing
with `--no-restore`. They do not copy the test-only models. Both restore the ordinary public SDK
packages without package-source credentials or BuildKit secrets. The Debug image still compiles the
PLC in Debug and installs the debugger; it does not supply Debug-built SDK binaries.
[.dockerignore](../.dockerignore) excludes host build outputs, test artifacts, and PKI.

```powershell
docker build -f Dockerfile.release -t opcplc-local:release .
docker build -f Dockerfile.debug -t opcplc-local:debug .
```

Historical Debug-package and authenticated-image results describe the earlier configuration,
not qualification of the current public-package Debug build.

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

Check each command succeeds before continuing. The sample defaults to TCP port 51234; passing 0 explicitly
selects an available port. Both modes have package-consumer coverage. The sample owns temporary client/server
certificate stores and asynchronous shutdown. It accepts otherwise-untrusted certificates only for its
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

- [x] Publish mirrored-feed CI changes and rerun hosted validation: a hosted run passed for
  `f8e90ec`, superseding the earlier Debug restore failure.
- [ ] Commit/push the local review fixes when authorized, then rerun hosted checks, Debug validation,
  and clean package-consumer qualification against those fixes.
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
