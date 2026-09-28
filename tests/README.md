# OPC PLC server tests
This project contains integration tests.

The test fixture runs an instance of the OPC PLC Server per test class, as a background thread, and performs test from the client side.

The Server is instrumented with mocks for time-related objects and methods (DateTime.Now, Timers) so
that time can be controlled programmatically.

Package mode is the default for OPC UA 2.0. To use a repaired sibling `UA-.NETStandard`
checkout explicitly, pass `-p:UseLocalOpcUaStack=true`. See the
[OPC UA 2.0 migration notes](../docs/opc-ua-v2-migration.md) for package-feed and local-source setup.

Model source generation is test-only: the test assembly and BoilerModel1 project provide independent
reference identities, states, and wire codecs. The PLC server imports NodeSet2 XML and builds runtime
codecs through the SDK instead of referencing these generated model classes.

