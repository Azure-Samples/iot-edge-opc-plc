# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG TARGETARCH
WORKDIR /app

# Copy configuration files
COPY common.props .
COPY Directory.Build.targets .
COPY src/Directory.Build.props src/
COPY src/opc-plc.csproj src/
COPY docs/media/icon.png docs/media/

# Restore dependencies
RUN if [ "$TARGETARCH" = "amd64" ]; then ARCH=x64; else ARCH="$TARGETARCH"; fi \
    && dotnet restore src/opc-plc.csproj -a "$ARCH"

# Copy source code
COPY src/ src/

# Publish
WORKDIR /app/src
RUN if [ "$TARGETARCH" = "amd64" ]; then ARCH=x64; else ARCH="$TARGETARCH"; fi \
    && dotnet publish opc-plc.csproj \
    -c Release \
    -o /app/publish \
    -a "$ARCH" \
    --self-contained true \
    /p:TargetLatestRuntimePatch=true

# Final stage
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS final
WORKDIR /app
COPY --from=build /app/publish .

# Setup non-root user
ARG APP_UID=1654
RUN mkdir -p /app && chown "$APP_UID" /app
USER $APP_UID

# Expose ports from container.json
EXPOSE 50000
EXPOSE 8080

ENTRYPOINT ["./opcplc"]
