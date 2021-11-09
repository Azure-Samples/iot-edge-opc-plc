@echo off

echo Building ModelDesign

REM Build master branch in https://github.com/OPCFoundation/UA-ModelCompiler
REM add ModelCompiler build output folder to PATH
Opc.Ua.ModelCompiler.exe -version v104 -d2 ".\ModelDesign.xml" -c ".\ModelDesign.csv" -o2 ".\"

echo Success!
