# OPC PLC server tests
This project contains integration tests.

The test fixture runs an instance of the OPC PLC Server per test class, as a background thread, and performs test from the client side.

The Server is instrumented with mocks for time-related objects and methods (DateTime.Now, Timers) so
that time can be controlled programmatically.

Package mode is the default for OPC UA 2.0. Debug automatically uses matching `.Debug` SDK packages,
including the model generator; Release uses the ordinary IDs. Debug packages require authenticated
GitHub Packages access or an approved mirror; CI uses the `aio-brokers` Azure Artifacts mirror with
its build identity. Restore again when switching build configurations.
To use a repaired sibling `UA-.NETStandard` checkout explicitly, pass `-p:UseLocalOpcUaStack=true`. See the
[OPC UA 2.0 migration notes](../docs/opc-ua-v2-migration.md) for package-feed and local-source setup.

Model source generation is test-only: the test assembly and BoilerModel1 project provide independent
reference identities, states, and wire codecs. The PLC server imports NodeSet2 XML and builds runtime
codecs through the SDK instead of referencing these generated model classes.

The shared client fixture creates `ManagedSession` and exposes `ISession`, with automatic
reconnection and asynchronous teardown. Its reconnect policy fails promptly on rejected fixed credentials
while preserving the SDK defaults for transient connection failures.
Boiler tests discover types with `DefaultComplexTypeSystemFactory`,
verify runtime structures, and then register generated reference codecs. This needs only the `Client`
package, not the optional Reflection.Emit `Client.ComplexTypes` package.

Shared monitoring and throughput helpers use the native V2 subscription manager and typed notification
batches, wait for monitored-item creation, and dispose subscriptions asynchronously. They retain payloads
only with notification pooling disabled. Generic reads and method calls use native `Variant` conversions
and collections; dedicated classic-API tests and heterogeneous object-valued assertions remain supported.

