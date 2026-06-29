@echo off
setlocal

REM Regenerates the WoT-Con NodeSet2 + .cs artefacts from WotConnection.xml/.csv.
REM WotConnection.xml and WotConnection.csv are kept in sync with
REM UA-.NETStandard / Libraries / Opc.Ua.WotCon / Design and rely on
REM OPC UA v1.05 types (e.g. ua:SemanticVersionString), so we invoke the
REM shared ModelCompiler.cmd with version v105 instead of its v104 default.

call "%~dp0..\..\ModelCompiler.cmd" WotConnection v105
IF ERRORLEVEL 1 exit /b 1

pushd "%~dp0"

REM Drop the per-language Constants/ folder the v105 compiler emits (CSharp/
REM JavaScript/Python/TypeScript). The C# variants live under a separate
REM Opc.Ua.WotCon.WebApi namespace and are not consumed by OPC PLC.
IF EXIST Constants (
    rmdir /S /Q Constants
)

REM Drop generator outputs we don't consume:
REM   - PredefinedNodes.{cs,xml,uanodes}: WotConNodeManager loads NodeSet2.xml
REM     directly, so the stack-internal NodeStateCollection serializations are
REM     dead weight.
REM   - NodeIds.permissions.csv: empty stub (no role-permission rules defined).
REM   - Types.bsd / Types.xsd: empty type-dictionary/schema stubs (no
REM     structured DataTypes in the model).
IF EXIST Opc.Ua.WotCon.PredefinedNodes.cs       del /Q Opc.Ua.WotCon.PredefinedNodes.cs
IF EXIST Opc.Ua.WotCon.PredefinedNodes.xml      del /Q Opc.Ua.WotCon.PredefinedNodes.xml
IF EXIST Opc.Ua.WotCon.PredefinedNodes.uanodes  del /Q Opc.Ua.WotCon.PredefinedNodes.uanodes
IF EXIST Opc.Ua.WotCon.NodeIds.permissions.csv  del /Q Opc.Ua.WotCon.NodeIds.permissions.csv
IF EXIST Opc.Ua.WotCon.Types.bsd                del /Q Opc.Ua.WotCon.Types.bsd
IF EXIST Opc.Ua.WotCon.Types.xsd                del /Q Opc.Ua.WotCon.Types.xsd

popd
