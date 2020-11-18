@echo off

echo Building ModelDesign

docker run --mount type=bind,source=%CD%,target=/model sailavid/ua-modelcompiler -- -console -d2 /model/ModelDesign.xml -cg /model/ModelDesign.csv -o2 /model -version v104

echo Success!
