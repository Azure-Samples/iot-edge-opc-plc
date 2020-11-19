@echo off

echo Building ModelDesign

REM Issue> The ModelCompiler from docker doesn't create the DataTypeDefinition'
REM docker run --mount type=bind,source=%CD%,target=/model sailavid/ua-modelcompiler -- -console -version v104 -d2 /model/ModelDesign.xml -cg /model/ModelDesign.csv -o2 /model

REM Build master branch in https://github.com/OPCFoundation/UA-ModelCompiler
REM add ModelCompiler build output folder to PATH
Opc.Ua.ModelCompiler.exe -version v104 -d2 ".\ModelDesign.xml" -c ".\ModelDesign.csv" -o2 ".\"

echo Success!
