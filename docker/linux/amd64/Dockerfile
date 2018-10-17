ARG runtime_base_tag=2.1-runtime-alpine
ARG build_base_tag=2.1-sdk-alpine

FROM microsoft/dotnet:${build_base_tag} AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY opcplc/*.csproj ./opcplc/
WORKDIR /app/opcplc
RUN dotnet restore

# copy and publish app
WORKDIR /app
COPY opcplc/. ./opcplc/
WORKDIR /app/opcplc
RUN dotnet publish -c Release -o out

# start it up
FROM microsoft/dotnet:${runtime_base_tag} AS runtime
WORKDIR /app
COPY --from=build /app/opcplc/out ./
WORKDIR /appdata
ENTRYPOINT ["dotnet", "/app/opcplc.dll"]