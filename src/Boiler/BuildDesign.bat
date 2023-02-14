@echo off
setlocal

REM If docker is not available, ensure that Opc.Ua.ModelCompiler.exe is in the PATH environment variable
set MODELCOMPILER=Opc.Ua.ModelCompiler.exe
set MODELCOMPILERIMAGE=ghcr.io/opcfoundation/ua-modelcompiler:latest
set MODELROOT=.

echo Pulling latest ModelCompiler from the GitHub container registry ...
docker pull %MODELCOMPILERIMAGE%
IF ERRORLEVEL 1 (
:nodocker
    Echo The docker command to download ModelCompiler failed. Using local PATH instead.
) ELSE (
    Echo Successfully pulled the latest docker container for ModelCompiler
    set MODELROOT=/model
    set MODELCOMPILER=docker run -v "%CD%:/model" -it --rm --name ua-modelcompiler %MODELCOMPILERIMAGE%
)

echo Building ModelDesign ...
%MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/ModelDesign.xml" -cg "%MODELROOT%/ModelDesign.csv" -o2 "%MODELROOT%/"
IF ERRORLEVEL 1 (
    echo Modelcompiler failed!
) ELSE (
    echo Success!
)
