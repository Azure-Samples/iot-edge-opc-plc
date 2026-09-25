# OPC UA 2.0 migration

## Draft PR checkpoint

This branch is a source-validated migration checkpoint, **not a merge-ready release change**.
It requires a compatible sibling UA-.NETStandard checkout (qualified commit
`9b8a3b1188411f3858fbdf3e4dacf8b82ca0cec9`). The configured OPC NuGet pins are still 1.5.378.176 and
are inactive in source mode; disabling source mode does not yet provide a supported package build.

Before merging for release: adopt the official 2.0 package set, qualify normal audited feed restores,
make source references opt-in, and complete the package/consumer/CI/deployment gates below.
The migration commit includes implementation, regression tests, test-only model references, runtime
asset rules, and this evidence record. Deferred Docker changes and ignored local probes, logs,
build outputs, caches, and PKI are not part of the commit. No PR or remote publication is implied.

Pre-commit validation on `migration/opc-ua-2-source-checkpoint`: fresh full Release suite
**913 passed, 0 failed, 0 skipped**, exit 0, against the exact clean SDK commit above.
Local evidence is `tests/TestResults/stack-v2-migration/pr-checkpoint-release-full.trx` and `.log`.
This run includes the typed heater read and offline asset-layout fixes. Existing Debug and independent
1.5.x-client evidence remains recorded below; normal-feed auditing and package-mode CI are still blocked.

## Constraints

- Migrate the PLC to the modern OPC UA 2.0 architecture; do not emulate removed stack APIs.
- Do not modify UA-.NETStandard source unless a minimal reproduction demonstrates a stack bug.
- Document the expected behavior, actual behavior, and regression test before proposing a stack fix.
- Preserve CLI options, application identity, PLC namespace URIs and NodeIds, simulation behavior,
  authentication, certificate persistence, reverse connect, and supported deployment modes.
- Retain the existing test assertions. Document intentional specification-driven behavior changes.
- Do not commit or create branches without explicit approval.

## Source-only resume (2026-09-25)

The stack bugs listed in the filing table below have been addressed upstream. The active SDK
checkout is clean master at `9b8a3b1188411f3858fbdf3e4dacf8b82ca0cec9`, matching its locally recorded
origin/master at the start of this work. The official NuGet build is expected on Monday; no version
or publication claim is made before it exists. The user authorized continuing against source and,
after inspection, explicitly approved reconciling the partially reverted PLC code and build files.

The earlier migration checkpoint is not the starting state of this resumed workspace: the async PLC
implementation remained, but project metadata, the Boiler1 test project, several server/model files,
and some certificate conversions had reverted or become inconsistent. Existing issue links and
unrelated worktree edits were preserved. No archived stack patch was reapplied and no SDK source
was changed in this continuation.

### Build mode

- `UseLocalOpcUaStack` defaults to `true`; `OpcUaSourceRoot` defaults to the sibling
  `../UA-.NETStandard` checkout. All `OPCFoundation.NetStandard.Opc.Ua.*` package references are
  removed in source mode, including legacy Debug and Gds.Client.Common variants, before adding
  current SDK project references. This prevents mixing 1.5 packages with 2.0 assemblies.
- The OPC package versions currently in `Directory.Packages.props` remain **1.5.378.176**, unchanged
  during this work. They are inactive in source mode, not a valid fallback for the migrated code.
  Do not use package mode or ship a package until the official 2.0 build is available and qualified.
- The PLC's existing Newtonsoft usage is now an explicit dependency at 13.0.4, matching the SDK's
  existing pin. Source mode disables automatic package generation; no packages were published.
- Production models load NodeSet2 XML with runtime codecs. Retained generated C# stays excluded,
  not deleted. Model generation is confined to the tests and the separate Boiler1 reference project.
  That project uses the authored ModelDesign XML and CSV; switching it to NodeSet2 generation changes
  exported schema/encoding identifiers and fails the strict retained-identity checks.
- Service overrides now use the merged SDK's `ValueTask`, `RequestLifetime`, `ByteString`, and
  `ArrayOf<T>` contracts. The native async manager factory and post-codec plugin initialization are
  connected. Metric labels remain the original RequestType names.
- `OpcPlcServer` again uses the existing DI host wrapper. Cancellation, startup failure, and restart
  drain simulation and dispose the host; an early startup failure does not stop uninitialized plugins.
  TrustList writable flags are applied through the async manager collection without relaxing access.
- Custom stores use ownership-transferring certificate wrappers at PEM boundaries. The flat store
  now honors `NoPrivateKeys` before loading PEM key material, as its existing tests require.

Typical Release validation from the PLC repository root:

```powershell
dotnet test tests/opc-plc-tests.csproj -c Release `
  -p:UseLocalOpcUaStack=true -p:OpcUaSourceRoot=C:\src\UA-.NETStandard `
  -p:CustomTestTarget=net10.0 -p:GeneratePackageOnBuild=false
```

Local runs additionally use the existing ignored
`tests/TestResults/stack-v2-migration/current-stack-source.props` via `DirectoryBuildPropsPath` to
isolate absolute per-project NuGet assets directories. Restore used the official NuGet v2 endpoint
and command-line-only `NuGetAudit=false` for the known local feed/TLS problems; repository feed and
audit policy were not changed. Do not add the old CustomAfterMicrosoftCommonTargets overrides.

### Evidence

Fresh artifacts are under `tests/TestResults/stack-v2-migration/`:

- `merged-sdk-source-app-final-build.log`: application Release build, zero warnings/errors.
- `merged-sdk-source-lifecycle.trx`: 27 lifecycle and Boiler2 cases pass after the host repair.
- `merged-sdk-source-gates.trx`: initial broad gate, 467/470 pass. The three failures identified
  public-only PEM key access, async TrustList discovery, and the wrong Boiler1 generator input.
- `merged-sdk-source-gate-repairs.trx`: all eight parameterized checks for those failures pass after
  the scoped repairs. No assertion, timeout, equivalence allowance, or performance threshold changed.
- `merged-sdk-source-full.trx` and `.log`: fresh full Release suite **913 passed, 0 failed, 0 skipped**,
  exit 0, approximately 7 minutes. This is validation of the merged SDK at the commit above and the
  reconciled PLC, not reuse of the historical pre-merge result.
- Throughput in that full run: burst 250,000/250,000 in 1.59 seconds (157,663/s), minimum-rate
  100,000/100,000 in 0.99 seconds (101,315/s), and sustained 250,000/250,000 in 6.83 seconds.
- Final assets inspection finds 11 OPC source projects and zero OPC binary packages. SHA256 checks
  match all eleven OPC runtime DLLs in the test output to the current SDK Release/net10.0 outputs.
  The SDK worktree remains clean. The scoped diff whitespace check passes, and the reconstructed
  Boiler1 project parses as valid MSBuild XML and builds successfully.

The focused and full-suite results overlap and are not additive. Subsequent Debug and independent
client qualification are recorded below. Official package mode remains unqualified. The cached
VS Code load diagnostic for the initially empty Boiler1 project has cleared.

Docker remains deferred. Package adoption, normal-feed auditing, and deployment qualification remain
separate gates. This continuation does not recreate issues or change the merged stack fixes.

## Debug and legacy-client qualification (2026-09-25)

The user authorized Debug validation followed by independent 1.5.x-client interoperability while
continuing to use the merged SDK source at `9b8a3b1188411f3858fbdf3e4dacf8b82ca0cec9`.

### Debug results

- Initial full Debug run: 885 passed, 28 failed (`merged-sdk-source-debug-full.trx`). Twenty-seven
  failures hit `VariantHelper.TryCastTo<T>`'s Debug assertion `typeof(U) == typeof(T)` when Boiler1
  read its Int32 heater enumeration as a CLR enum; the remaining stacklight failure followed the
  interrupted simulation state. Throughput passed even in that initial run.
- The PLC now reads the heater field with `GetInt32()` and compares it to the enum's Int32 constant.
  This uses the wire type directly, preserves the heater rules, and avoids the asserting generic
  conversion. No SDK code, assertions, or test thresholds were changed. The SDK helper's generic
  enum branch remains a separate upstream triage item; this is not a claim that helper was repaired.
- Existing heater, event, and stacklight checks pass 28/28 in `merged-sdk-source-debug-enum.trx`.
- Final full Debug run: **913 passed, 0 failed, 0 skipped**, exit 0
  (`merged-sdk-source-debug-final.trx` and `.log`). All eleven OPC runtime DLLs in the Debug test
  output SHA256-match the merged SDK's Debug/net10.0 outputs.
- Debug throughput: burst 250,000/250,000 in 2.61 seconds (95,747/s), minimum-rate 100,000/100,000
  in 1.18 seconds (84,791/s), sustained 250,000/250,000 in 7.46 seconds.

### Independent client results

The existing ignored `tests/TestResults/stack-v2-migration/interop-v1-client` probe was extended,
without upgrading its SDK, to exercise runtime complex values and custom events. It references exact
`1.5.378.176` Client and Configuration packages and links only the retained legacy Boiler1/SimpleEvents
codec source. It has no SDK/PLC project references and no dependency on newly generated 2.0 models.
All five resolved OPC runtime packages are 1.5.378.176; output DLL hashes match their package contents.

The same client binary passes against separately spawned **Debug and Release** PLC processes:

- SignAndEncrypt with Aes256_Sha256_RsaPss, anonymous and valid username sessions.
- Rejection of an invalid password with an identity/access-denial status.
- UInt32 and opaque-NodeId reads; paused scalar write/readback; stop/start methods.
- Nested BoilerDataType decoding with the legacy codec, complex write/readback, and HeaterOn.
- A changing data subscription and a fresh session with a successful read.
- Custom event type and CycleId, plus scalar and two-element array CycleStep payloads decoded by
  the legacy codec, with field values checked.

Evidence: `merged-sdk-v1-client-build.log`, `merged-sdk-v1-interop-debug.log`, and
`merged-sdk-v1-interop-release.log`, each successful with exit 0. The current Release server was
rebuilt after the Int32-read change with zero warnings/errors (`merged-sdk-interop-release-build.log`).
The earlier Release 913/913 run predates that one-line change; the complete Debug suite and both
cross-version process checks covered it at this checkpoint. The subsequent pre-commit Release run
at the top of this document also passes 913/913 with that change included.

Both server processes use ephemeral ports and isolated temporary PKI. The probe terminates only its
own spawned process and deletes its temporary directory in finally. Channel certificates are
auto-accepted for this isolated compatibility probe; this is not production trust-chain/revocation
qualification. Cross-version GDS, X509 user authentication, and container/Linux behavior are not
covered by this probe and must not be inferred from the successful Windows checks.

Build the client without importing PLC source-reference targets:

```powershell
dotnet build tests/TestResults/stack-v2-migration/interop-v1-client/interop-v1-client.csproj -c Release `
  -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false `
  -p:ImportDirectoryPackagesProps=false -p:ManagePackageVersionsCentrally=false
dotnet tests/TestResults/stack-v2-migration/interop-v1-client/bin/Release/net10.0/interop-v1-client.dll `
  C:\src\iot-edge-opc-plc\src\bin\Release\net10.0\opcplc.dll
```

The client probe retains its isolated package cache and diagnostic-only NuGet audit override.
The SDK checkout remains clean. Package pins, repository feed policy, and the Docker deferral are unchanged.

## Package and feed-audit gate (2026-09-25)

Status: **blocked on package publication and configured-feed connectivity**. No package versions,
feed settings, TLS settings, audit policy, or source-mode defaults were changed by this check.

### Package availability

- The PLC's configured online source is `https://packagefeedproxy.microsoft.io/nuget/v3/index.json`.
  The saved PLC source listing has nuget.org disabled and the Visual Studio offline source enabled.
  SDK projects also inherit their repository's nuget.org source; actual restore behavior is per project.
- Live proxy metadata returned only `2.0.0-preview.3`, `2.0.0-preview.4`, and `2.0.0-preview.5` for
  `OPCFoundation.NetStandard.Opc.Ua.Server` and
  `OPCFoundation.NetStandard.Opc.Ua.SourceGeneration`. The generator identity was verified from the
  SDK's SourceGeneration.Pack project, not inferred from the differently named build props file.
- These older previews do not establish a published package set containing the merged fixes.
  Package adoption remains deferred until the scheduled official build is available through the
  intended feeds. This check does not claim absence from every possible upstream feed.

### Configured-feed restore

- Evaluated settings: `NuGetAudit=true`, `NuGetAuditMode=all`, no NuGetAuditSuppress items, and no
  command-line RestoreSources override. The gate used the current SDK source graph while checking
  availability and auditing of its package dependencies; it is not a restore of future 2.0 packages.
- An ignored `tests/TestResults/stack-v2-migration/feed-audit-source.props` isolates per-project assets
  under `obj/feed-audit-source`, separate from the qualified `obj/current-stack-source` assets.
- Restore used `--force --no-http-cache` and a newly created primary package directory:
  `tests/TestResults/stack-v2-migration/feed-audit-packages-635dd7c132cf4da6bbba1152c9b228cd`.
  The configured Visual Studio shared fallback folder remained present, so this is not proof of a
  fully cold dependency restore even for the projects that restored successfully.
- `configured-feed-audit-restore.log` records exit 1. SDK projects fail with `NU1301` loading
  `https://api.nuget.org/v3/index.json`: SSL connection failure / TLS alert `HandshakeFailure`.
  Some PLC projects report successful restore, but the overall graph is incomplete.
- No NU190x diagnostics were emitted. That does **not** establish that auditing completed or that
  the dependency graph is free of known vulnerabilities.
- Verification confirmed the qualified source-build assets still reference their original package
  folders, not the gate cache. The SDK worktree remains clean; no build outputs were replaced.

Resume this gate after the official packages are available and the required configured feeds are
reachable. Verify package provenance and a complete runtime/test-generator set, then run a genuinely
cold restore (including isolation from fallback folders) with auditing enabled before package-mode
builds/tests. Do not declare success by using the prior cache, forcing the v2 connectivity workaround,
disabling audit, or relaxing TLS validation. Offline package-layout review can proceed separately;
Docker remains deferred.

## Offline artifact-layout review (2026-09-25)

The user authorized offline packaging/layout review and scoped fixes, without changing package pins,
creating/publishing packages, or running Docker. This section validates MSBuild inputs and consumer
copy behavior; it is not qualification of a final NuGet archive or a deployed image.

### Findings and fixes

1. The application's broad model-folder Content globs selected 81 non-assembly package inputs,
   including 27 C# files, five scripts, legacy binary models, and model-compiler inputs, but omitted
   `wwwroot/stacklight.html`. The project now uses one explicit ten-file runtime asset list for build,
   publish, and package metadata: eight NodeSet2 XML files, the stacklight HTML, and `nodesfile.json`.
   Each package path preserves its exact runtime-relative location under `contentFiles/`.
   Retained model source and test baselines were not deleted or changed.
2. The package target copied content only in an AfterTargets=Build task, leaving the consumer's
   publish manifest unaware of it. A dependency-free consumer probe reproduced all ten assets missing
   from ComputeFilesToPublish despite successful build-time copying. The target now declares linked
   None items with TargetPath, CopyToOutputDirectory, and CopyToPublishDirectory metadata; Pack=false
   prevents the consuming project from repacking the supplied assets.
3. The existing application build directory contains four PKI/runtime-state files from previous runs.
   They are excluded from the evaluated release inputs and were left untouched. Do not deploy by
   recursively copying or archiving a used bin directory; use a fresh output produced by the qualified
   package/publish workflow when that later gate is enabled.
4. `samples/OpcPlcBase.cs` compiles against the current PLC/SDK public API in the offline consumer
   probe. The illustrative sample project still pins PLC package `2.10.0-gdd6f023a37`, and its example
   test relies on an external OpcUaClientFactory/test setup. No sample pin was changed. Actual restore
   and execution against the migrated PLC package remain pending publication and consumer qualification.

### Checks completed

- `_GetPackageFiles` evaluation, without Pack/GenerateNuspec: now 13 non-assembly inputs, consisting
  of the ten runtime assets plus the build target, README, and icon. Each asset appears exactly once
  at the expected path. No loose C#, scripts, legacy .uanodes, test fixtures, or PKI/key-file inputs.
- Offline application Release build: zero warnings/errors. The ten files copied to the application
  output SHA256-match their source assets (`offline-asset-build.log`).
- `ResolveReferences;ComputeFilesToPublish` evaluation: 45 files, with the ten required assets and
  application/SDK/runtime assemblies present. No BoilerModel1 test assembly, model-generator DLLs,
  test framework DLLs, loose source/tool scripts, or PKI/key-file inputs. An initial evaluation without
  ResolveReferences had only 38 entries and is not the final dependency-complete manifest.
- The local package-shaped fixture contains copies of the evaluated file inputs, not a .nupkg.
  A temporary net10.0 consumer imports the actual package target. It has no NuGet package references,
  restores only from the installed offline source, and disables repository-wide build imports.
- Before the target fix: consumer Build succeeds, publish-asset check fails for all ten files
  (`offline-consumer-before.log`). After the fix: Build and publish-manifest assertions pass
  (`offline-consumer-after.log`).
- The unchanged sample base compiles in the consumer using local assembly references
  (`offline-sample-consumer.log`). A final run into a newly created output directory also passes;
  all ten asset hashes match the staged package content (`offline-consumer-fresh-output.log`).
- The SDK worktree remains clean. No package archive, deployment, or Docker operation was performed.
  Production C# and test assertions are unchanged by this layout review, so no full regression suite
  was rerun for these build-item changes.

Manifests and the ignored probe are under `tests/TestResults/stack-v2-migration/`:
`offline-package-inputs-before.json`, `offline-package-inputs-after.json`, `offline-publish-inputs.json`,
and `offline-layout-probe/consumer/consumer.csproj`. The probe tests manually imported package targets;
NuGet dependency resolution, automatic imports from an actual archive, package-mode build switches,
consumer execution, and platform/deployment behavior remain separate release gates.

## Target architecture

- Use the stack's async node managers, runtime NodeSet2 imports, and server-scoped runtime codecs.
- Keep simulation state and deterministic time independent from OPC UA transport and node registration.
- Use `Variant`, `ArrayOf<T>`, `ByteString`, and typed accessors at OPC UA boundaries.
- Compose telemetry, authentication, certificate stores, and lifecycle services through supported
  injectable providers. Preserve a direct construction path for existing PLC callers.
- Preserve model identifiers and encoding; use generated models only as independent test references.
- Keep compatibility analyzers and runtime shims temporary; none may remain in the release output.

## Execution checkpoints

- [x] Capture the unmodified 1.5.378.176 Release test baseline: 225 passed, zero failed.
- [x] Use native async node managers, current value/service APIs, and cancellation-safe hosted lifecycle.
- [x] Load runtime models and verify identities/encoding against independent test-only model references.
- [x] Migrate security providers, certificate ownership, reverse connect, and test-client fixtures.
- [x] Use a clean merged SDK source checkout without mixed OPC binary packages.
- [x] Pass the full PLC Release and Debug regression suites at the checkpoints recorded above.
- [x] Validate the documented Windows workflows with unmodified 1.5.378.176 client SDK binaries.
- [ ] Adopt the official 2.0 runtime and compatible test-generator packages when available; replace legacy
  package IDs/build switches and restore package mode as the default, with local source mode opt-in.
- [ ] Validate clean CI-feed restores with auditing enabled and no diagnostic restore overrides.
- [x] Review offline package/publish file inputs, runtime asset layout, and the sample base API;
  verify build/publish asset copying through the real package target in an isolated consumer.
- [ ] Qualify the actual NuGet archive, automatic consumer imports/dependencies, migrated sample
  package pin, end-to-end sample execution, and release CI configuration after package adoption.
- [ ] Complete deployment/platform qualification; Docker remains deferred by the user.
- [ ] Separately triage the SDK generic-enum Debug assertion encountered during qualification.

## Testing strategy

Reuse the NUnit integration suite and its deterministic TimeService mocks with minimal fixture
changes. Do not replace protocol-level assertions with implementation-specific assertions.

Canonical baseline command, run from the repository root:

```powershell
dotnet test tests/opc-plc-tests.csproj -c Release --logger "trx;LogFileName=baseline-1.5.378.trx" --results-directory tests/TestResults/stack-v2-migration
```

Retain TRX counts and failure details for every checkpoint. A build or test failure is not a completed
checkpoint. Check representative scalar, array, matrix, opaque, and structured values; method calls;
subscriptions and events; alarms; custom model loading; and persistent certificate stores.

Security acceptance must include rejected certificates and invalid credentials, not only auto-accept.
Verify GDS trust-list staging and same-session ApplyChanges. Run a separately pinned 1.5.x client
against the migrated server so upgrading both sides cannot conceal wire incompatibilities.

Container runtime, cross-version interoperability, and performance qualification remain explicit
release gates, separate from in-process tests. Preserve the previous deployable image and securely
back up configuration and PKI state before canary rollout; never commit certificates or secrets.

## Historical evidence (2026-09-17)

- 2026-09-17: PLC worktree was clean on main before migration.
- Both application and test project target net10.0; stack references are 1.5.378.176.
- Fifteen checked-in model-generated C# files cover Boiler1, Boiler2, DI, WotCon, and SimpleEvents.
- Test ComplexTypes package references currently mix Debug and Release variants.
- Local stack project references use the old Libraries/Stack directory layout.
- NuGet v3 metadata requests failed TLS on this machine. The official v2 endpoint works.
- The full unmodified Release suite passed: 225 executed, 225 passed, zero failed or skipped.
  Evidence: `tests/TestResults/stack-v2-migration/baseline-1.5.378.trx`.
- Exact `2.0.0-preview.4` package identities were verified through v2 metadata and isolated restore.
  This release still uses `Gds.Client.Common`; the checkout's newer rename guidance does not apply.
- The application and tests now use ordinary packages in both configurations. Local source-reference
  switches were removed to prevent accidentally mixing the newer stack checkout with preview.4.
- The source generator is an explicit build-only reference; preview.4 Core does not supply it.
- Boiler1 and Boiler2 share a model URI and collide when ModelDesign inputs are generated together.
  `models/BoilerModel1/BoilerModel1.csproj` isolates Boiler1 without changing its model URI.
  Its Release build passed with zero warnings and zero errors. Address-space identity is not yet tested.
- Newtonsoft.Json is now explicit because the PLC still uses it and 2.0 no longer supplies it transitively.
- Migration analyzer auto-fixes were applied; a missing generic collection import was repaired manually.
- The latest full application build fails with 20 errors. The migrated application has NOT passed tests.
  Remaining declaration errors include Kubernetes certificate-store contracts, server service overrides,
  certificate validation hooks, the alarm random source, and node-manager overrides. Further errors
  may become visible once these declaration errors are resolved.
- Editing blocker: two successful editor patches to `src/KubernetesSecretCertificateStore.cs` changed
  the editor buffer but not disk. A terminal check still reports `Task<X509Certificate2Collection>`
  where the buffer contains `Task<CertificateCollection>`. Do not trust builds of that slice until
  the buffer/disk mismatch is resolved. No stack source was changed.

## Historical resume notes (2026-09-17)

These notes describe the initial migration state, not the current next actions. Use the current
execution checklist and September 25 qualification sections above when resuming work.

Open the PLC repository as the active workspace and reconcile the pending Kubernetes store edit
before continuing. Preserve all in-progress changes and the baseline TRX. Continue from the current
compiler log; do not reapply the whole migration or discard the existing test assertions.

The isolated restore command used during this session was:

```powershell
dotnet restore opcplc.sln --source https://www.nuget.org/api/v2/ --packages tests/TestResults/stack-v2-migration/packages -p:NuGetAudit=false
```

`NuGetAudit=false` was supplied only to local connectivity/build probes, not committed to project
configuration. Dependency auditing and restores using the actual CI feeds remain release gates.

The model project still needs solution registration and packaging checks. Old generated C# is excluded
from compilation but retained on disk for comparison; remove it only after validating generated model
identity. The migration analyzer/runtime shim is still present and must be removed before release.

## Stack issue filing - 2026-09-24

The entries below record validation of retained SDK red/green TRX evidence and current repair diffs,
not fresh full-suite execution. Tested base: `138086863bc69e8986ad4e997ad5d3872e9626d0`.
GitHub master checked: `a2539cb53dd1666cef856f33812d7a8830d68909`; the ten affected source paths
were unchanged from the tested base. Older migration status above is historical.

| Finding | Validation / duplicate outcome | Verified GitHub issue |
| --- | --- | --- |
| D01: optional initialization erases derived mandatory children | Six failing SDK regressions plus three passing controls; repaired fixture 9/9. Related #1552 and #521 are different behaviors. | [#4525](https://github.com/OPCFoundation/UA-.NETStandard/issues/4525) |
| D02: copied children retain source parents | Two failing identity tests, one passing external-parent control; repaired 3/3. Historical #3346 differs. | [#4526](https://github.com/OPCFoundation/UA-.NETStandard/issues/4526) |
| D03: security validation bypasses scoped stores | Direct custom-provider failure with passing Directory control; SDK provider slice 22/22 after repair. No duplicate found. | [#4527](https://github.com/OPCFoundation/UA-.NETStandard/issues/4527) |
| D04: startup/cold-cache key loading bypasses providers | Startup failures and Directory control verified; cold-path bypass source-confirmed with repaired coverage. No duplicate found. | [#4528](https://github.com/OPCFoundation/UA-.NETStandard/issues/4528) |
| D05: provisioning/reuse/deletion bypasses scoped stores | Independent before 1/5, repaired 5/5 including missing-key refusal. No duplicate found. | [#4529](https://github.com/OPCFoundation/UA-.NETStandard/issues/4529) |
| D06: existing-key CSR bypasses own-store provider | Independent CSR failure plus Directory control; repaired 2/2, provider invoked and nonempty DER prefix checked. | [#4530](https://github.com/OPCFoundation/UA-.NETStandard/issues/4530) |
| D07: TrustList bypasses scoped providers | Before 0/3, after 3/3; read/lifetime/failure checks verified; added injection boundary caveat disclosed. | [#4531](https://github.com/OPCFoundation/UA-.NETStandard/issues/4531) |
| D08: partial definition-only load discards valid types | Before 0/1; repaired fixture 19/19. Incomplete result remains false; valid type becomes available. | [#4532](https://github.com/OPCFoundation/UA-.NETStandard/issues/4532) |
| D09: runtime resolver omits derivable structure metadata | Before 0/1; repaired fixture 16/16. Missing metadata is completed on a clone, not overwritten in the source. | [#4533](https://github.com/OPCFoundation/UA-.NETStandard/issues/4533) |
| D10: queued publish-message tail becomes Idle | Before 0/2, repaired 2/2; neighboring 220/220 per TFM and retained full PLC 913/913. Delayed delivery, not proven loss. | [#4534](https://github.com/OPCFoundation/UA-.NETStandard/issues/4534) |

All ten reports were created individually and read back to verify their complete title/body.
No production code or candidate stack fix was changed during filing; no tests were rerun.
The reports disclose independent-before prerequisites and source-only versus runtime evidence.
