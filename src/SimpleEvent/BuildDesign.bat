@echo off
setlocal

REM if docker is not available, ensure the Opc.Ua.ModelCompiler.exe is in the PATH
set MODELCOMPILER=Opc.Ua.ModelCompiler.exe
REM 
set MODELCOMPILERIMAGE=ghcr.io/opcfoundation/ua-modelcompiler:latest
set MODELROOT=.

echo pull latest modelcompiler from github container registry
docker pull %MODELCOMPILERIMAGE%
IF ERRORLEVEL 1 (
:nodocker
    Echo The docker command to download ModelCompiler failed. Using local PATH instead to execute ModelCompiler.
) ELSE (
    Echo Successfully pulled the latest docker container for ModelCompiler.
    set MODELROOT=/model
    set MODELCOMPILER=docker run -v "%CD%:/model" -it --rm --name ua-modelcompiler %MODELCOMPILERIMAGE% 
)

echo Building ModelDesign
%MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/ModelDesign.xml" -cg "%MODELROOT%/ModelDesign.csv" -o2 "%MODELROOT%/"
IF ERRORLEVEL 1 (
echo Modelcompiler failed!
) ELSE (
echo Success!
)