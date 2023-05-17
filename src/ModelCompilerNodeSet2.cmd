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
REM private build until the official image is updated
set MODELCOMPILERIMAGE=ghcr.io/mregen/ua-modelcompiler:latest-docker-nodeset2
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

rem filename1
for %%A in ("%modelName1%") do (
    set "filename1=%%~nxA"
)
rem filename2
for %%A in ("%modelName2%") do (
    set "filename2=%%~nxA"
)

rem Display the results
echo Model1 Filename : %filename1%
echo Model2 Filename : %filename2%
mkdir temp

IF "%4" == "" (
    echo Building Nodeset2 %modelName1%.xml,%namespace1%,%prefix1% ...
    COPY %modelName1%.xml temp
    %MODELCOMPILER% compile -version v104 -id 1000 -d2 "%MODELROOT%/temp/%filename1%.xml,%namespace1%,%prefix1%" -o2 "%MODELROOT%/"
) ELSE (
    COPY %modelName1%.xml temp
    COPY %modelName2%.xml temp
    echo Building Nodeset2 %modelName1%.xml,%namespace1%,%prefix1% %modelName2%.xml,%namespace2%,%prefix2%...
    %MODELCOMPILER% compile -version v104 -d2 "%MODELROOT%/temp/%filename1%.xml,%namespace1%,%prefix1%" -d2 "%MODELROOT%/temp/%filename2%.xml,%namespace2%,%prefix2%" -cg "%MODELROOT%/%modelName1%.csv" -o2 "%MODELROOT%/"
)

rmdir /s/q temp

echo:
IF ERRORLEVEL 1 (
    echo ModelCompiler failed!
) ELSE (
    echo ModelCompiler succeeded!
)