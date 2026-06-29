@echo off
setlocal
set modelName=%1
set modelVersion=%2
IF "%modelVersion%"=="" set modelVersion=v104

REM If docker is not available, ensure that Opc.Ua.ModelCompiler.exe is in the PATH environment variable
set MODELCOMPILER=Opc.Ua.ModelCompiler.exe
set MODELCOMPILERIMAGE=ghcr.io/opcfoundation/ua-modelcompiler:latest
set MODELROOT=.

echo Pulling latest ModelCompiler from the GitHub container registry ...
docker pull %MODELCOMPILERIMAGE%
IF ERRORLEVEL 1 (
:nodocker
    echo The docker command to download ModelCompiler failed, using local PATH instead
) ELSE (
    echo Successfully pulled the latest docker container for ModelCompiler
    set MODELROOT=/model
    set MODELCOMPILER=docker run -v "%CD%:/model" -i --rm --name ua-modelcompiler-%modelName% %MODELCOMPILERIMAGE%
)

echo:
echo Building %modelName%.xml (%modelVersion%) ...
%MODELCOMPILER% compile -version %modelVersion% -d2 "%MODELROOT%/%modelName%.xml" -cg "%MODELROOT%/%modelName%.csv" -o2 "%MODELROOT%/"

echo:
IF ERRORLEVEL 1 (
    echo ModelCompiler failed!
) ELSE (
    echo ModelCompiler succeeded!
)