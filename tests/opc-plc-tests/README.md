# OPC PLC server tests
This project contains integration tests.

The test fixture runs an instance of the OPC PLC Server per test class, as a background thread, and performs test from the client side.

The Server is instrumented with mocks for time-related objects and methods (DateTime.Now, Timers) so
that time can be controlled programmatically.

Tests can be run directly with no configuration required.

On Windows, you might see the following exception:
```
OneTimeSetUp: Opc.Ua.ServiceResultException : Error establishing a connection: Error received from remote host: Could not verify security on OpenSecureChannel request.
```

In that case, navigate to the iot-edge-opc-plc\tests\opc-plc-tests\bin\Debug\netcoreapp3.1\pki\own\private directory. Double-click on the certificate generated in that folder and import it to the Local Machine store (leaving all settings to default, i.e. empty password). The tests should now be able to run.

