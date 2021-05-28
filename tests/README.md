# OPC PLC server tests
This project contains integration tests.

The test fixture runs an instance of the OPC PLC Server per test class, as a background thread, and performs test from the client side.

The Server is instrumented with mocks for time-related objects and methods (DateTime.Now, Timers) so
that time can be controlled programmatically.

Tests can be run directly with no configuration required.

