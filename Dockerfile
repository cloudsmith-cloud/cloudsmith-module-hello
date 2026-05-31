# Copyright 2026 CloudSmith Contributors
# SPDX-License-Identifier: Apache-2.0
#
# Multi-stage Dockerfile for the CloudSmith Hello World reference module.
#
# Stage 1 (build):  Restore NuGet packages and publish the ASP.NET Core app.
# Stage 2 (runtime): Copy published output into a minimal ASP.NET runtime image.
#
# The GitHub Packages NuGet feed (for CloudSmith.Sdk) is injected via build ARG
# so that the NUGET_SOURCE_TOKEN secret is never baked into the image layer cache.
#
# Build with:
#   docker build \
#     --build-arg NUGET_SOURCE_TOKEN=<ghp_token> \
#     -t ghcr.io/cloudsmith-cloud/cloudsmith-module-hello:dev .
#
# Run locally:
#   docker run --rm -p 8080:8080 ghcr.io/cloudsmith-cloud/cloudsmith-module-hello:dev

ARG DOTNET_SDK_VERSION=9.0
ARG DOTNET_RUNTIME_VERSION=9.0

# ── Stage 1: build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build

ARG NUGET_SOURCE_TOKEN
WORKDIR /src

# Copy project file first to benefit from layer caching on restore.
COPY src/CloudSmith.Module.Hello/CloudSmith.Module.Hello.csproj \
     src/CloudSmith.Module.Hello/

# Add the GitHub Packages NuGet source for CloudSmith.Sdk.
# The token is injected at build time and never written to the image.
RUN dotnet nuget add source \
      "https://nuget.pkg.github.com/cloudsmith-cloud/index.json" \
      --name "github-cloudsmith" \
      --username "x-access-token" \
      --password "${NUGET_SOURCE_TOKEN}" \
      --store-password-in-clear-text

# Restore dependencies.
RUN dotnet restore src/CloudSmith.Module.Hello/CloudSmith.Module.Hello.csproj

# Copy remaining source files.
COPY src/CloudSmith.Module.Hello/ src/CloudSmith.Module.Hello/

# Publish a self-contained, trimmed release build to /app.
RUN dotnet publish src/CloudSmith.Module.Hello/CloudSmith.Module.Hello.csproj \
      --configuration Release \
      --output /app \
      --no-restore

# ── Stage 2: runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION} AS runtime

# Create a non-root user to run the module.
RUN addgroup --system --gid 1001 cloudsmith \
 && adduser  --system --uid 1001 --ingroup cloudsmith --no-create-home cloudsmith

WORKDIR /app

# Copy published output from the build stage.
COPY --from=build /app .

# Copy the module manifest so the MLM can read it from the running container.
COPY module.json /app/module.json

# Set the runtime user.
USER cloudsmith

# CloudSmith module containers listen on port 8080 by default.
EXPOSE 8080

# Module-local liveness probe — called by Docker / Kubernetes.
# The platform MLM also uses this to confirm the module is ready.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CloudSmith.Module.Hello.dll"]
