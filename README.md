# OPC PLC server
Implements an OPC UA server with different nodes which generate random data and anomalies.

## Features
The following nodes are part of the PLC simulation:
- with alternating boolean
- random signed 32-bit integer
- random unsigend 32-bit integer
- a sine wave with a spike anomaly
- a sine wave with a dip anomaly
- a value showing a positive trend
- a value showing a negative trend

By default everything is enabled, please use command line options to disable certain anomaly or data generation features.

## Getting Started

### Prerequisites

The implementation is based on .NET Core so it is cross-platform and recommended hosting environment is docker.

### Installation

There is no installation required.

### Quickstart

A docker container of the component is hosted in the Microsoft Container Registry and can be pulled by:

docker pull mcr.microsoft.com/iotedge/iot-plc

The tags of the containers match the tags of this repository and the containers are available for Windows amd64, Linux amd64 and Linux ARM32.


## Demo

The [OpcClient](https://github.com/Azure-Samples/iot-edge-opc-client) is an OPC UA client, which can be used to work with this OPC UA server implementation.

Please check out the github repository https://github.com/Azure-Samples/iot-edge-industrial-configs for sample configurations showing usage of this OPC UA server implementation.


## Resources

- [The OPC Foundation OPC UA .NET reference stack](https://github.com/OPCFoundation/UA-.NETStandard)
