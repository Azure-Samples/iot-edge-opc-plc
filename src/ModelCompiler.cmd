@echo off
setlocal
set modelName1=%1
set namespace1=%2
set prefix1=%3
set modelName2=%4
set namespace2=%5
set prefix2=%6

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
IF "%2" == "" (
    echo Building %modelName1%.xml ...
    %MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/%modelName1%.xml" -cg "%MODELROOT%/%modelName1%.csv" -o2 "%MODELROOT%/"
) ELSE IF "%4" == "" (
    echo Building %modelName1%.xml,%namespace1%,%prefix1% ...
    %MODELCOMPILER% compile -version v104 -id 1000 -d2 "%MODELROOT%/%modelName1%.xml,%namespace1%,%prefix1%" -o2 "%MODELROOT%/"
    IF %ERRORLEVEL% EQU 0 echo Success!
) ELSE (
    echo Building %modelName1%.xml,%namespace1%,%prefix1% %modelName2%.xml,%namespace2%,%prefix2%...
    %MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/%modelName1%.xml,%namespace1%,%prefix1%" -d2 "%MODELROOT%/%modelName2%.xml,%namespace2%,%prefix2%" -cg "%MODELROOT%/%modelName1%.csv" -o2 "%MODELROOT%/"
    IF %ERRORLEVEL% EQU 0 echo Success!
)

echo:
IF ERRORLEVEL 1 (
    echo ModelCompiler failed!
) ELSE (
    echo ModelCompiler succeeded!
)
