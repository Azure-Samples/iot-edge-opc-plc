---
page_type: sample
description: "Industrial IoT - Sample OPC UA server that generates random data and anomalies."
languages:
- csharp
products:
- azure
- azure-iot-hub
urlFragment: azure-iot-sample-opc-ua-server
---

# OPC PLC server
Implements an OPC-UA server with different nodes generating random data, anomalies and configuration of user defined nodes.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fiot-edge-opc-plc%2Fmain%2Ftools%2Ftemplates%2Fazuredeploy.opcplc.aci.json)

* After deployment, the OPC PLC server will be available at `opc.tcp://<NAME>.<REGION>.azurecontainer.io:50000`
* See [region limits](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-region-availability#availability---general)

## Disclaimer
The OPC PLC is a sample server. Although it undergoes regular vulnerability scans and prompt patching, we do not recommend deploying it in a production environment due to potential security risks.

## Features
The following nodes are part of the PLC simulation:
- Alternating boolean
- Random signed 32-bit integer
- Random unsigned 32-bit integer
- Sine wave with a spike anomaly
- Sine wave with a dip anomaly
- Value showing a positive trend
- Value showing a negative trend
- Value having periodical good, bad and uncertain statuses (slow changing - 10 s by default)
- Value having periodical good, bad and uncertain statuses (fast changing - 1 s by default)

By default everything is enabled, use command line options to disable certain anomaly or data generation features.
Additionally to those nodes with simulated data, a JSON configuration file allows nodes to be created as specified. Finally, the simulation supports a number of nodes of specific types that can change at a configurable rate.

## Getting Started
### Prerequisites
The implementation is based on .NET Core so it is cross-platform. The recommended hosting environment is Docker.

### Quickstart
A Docker container of the component is hosted in the Microsoft Container Registry (MCR) and can be pulled by:
~~~console
docker pull mcr.microsoft.com/iotedge/opc-plc:<See version.json>
~~~

The tags of the container match the tags of this repository and the containers are available for Windows and Linux.

Sample start command for Docker:
~~~console
docker run --rm -it -p 50000:50000 -p 8080:8080 --name opcplc mcr.microsoft.com/iotedge/opc-plc:latest --pn=50000 --autoaccept --sph --sn=5 --sr=10 --st=uint --fn=5 --fr=1 --ft=uint --gn=5
~~~

Sample start command for Windows:
~~~console
dotnet opcplc.dll --pn=50000 --at X509Store --autoaccept --sph --sn=5 --sr=10 --st=uint --fn=5 --fr=1 --ft=uint --gn=5
~~~

Note: Make sure that your OPC UA client uses security policy `Basic256Sha256` and message security mode `Sign & Encrypt` to connect.

## User node configuration via JSON configuration file
If the module (application) is started with the argument `--nodesfile` then the specified JSON configuration file is loaded.
Nodes defined in the JSON file will be published by the server. This enables another OPC-UA client application to set the state/value of the node. Please note that nodes specified in the JSON file are NOT part of the simulation. They remain visible in an unchanged state until an OPC UA client changes their status.

The following command shows how to use a configuration file on Windows:
~~~console
dotnet opcplc.dll --at X509Store --nodesfile nodesfile.json
~~~
Here's a sample node configuration file:
~~~json
{
  "Folder": "MyTelemetry",
  "FolderList": [
    {
      "Folder": "Directory",
      "NodeList": [
        {
          "NodeId": "ChildNode"
        },
      ]
    }
  ],
  "NodeList": [
    {
      "NodeId": 1023,
      "Name": "ActualSpeed",
      "Description": "Rotational speed"
    },
    {
      "NodeId": "aRMS"
    },
    {
      "NodeId": "1025",
      "Name": "DKW",
      "DataType": "Float",
      "ValueRank": -1,
      "AccessLevel": "CurrentReadOrWrite",
      "Description": "Diagnostic characteristic value"
    }
  ]
}
~~~
- Folder: Defines the name of the folder under which the user specified nodes should be created. This folder is created below the root of the OPC UA server.
- FolderList: Defines the list of child folders, which will be published by the emulated server. (Optional)
- NodeList: Defines the list of nodes, which will be published by the emulated server. Nodes specified in the list can be browsed and changed by OPC UA applications. This enables developers to easily implement and test OPC UA client applications.
- NodeId: Specifies the identifier of the node and is required. This value can be a decimal or string value. Every other JSON type is converted to a string identifier.
- Name: The display name of the tag. If not set it will be set to the NodeId. (Optional)
- DataType: The OPC UA valid type. It specifies one of types defined by BuiltInType. If an invalid type is specified or if it is omitted it defaults to 'Int32'. (Optional)
- ValueRank: As defined by type ValueRanks. If omitted it will be set to the value '-1' (Scalar). (Optional)
- AccessLevel: Specifies one of access levels defined by type AccessLevels. If an invalid access level is specified or if it is omitted it defaults to 'CurrentReadOrWrite'. (Optional)
- Description: Description of the node. If not set it will be set to the NodeId. (Optional)

## Slow and fast changing nodes
A number of changing nodes can be simulated with the following options. The nodes are categorized into slow and fast only for convenience.
- sn: Number of slow nodes (default 1)
- sr: Rate in seconds at which to change the slow nodes (uint, default every 10 s)
- st: Data type for slow nodes (UInt|Double|Bool|UIntArray, case insensitive)
- stl: lower bound of data type of slow nodes (UInt|Double types only, defaults to minimium value of the type in C# with exception of Double where it defaults to 0.0)
- stu: upper bound of data type of slow nodes (UInt|Double types only, defaults to maximum value of the type in C#)
- str: randomization of slow nodes value (UInt|Double types only, defaults to false)
- sts: step or increment size of slow nodes value (UInt|Double types only, defaults to 1)
- fn: Number of fast nodes (default 1)
- fr: Rate in seconds at which to change the fast nodes (uint, default every 1 s)
- vfr: Rate in milliseconds at which to change the fast nodes (uint, default every 1000 ms)
- ft: Data type for fast nodes (UInt|Double|Bool|UIntArray, case insensitive)
- ftl: lower bound of data type of fast nodes (UInt|Double types only, defaults to minimium value of the type in C# with exception of Double where it defaults to 0.0)
- ftu: upper bound of data type of fast nodes (UInt|Double types only, defaults to maximum value of the type in C#)
- ftr: randomization of fast nodes value (UInt|Double types only, defaults to false)
- fts: step or increment size of fast nodes value (UInt|Double types only, defaults to 1)

### Data types
- UInt: Increases by 1
- Double: Increases by 0.1
- Bool: Alternates
- UIntArray: 32 values that increase by 1

## OPC Publisher file (pn.json)
The options `--sph` and `--sp` show and dump an OPC Publisher configuration file (default name: `pn.json`) that matches the configuration. In addition, a web server hosts the file on a configurable port (`--wp`, default 8080): e.g. http://localhost:8080/pn.json
Additionally, you can set the configuration file name via the option `--spf`.

## Complex type (Boiler #1)
Adds a simple boiler to the address space.

Features:
- BoilerStatus is a complex type that shows: Temperature, Pressure and HeaterState
- Method to turn heater on/off
- When the heater is on, the bottom temperature increases by 1 degree/s, the top temperature is always 5 degrees less than the bottom one
- Pressure is calculated as 100000 + bottom temperature

## Boiler #2 derived from the Device Information (DI) companion spec
Adds a configurable boiler that exposes DI properties such as AssetId (ITagNameplate, IVendorNameplate) and DeviceHealth.

Features:
- Configure and expose: BaseTemperature, TargetTemperature, TemperatureChangeSpeed, CurrentTemperature, HeaterState, Overheated, OverheatThresholdTemperature
- Method to switch heater on/off
- The MaintenanceInterval and OverheatInterval both emit events

Simulation details:
- When the heater is off, the CurrentTemperature falls to the BaseTemperature and the heater is switched on
- When the heater is on, the CurrentTemperature raises to the TargetTemperature and the heater is switched off
- Emits a "MaintenanceRequiredAlarmType" event when DeviceHealth is MAINTENANCE_REQUIRED
- The OverheatInterval increases the temperature to 10 degrees above the OverheatThresholdTemperature, the heater is switched off and:
  - Emits a "CheckFunctionAlarmType" event when DeviceHealth is CHECK_FUNCTION
  - Emits a "FailureAlarmType" event when DeviceHealth is FAILURE
  - Emits an "OffSpecAlarmType" event when DeviceHealth is OFF_SPEC

DeviceHealth (DeviceHealthEnumeration) details:
- NORMAL: BaseTemperature <= CurrentTemperature <= TargetTemperature
- CHECK_FUNCTION: TargetTemperature < CurrentTemperature < OverheatThresholdTemperature
- FAILURE: Temperature >= OverheatThresholdTemperature
- OFF_SPEC: CurrentTemperature < BaseTemperature or CurrentTemperature > OverheatThresholdTemperature + 5
- MAINTENANCE_REQUIRED: Triggered by MaintenanceInterval

| Temperature                        | DeviceHealth   |
| ---------------------------------- | -------------- |
| < BaseTemperature                  | OFF_SPEC       |
| >= BaseTemperature                 | NORMAL         |
| <= TargetTemperature               | NORMAL         |
| > TargetTemperature                | CHECK_FUNCTION |
| >= OverheatThresholdTemperature    | FAILURE        |
| > OverheatThresholdTemperature + 5 | OFF_SPEC       |


## Simple Events
The option `--ses` enables simple events from the [quickstart sample](https://github.com/OPCFoundation/UA-.NETStandard-Samples/tree/master/Workshop/SimpleEvents) from OPC Foundation.

Simple Events defines four new event types. _SystemCycleStatusEventType_ is inherited from the [_SystemEventType_](https://reference.opcfoundation.org/v104/Core/ObjectTypes/SystemEventType/) and
_SystemCycleStartedEventType_, _SystemCycleAbortedEventType_, _SystemCycleFinishedEventType_ from _SystemCycleStatusEventType_.

Every 3000 ms a new _SystemCycleStartedEventState_ is triggered. (The other event types are not used.)
Two _messages_ are generated with a counter "The system cycle '\{counter}' has started." for each event, one has severity 1 and the other one severity 2.

A structure of type _CycleStepDataType_ is added to _SystemCycleStartedEventState_ event. The values in that
structure is hard coded to Name: Step 1 and Duration: 1000.


## Alarms and Condition
The option `--alm` enables Alarm and Condition [quickstart sample](https://github.com/OPCFoundation/UA-.NETStandard-Samples/tree/master/Workshop/AlarmCondition) from OPC Foundation.

It creates a hierarchical folder structure from _Server_, starting with _Green_ and _Yellow_. The leaf nodes
_SouthMotor_, _WestTank_, _EastTank_ and _NorthMotor_ are sources for the alarms.

The alarms are of different types:
- Bronze - [TripAlarmType](https://reference.opcfoundation.org/v104/Core/ObjectTypes/TripAlarmType/)
- Gold - [ExclusiveDeviationAlarmType](https://reference.opcfoundation.org/v104/Core/ObjectTypes/ExclusiveDeviationAlarmType/)
- Silver - [NonExclusiveLevelAlarmType](https://reference.opcfoundation.org/v104/Core/ObjectTypes/NonExclusiveLevelAlarmType/)
- OnlineState - [DialogConditionType](https://reference.opcfoundation.org/v104/Core/ObjectTypes/DialogConditionType/)

All these alarms will update on a regular interval. It is also possible to _Acknowledge_, _Confirm_ and add _Comment_ to them.

This simulation also emits two types of system events: [_SystemEventType_](https://reference.opcfoundation.org/v104/Core/ObjectTypes/SystemEventType/)
 and [_AuditEventType_](https://reference.opcfoundation.org/v104/Core/ObjectTypes/AuditEventType/), every 1000 ms.

## Deterministic Alarms testing
The option `--dalm=<file>` enables deterministic testing of Alarms and Conditions.

More information about this feature can be found [here](deterministic-alarms.md).

## Chaos mode
Randomly injects errors, closes subscriptions or sessions, expires subscriptions and more. You can use it to test the resiliency of OPC UA clients. To enable start the server with the option `--chaos=True`.

## Other features
- Node with special characters in name and NodeId
- Node with long ID (3950 bytes)
- Nodes with large values (10/50 kB string, 100 kB StringArray, 200 kB ByteArray)
- Nodes for testing all datatypes, arrays, methods, permissions, etc. The ReferenceNodeManager of the [OPC UA .NET reference stack](https://github.com/OPCFoundation/UA-.NETStandard) is used for this purpose.
- Limit the number of updates of Slow and Fast nodes. Update the values of the `SlowNumberOfUpdates` and `FastNumberOfUpdates` configuration nodes in the `OpcPlc/SimulatorConfiguration` folder to:
  - `< 0` (default): Slow and Fast nodes are updated indefinitely
  - `0`: Slow and Fast nodes are not updated
  - `> 0`: Slow and Fast nodes are updated the given number of times, then they stop being updated (the value of the configuration node is decremented at every update).
- Nodes with deterministic random GUIDs as node IDs: `--gn=<number_of_nodes>`
- Nodes of type NodeId and ExpandedNodeId with IdType:
  - String
  - Numeric
  - GUID
  - Opaque
- Nodes with 1 kB (ByteString) values: `--vf1k`. The first byte cycles from 0 to 255 in a configurable rate in ms: `--vf1kr`. The values are deterministic but scrambled to ensure that they are not efficiently compressed.
- Load binary *.PredefinedNodes.uanodes file(s) compiled from an XML NodeSet: `--unf=<PredefinedNodes_uanodes>`
- Load *.NodeSet2.xml file(s): `--ns2=<NodeSet2_xml>`
- Node that shows working set memory consumption in MB: Root/Objects/OpcPlc/Telemetry/Special/WorkingSetMB

## OPC UA Methods
| Name                 | Description                                    | Prerequisite                                   |
| -------------------- | ---------------------------------------------- | ---------------------------------------------- |
| ResetTrend           | Reset the trend values to their baseline value | Generate positive or negative trends activated |
| ResetStepUp          | Resets the StepUp counter to 0                 | Generate data activated                        |
| StopStepUp           | Stops the StepUp counter                       | Generate data activated                        |
| StartStepUp          | Starts the StepUp counter                      | Generate data activated                        |
| StopUpdateSlowNodes  | Stops the increase of value of slow nodes      | slow nodes activated                           |
| StopUpdateFastNodes  | Stops the increase of value of fast nodes      | fast nodes activated                           |
| StartUpdateSlowNodes | Start the increase of value of slow nodes      | slow nodes activated                           |
| StartUpdateFastNodes | Start the increase of value of fast nodes      | fast nodes activated                           |

## NuGet
- The OPC PLC build generates a NuGet package that can be used to add the OPC PLC server to your own project, e.g. for unit tests
- Sample base class for unit tests: `./samples/OpcPlcBase.cs`
- Sample unit test file that uses the base class: `./samples/OpcUaUnitTests.cs`
- Sample NuGet config to consume a local package (needs to be next to the solution file): `./samples/nuget.config`
- Sample project file that shows how to import the local OPC PLC nuget package: `./samples/OpcUaUnitTests.prj`
- Note: The NuGet package is not published to NuGet.org

## Build
The build scripts are for Azure DevOps and the container build is done in ACR. To use your own ACR you need to:

- Create a **service connection** called azureiiot to the subscription/resource group in which your ACR is located
- Set a variable called **BUILD_REGISTRY** with the name of your Azure Container Registry

Using `<reporoot>/tools/scripts/build.ps1` you can also build with Docker Desktop locally. The sample below builds a debug container and is started at the root of the repository:
~~~powershell
.\tools\scripts\build.ps1 -Path . -Debug
~~~

If you want to build using Docker yourself, it is a bit more complicated, since the dockerfile is generated by the scripts.
So first run the `build.ps1` script as above, then locate the dockerfile for your configuration and target runtime under `<reporoot>/src/bin/publish`.
Next, make your modifications and publish the opc-plc project in Visual Studio. Ensure that you have chosen "Self-Contained" as "Deployment Mode" and the correct "Target runtime" in the Visual Studio Publish configuration. Finally, run the `docker build` command in the folder you published to using the dockerfile of your configuration and target runtime.

Building with PowerShell is even simpler. Here's an example for a linux-x64 build:
~~~powershell
.\tools\scripts\docker-source.ps1 .\src
docker build -f .\src\bin\publish\Release\linux-x64\Dockerfile.linux-amd64 -t iotedge/opc-plc .\src\bin\publish\Release\linux-x64
~~~

## Notes
X.509 certificates:

* Running on Windows natively, you cannot use an application certificate store of type `Directory`, since the access to the private key will fail. Use the option `--at X509Store` in this case.
* Running as Linux Docker container, you can map the certificate stores to the host file system by using the Docker run option `-v <hostpkidirectory>:/app/pki`. This will make the certificate persistent over starts.
* Running as Linux Docker container using an X509Store for the application certificate, you need to use the Docker run option `-v x509certstores:/root/.dotnet/corefx/cryptography/x509stores` and the application option `--at X509Store`
* When running in kubernetes context, use option `--at FlatDirectory`. This enables the OPC UA server to consume both public key and private key certificates directly from the /app/pki/own/ path without expecting the `certs` and `private` subdirectories. Furthermore, certificates of type .crt and .key are accepted.

User certificate-based authentication:

* The OPC PLC server supports X.509 certificate-based user authentication in addition to anonymous and username/password authentication.
* Use `--tup` or `--trustedusercertstorepath` to specify the path where trusted user certificates are stored (default: `pki/trusted-user`).
* Use `--uip` or `--userissuercertstorepath` to specify the path where issuer certificates for user certificate chain validation are stored (default: `pki/issuer-user`).
* Add trusted user certificates using `--tuf` or `--addtrustedusercertfile` (file path) or `--tub` or `--addtrustedusercertbase64` (base64 string). Multiple certificates can be provided as comma-separated values.
* Add user issuer certificates using `--uif` or `--adduserissuercertfile` (file path) or `--uib` or `--adduserissuercertbase64` (base64 string). Multiple certificates can be provided as comma-separated values.

Creating a user certificate with OpenSSL:

To create a self-signed user certificate for testing purposes, use the following OpenSSL commands:

~~~powershell
# Generate a self-signed certificate with private key in .der format
openssl req -x509 -newkey rsa:2048 -keyout user.pem -out user.der  -outform der -sha256 -days 365 -nodes `
	-subj "/CN=test user" `
	-addext "keyUsage=critical, nonRepudiation, digitalSignature, keyEncipherment, dataEncipherment, keyCertSign" `
	-addext "extendedKeyUsage = critical, clientAuth" `
	-addext "basicConstraints=CA:FALSE"

# Create a PFX file (optional, for client use)
openssl pkcs12 -export -inkey user.pem -in user.der -out user.pfx -password pass:TestP@ssw0rd123
~~~

After generating the certificate, add the public key to the trusted user certificate store:
~~~powershell
dotnet opcplc.dll --tuf user.der
~~~

Alternatively, you can manually copy the certificate to the trusted user certificate store directory:
~~~powershell
# Create the directory if it doesn't exist
mkdir -p pki/trusted-user/certs

# Copy the certificate file
cp user.der pki/trusted-user/certs/
~~~

For Docker, mount the PKI directory and copy the certificate:
~~~powershell
# Copy certificate to your local PKI directory
mkdir -p ./pki/trusted-user/certs
cp user.der ./pki/trusted-user/certs/

# Run the container with the PKI directory mounted
docker run --rm -it -p 50000:50000 -v $(pwd)/pki:/app/pki mcr.microsoft.com/iotedge/opc-plc:latest
~~~

Client configuration for user certificate authentication:

When connecting to the OPC PLC server using certificate-based user authentication, OPC UA clients must provide both the public certificate and private key. The format depends on the client implementation:

* **Separate files**: Some clients require the public certificate (`.der` or `.crt` file) and private key (`.pem` or `.key` file) as separate files.
* **PFX/PKCS#12 format**: Other clients accept a single `.pfx` (or `.p12`) file that contains both the public certificate and private key, along with the password used to protect the private key.

Note: Only the public certificate (`.der` or `.crt`) needs to be added to the server's trusted user certificate store using the `--tuf` option. The private key remains with the client and is used to prove the user's identity during session establishment.

Disabling certificate authentication:

* Certificate authentication can be disabled using the `--dca` or `--disablecertauth` flag.

## Resources
- [The OPC Foundation OPC UA .NET reference stack](https://github.com/OPCFoundation/UA-.NETStandard)

## Command-line reference
```
Usage: dotnet opcplc.dll [<options>]

OPC UA PLC for different data simulation scenarios.
To exit the application, press CTRL-C while it's running.

Use the following format to specify a list of strings:
"<string 1>,<string 2>,...,<string n>"
or if one string contains commas:
""<string 1>","<string 2>",...,"<string n>""

Options:
      --lf, --logfile=VALUE  the filename of the logfile to use.
                               Default: './hostname-port-plc.log'
      --lt, --logflushtimespan=VALUE
                             the timespan in seconds when the logfile should be
                               flushed.
                               Default: 00:00:30 sec
      --ll, --loglevel=VALUE the loglevel to use (allowed: critical, error,
                               warn, info, debug, trace).
                               Default: info
      --sc, --simulationcyclecount=VALUE
                             count of cycles in one simulation phase.
                               Default: 50 cycles
      --ct, --cycletime=VALUE
                             length of one cycle time in milliseconds.
                               Default: 100 msec
      --ei, --eventinstances=VALUE
                             number of event instances.
                               Default: 0
      --er, --eventrate=VALUE
                             rate in milliseconds to send events.
                               Default: 1000
      --pn, --portnum=VALUE  the server port of the OPC server endpoint.
                               Default: 50000
      --op, --path=VALUE     the endpoint URL path part of the OPC server
                               endpoint.
                               Default: ''
      --ph, --plchostname=VALUE
                             the fully-qualified hostname of the PLC.
                               Default: hostname
      --ol, --opcmaxstringlen=VALUE
                             the max length of a string OPC can transmit/
                               receive.
                               Default: 4194304
      --otlpee, --otlpendpoint=VALUE
                             the endpoint URI to which the OTLP exporter is
                               going to send information.
                               Default: ''
      --otlpei, --otlpexportinterval=VALUE
                             the interval for exporting OTLP information in
                               seconds.
                               Default: 60
      --otlpep, --otlpexportprotocol=VALUE
                             the protocol for exporting OTLP information.
                               (allowed values: grpc, protobuf).
                               Default: grpc
      --otlpub, --otlpublishmetrics=VALUE
                             how to handle metrics for publish requests.
                               (allowed values: disable=Always disabled, enable=
                               Always enabled, auto=Auto-disable when sessions >
                                40 or monitored items > 500).
                               Default: auto
      --lr, --ldsreginterval=VALUE
                             the LDS(-ME) registration interval in ms. If 0,
                               then the registration is disabled.
                               Default: 0
      --aa, --autoaccept     all certs are trusted when a connection is
                               established.
                               Default: False
      --drurs, --dontrejectunknownrevocationstatus
                             Don't reject chain validation with CA certs with
                               unknown revocation status, e.g. when the CRL is
                               not available or the OCSP provider is offline.
                               Default: False
      --ut, --unsecuretransport
                             enables the unsecured transport.
                               Default: False
      --to, --trustowncert   the own certificate is put into the trusted
                               certificate store automatically.
                               Default: False
      --msec, --maxsessioncount=VALUE
                             maximum number of parallel sessions.
                               Default: 100
      --mset, --maxsessiontimeout=VALUE
                             maximum time that a session can remain open
                               without communication in milliseconds.
                               Default: 3600000
      --msuc, --maxsubscriptioncount=VALUE
                             maximum number of subscriptions.
                               Default: 100
      --mqrc, --maxqueuedrequestcount=VALUE
                             maximum number of requests that will be queued
                               waiting for a thread.
                               Default: 2000
      --at, --appcertstoretype=VALUE
                             the own application cert store type.
                               (allowed values: Directory, X509Store,
                               FlatDirectory)
                               Default: 'Directory'
      --ap, --appcertstorepath=VALUE
                             the path where the own application cert should be
                               stored.
                               Default (depends on store type):
                               X509Store: 'CurrentUser\UA_MachineDefault'
                               Directory: 'pki\own'
                               FlatDirectory: 'pki\own'
      --tp, --trustedcertstorepath=VALUE
                             the path of the trusted cert store.
                               Default 'pki\trusted'
      --rp, --rejectedcertstorepath=VALUE
                             the path of the rejected cert store.
                               Default 'pki\rejected'
      --ip, --issuercertstorepath=VALUE
                             the path of the trusted issuer cert store.
                               Default 'pki\issuer'
      --tup, --trustedusercertstorepath=VALUE
                             the path of the trusted user cert store.
                               Default 'pki\trusted-user'
      --uip, --userissuercertstorepath=VALUE
                             the path of the user issuer cert store.
                               Default 'pki\issuer-user'
      --csr                  show data to create a certificate signing request.
                               Default 'False'
      --ab, --applicationcertbase64=VALUE
                             update/set this application's certificate with the
                               certificate passed in as base64 string.
      --af, --applicationcertfile=VALUE
                             update/set this application's certificate with the
                               specified file.
      --pb, --privatekeybase64=VALUE
                             initial provisioning of the application
                               certificate (with a PEM or PFX format) requires
                               a private key passed in as base64 string.
      --pk, --privatekeyfile=VALUE
                             initial provisioning of the application
                               certificate (with a PEM or PFX format) requires
                               a private key passed in as file.
      --cp, --certpassword=VALUE
                             the optional password for the PEM or PFX or the
                               installed application certificate.
      --tb, --addtrustedcertbase64=VALUE
                             adds the certificate to the application's trusted
                               cert store passed in as base64 string (comma
                               separated values).
      --tf, --addtrustedcertfile=VALUE
                             adds the certificate file(s) to the application's
                               trusted cert store passed in as base64 string (
                               multiple comma separated filenames supported).
      --ib, --addissuercertbase64=VALUE
                             adds the specified issuer certificate to the
                               application's trusted issuer cert store passed
                               in as base64 string (comma separated values).
      --if, --addissuercertfile=VALUE
                             adds the specified issuer certificate file(s) to
                               the application's trusted issuer cert store (
                               multiple comma separated filenames supported).
      --tub, --addtrustedusercertbase64=VALUE
                             adds the certificate to the application's trusted
                               user cert store passed in as base64 string (
                               comma separated values).
      --tuf, --addtrustedusercertfile=VALUE
                             adds the certificate file(s) to the application's
                               trusted user cert store (multiple comma
                               separated filenames supported).
      --uib, --adduserissuercertbase64=VALUE
                             adds the specified issuer certificate to the
                               application's user issuer cert store passed in
                               as base64 string (comma separated values).
      --uif, --adduserissuercertfile=VALUE
                             adds the specified issuer certificate file(s) to
                               the application's user issuer cert store (
                               multiple comma separated filenames supported).
      --rb, --updatecrlbase64=VALUE
                             update the CRL passed in as base64 string to the
                               corresponding cert store (trusted or trusted
                               issuer).
      --uc, --updatecrlfile=VALUE
                             update the CRL passed in as file to the
                               corresponding cert store (trusted or trusted
                               issuer).
      --rc, --removecert=VALUE
                             remove cert(s) with the given thumbprint(s) (comma
                               separated values).
      --daa, --disableanonymousauth
                             flag to disable anonymous authentication.
                               Default: False
      --dua, --disableusernamepasswordauth
                             flag to disable username/password authentication.
                               Default: False
      --dca, --disablecertauth
                             flag to disable certificate authentication.
                               Default: False
      --au, --adminuser=VALUE
                             the username of the admin user.
                               Default: sysadmin
      --ac, --adminpassword=VALUE
                             the password of the administrator.
                               Default: demo
      --du, --defaultuser=VALUE
                             the username of the default user.
                               Default: user1
      --dc, --defaultpassword=VALUE
                             the password of the default user.
                               Default: password
      --alm, --alarms        add alarm simulation to address space.
                               Default: False
      --ses, --simpleevents  add simple events simulation to address space.
                               Default: False
      --dalm, --deterministicalarms=VALUE
                             add deterministic alarm simulation to address
                               space.
                               Provide a script file for controlling
                               deterministic alarms.
      --sp, --showpnjson     show OPC Publisher configuration file using IP
                               address as EndpointUrl.
                               Default: False
      --sph, --showpnjsonph  show OPC Publisher configuration file using
                               plchostname as EndpointUrl.
                               Default: False
      --spf, --showpnfname=VALUE
                             filename of the OPC Publisher configuration file
                               to write when using options sp/sph.
                               Default: pn.json
      --wp, --webport=VALUE  web server port for hosting OPC Publisher
                               configuration file.
                               Default: 8080
      --cdn, --certdnsnames=VALUE
                             add additional DNS names or IP addresses to this
                               application's certificate (comma separated
                               values; no spaces allowed).
                               Default: DNS hostname
      --chaos                run the server in Chaos mode. Randomly injects
                               errors, closes sessions and subscriptions etc.
                               Default: False
  -h, --help                 show this message and exit
      --b2ts, --boiler2tempspeed=VALUE
                             Boiler #2 temperature change speed in degrees per
                               second.
                               Default: 1
      --b2bt, --boiler2basetemp=VALUE
                             Boiler #2 base temperature to reach when not
                               heating.
                               Default: 10
      --b2tt, --boiler2targettemp=VALUE
                             Boiler #2 target temperature to reach when heating.

                               Default: 80
      --b2mi, --boiler2maintinterval=VALUE
                             Boiler #2 required maintenance interval in seconds.

                               Default: 300
      --b2oi, --boiler2overheatinterval=VALUE
                             Boiler #2 overheat interval in seconds.
                               Default: 120
      --nv, --nodatavalues   do not generate data values.
                               Default: False
      --gn, --guidnodes=VALUE
                             number of nodes with deterministic GUID IDs.
                               Default: 1
      --nd, --nodips         do not generate dip data.
                               Default: False
      --fn, --fastnodes=VALUE
                             number of fast nodes.
                               Default: 1
      --fr, --fastrate=VALUE rate in seconds to change fast nodes.
                               Default: 1
      --ft, --fasttype=VALUE data type of fast nodes (UInt|Double|Bool|
                               UIntArray).
                               Default: UInt
      --ftl, --fasttypelowerbound=VALUE
                             lower bound of data type of fast nodes.
                               Default: min value of node type.
      --ftu, --fasttypeupperbound=VALUE
                             upper bound of data type of fast nodes.
                               Default: max value of node type.
      --ftr, --fasttyperandomization=VALUE
                             randomization of fast nodes value.
                               Default: False
      --fts, --fasttypestepsize=VALUE
                             step or increment size of fast nodes value.
                               Default: 1
      --fsi, --fastnodesamplinginterval=VALUE
                             rate in milliseconds for pn.json client to sample
                               fast nodes.
                               Default: 0
      --vfr, --veryfastrate=VALUE
                             rate in milliseconds to change fast nodes.
                               Default: 1000
      --nn, --nonegtrend     do not generate negative trend data.
                               Default: False
      --ns2, --nodeset2file=VALUE
                             the *.NodeSet2.xml file that contains the nodes to
                               be created in the OPC UA address space (multiple
                               comma separated filenames supported).
      --np, --nopostrend     do not generate positive trend data.
                               Default: False
      --sn, --slownodes=VALUE
                             number of slow nodes.
                               Default: 1
      --sr, --slowrate=VALUE rate in seconds to change slow nodes.
                               Default: 10
      --st, --slowtype=VALUE data type of slow nodes (UInt|Double|Bool|
                               UIntArray).
                               Default: UInt
      --stl, --slowtypelowerbound=VALUE
                             lower bound of data type of slow nodes.
                               Default: min value of node type.
      --stu, --slowtypeupperbound=VALUE
                             upper bound of data type of slow nodes.
                               Default: max value of node type.
      --str, --slowtyperandomization=VALUE
                             randomization of slow nodes value.
                               Default: False
      --sts, --slowtypestepsize=VALUE
                             step or increment size of slow nodes value.
                               Default: 1
      --ssi, --slownodesamplinginterval=VALUE
                             rate in milliseconds for pn.json client to sample
                               slow nodes.
                               Default: 0
      --ns, --nospikes       do not generate spike data.
                               Default: False
      --unf, --uanodesfile=VALUE
                             the binary *.PredefinedNodes.uanodes file that
                               contains the nodes to be created in the OPC UA
                               address space (multiple comma separated
                               filenames supported), use ModelCompiler.cmd <
                               ModelDesign> to compile.
      --nf, --nodesfile=VALUE
                             the filename that contains the list of nodes to be
                               created in the OPC UA address space.
      --vfbs, --veryfastbsnodes=VALUE
                             number of very fast ByteString nodes.
                               Default: 1
      --vfbss, --veryfastbssize=VALUE
                             size in bytes to change very fast ByteString nodes
                               (min. 1).
                               Default: 1024
      --vfbsr, --veryfastbsrate=VALUE
                             rate in ms to change very fast ByteString nodes.
                               Default: 1000
```
