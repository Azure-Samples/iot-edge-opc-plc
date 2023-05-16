@echo off
setlocal
set modelName1=%1
set modelName2=%2

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
    set MODELCOMPILER=docker run -v "%CD%:/model" -it --rm --name ua-modelcompiler %MODELCOMPILERIMAGE%
)

echo:
echo Building %modelName1%.xml ...
IF "%2" == "" (
    %MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/%modelName1%.xml" -cg "%MODELROOT%/%modelName1%.csv" -o2 "%MODELROOT%/"
) ELSE (
    echo Building %modelName2%.xml ...

    echo Building DI from Nodeset2
    %MODELCOMPILER% compile -version v104 -id 1000 -d2 "%MODELROOT%/%modelName2%.xml,Opc.Ua.DI,OpcUaDI" -o2 "%MODELROOT%/DI"
    IF %ERRORLEVEL% EQU 0 echo Success!


    %MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/%modelName1%.xml,BoilerModel2,Boiler2" -d2 "%MODELROOT%/%modelName2%.xml,Opc.Ua.DI,OpcUaDI" -cg "%MODELROOT%/%modelName1%.csv" -o2 "%MODELROOT%/"
)

echo:
IF ERRORLEVEL 1 (
    echo ModelCompiler failed!
) ELSE (
    echo ModelCompiler succeeded!
)
