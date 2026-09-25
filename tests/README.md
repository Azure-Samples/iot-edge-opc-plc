# OPC PLC server tests
This project contains integration tests.

The test fixture runs an instance of the OPC PLC Server per test class, as a background thread, and performs test from the client side.

The Server is instrumented with mocks for time-related objects and methods (DateTime.Now, Timers) so
that time can be controlled programmatically.

The runtime-model migration currently uses the repaired sibling UA-.NETStandard checkout by default.
See [the migration checkpoint](../docs/opc-ua-v2-migration.md#current-runtime-model-checkpoint-2026-09-23)
for source-mode prerequisites and validation commands.

Model source generation is test-only: the test assembly and BoilerModel1 project provide independent
reference identities, states, and wire codecs. The PLC server imports NodeSet2 XML and builds runtime
codecs through the SDK instead of referencing these generated model classes.

