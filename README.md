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

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fiot-edge-opc-plc%2Fmaster%2Ftools%2Ftemplates%2Fazuredeploy.opcplc.aci.json)

* After deployment, the OPC PLC server will be available at `opc.tcp://<NAME>.<REGION>.azurecontainer.io:50000`
* See [region limits](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-region-availability#availability---general)

## Features
The following nodes are part of the PLC simulation:
- Alternating boolean
- Random signed 32-bit integer
- Random unsigend 32-bit integer
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

### Installation

There is no installation required.

### Quickstart

A Docker container of the component is hosted in the Microsoft Container Registry (MCR) and can be pulled by:
~~~
docker pull mcr.microsoft.com/iotedge/opc-plc:<See version.json>
~~~
The tags of the container match the tags of this repository and the containers are available for Windows and Linux. 

## User node configuration via JSON configuration file
If the module (application) is started with the argument **--nodesfile** then the specified JSON configuration file is loaded.
Nodes defined in the JSON file will be published by the server. This enables another OPC-UA client application to set the state/value of the node. Please note that nodes specified in the JSON file are NOT part of the simulation. They remain visible in an unchanged state until an OPC UA client changes their status.
The following command shows how to start the application on a Windows host:
~~~
dotnet opcplc.dll --at X509Store --nodesfile nodesfile.json
~~~
Here's a sample node configuration file:
~~~
{
  "Folder": "MyTelemetry",
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
- NodeList: Defines the list of nodes, which will be published by the emulated server. Nodes specified in the list can be browsed and changed by OPC UA applications. This enables developers to easyly implement and test OPC UA client applications.
- NodeId: Specifies the identifier of the node and is required. This value can be a decimal or string value. Every other JSON type is converted to a string identifier.
- Name: The display name of the tag. If not set it will be set to the NodeId. (Optional)
- DataType: The OPC UA valid type. It specifies one of types defined by BuiltInType. If an invalid type is specified or if it is ommitted it defaults to 'Int32'. (Optional)
- ValueRank: As defined by type ValueRanks. If omitted it will be set to the value '-1' (Scalar). (Optional)
- AccessLevel: Specifies one of access levels defined by type AccessLevels. If an invalid access level is specified or if it is omitted it defaults to 'CurrentReadOrWrite'. (Optional)
- Description: Description of the node. If not set it will be set to the NodeId. (Optional)

## Slow and fast changing nodes
A number of changing nodes can be simulated with the following options. The nodes are categorized into slow and fast only for convenience.
- sn: Number of slow nodes (default 1)
- sr: Rate in seconds at which to change the slow nodes (uint, default every 10 s)
- st: Data type for slow nodes (UInt|Double|Bool|UIntArray, case insensitive)
- fn: Number of fast nodes (default 1)
- fr: Rate in seconds at which to change the fast nodes (uint, default every 1 s)
- ft: Data type for fast nodes (UInt|Double|Bool|UIntArray, case insensitive)

### Data types
- UInt: Increases by 1
- Double: Increases by 0.1
- Bool: Alternates
- UIntArray: 32 values that increase by 1

Sample start command on a Windows host:
~~~
dotnet opcplc.dll --pn=50000 --at X509Store --autoaccept --nospikes --nodips --nopostrend --nonegtrend --nodatavalues --sph --sn=25 --sr=10 --st=uint --fn=5 --fr=1 --ft=uint
~~~

Sample start command for Docker:
~~~
docker run --rm -it -p 50000:50000 -p 8080:8080 --name opcplc mcr.microsoft.com/iotedge/opc-plc:latest --pn=50000 --autoaccept --nospikes --nodips --nopostrend --nonegtrend --nodatavalues --sph --sn=25 --sr=10 --st=uint --fn=5 --fr=1 --ft=uint
~~~

## OPC Publisher file (pn.json)

The options `sph` and `sp` show and dump an OPC Publisher configuration file (default name: `pn.json`) that matches the configuration. In addition, a web server hosts the file on a configurable port (`wp`, default 8080): e.g. http://localhost:8080/pn.json
Additionally, you can set the configuration file name via the option `spf`.

## Complex type (boiler)
 
The option `--ctb` adds a simple boiler to the address space.

Features:
- BoilerStatus is a complex type that shows: temperature, pressure and heater state
- Method to turn heater on/off
- When the heater is on, the bottom temperature increases by 1 degree/s, the top temperature is always 5 degrees less than the bottom one
- Pressure is calculated as 100000 + bottom temperature

## Other features
 
- Node with special characters in name: `--scn`

## Build

The build scripts are for Azure DevOps and the container build is done in ACR. To use your own ACR you need to:

- Create a **service connection** called azureiiot to the subscription/resource group in which your ACR is located
- Set a variable called **BUILD_REGISTRY** with the name of your Azure Container Registry

Using `<reporoot>/tools/scripts/build.ps1` you can also build with Docker Desktop locally. The sample below builds a debug container and is started at the root of the repository:
~~~
.\tools\scripts\build.ps1 -Path . -Debug
~~~

If you want to build using Docker yourself, it is a bit more complicated, since the dockerfile is generated by the scripts.
So first run the `build.ps1` script as above, then locate the dockerfile for your configuration and target runtime under `<reporoot>/src/bin/publish`.
Next, make your modifications and publish the opc-plc project in Visual Studio. Ensure that you have chosen "Self-Contained" as "Deployment Mode" and the 
correct "Target runtime" in the Visual Studio Publish configuration. Finally, run the `docker build` command in the folder you published to using the dockerfile of your configuration and target runtime. 

Building with PowerShell is even simpler. Here's an example for a linux-x64 build:
~~~
.\tools\scripts\docker-source.ps1 .\src
docker build -f .\src\bin\publish\Release\linux-x64\Dockerfile.linux-amd64 -t iotedge/opc-plc .\src\bin\publish\Release\linux-x64
~~~

## Notes

X.509 certificates:

* Running on Windows natively, you cannot use an application certificate store of type `Directory`, since the access to the private key will fail. Use the option `--at X509Store` in this case.
* Running as Linux Docker container, you can map the certificate stores to the host file system by using the Docker run option `-v <hostdirectory>:/appdata`. This will make the certificate persistent over starts.
* Running as Linux Docker container using an X509Store for the application certificate, you need to use the Docker run option `-v x509certstores:/root/.dotnet/corefx/cryptography/x509stores` and the application option `--at X509Store`

## Resources

- [The OPC Foundation OPC UA .NET reference stack](https://github.com/OPCFoundation/UA-.NETStandard)
