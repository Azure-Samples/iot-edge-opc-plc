@echo off
setlocal

REM Regenerates the WoT-Con NodeSet2 + .cs artefacts from WotConnection.xml/.csv.
REM WotConnection.xml and WotConnection.csv are kept in sync with
REM UA-.NETStandard / Libraries / Opc.Ua.WotCon / Design and rely on
REM OPC UA v1.05 types (e.g. ua:SemanticVersionString), so this script
REM targets -version v105 instead of the v104 default used by the shared
REM ../../ModelCompiler.cmd.

set MODELCOMPILER=Opc.Ua.ModelCompiler.exe
set MODELCOMPILERIMAGE=ghcr.io/opcfoundation/ua-modelcompiler:latest
set MODELROOT=.

echo Pulling latest ModelCompiler from the GitHub container registry ...
docker pull %MODELCOMPILERIMAGE%
IF ERRORLEVEL 1 (
    echo The docker command to download ModelCompiler failed, using local PATH instead
) ELSE (
    echo Successfully pulled the latest docker container for ModelCompiler
    set MODELROOT=/model
    set MODELCOMPILER=docker run -v "%CD%:/model" -i --rm --name ua-modelcompiler-wotcon %MODELCOMPILERIMAGE%
)

echo:
echo Building WotConnection.xml (v105) ...
%MODELCOMPILER% compile -version v105 -d2 "%MODELROOT%/WotConnection.xml" -cg "%MODELROOT%/WotConnection.csv" -o2 "%MODELROOT%/"

echo:
IF ERRORLEVEL 1 (
    echo ModelCompiler failed!
    exit /b 1
) ELSE (
    echo ModelCompiler succeeded!
)

REM Drop the per-language Constants/ folder the v105 compiler emits (CSharp/
REM JavaScript/Python/TypeScript). The C# variants live under a separate
REM Opc.Ua.WotCon.WebApi namespace and are not consumed by OPC PLC.
IF EXIST Constants (
    rmdir /S /Q Constants
)
