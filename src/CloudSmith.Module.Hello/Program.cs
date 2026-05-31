// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

// ============================================================================
// CloudSmith Module Host — Hello World Reference Module
// ============================================================================
//
// This Program.cs is the entry point for the Hello module container.
// When CloudSmith installs this module, it starts this container and the
// platform proxies matching requests to it.
//
// For a real module, copy this file and:
//   1. Replace MapCloudSmithModule<HelloModule>() with your module type.
//   2. Replace MapHelloEndpoints() with your endpoint registration method.
//   3. Adjust AddHealthCheck to match your module's health check type and name.
//   4. Add any module-specific middleware (auth policies, CORS, rate limiting).
// ============================================================================

using CloudSmith.Module.Hello;
using CloudSmith.Sdk.Hosting;

var builder = WebApplication.CreateBuilder(args);

// --- Module registration ---
// MapCloudSmithModule wires the module into the platform DI container and:
//   - Registers HelloModule as singleton ICloudSmithModule
//   - Loads and validates the embedded module manifest
//   - Registers nav slot contributions from [CloudSmithSlotContribution] attributes
builder.Services
    .MapCloudSmithModule<HelloModule>()
    .AddHealthCheck<HelloHealthCheck>(
        name: HelloHealthCheck.Name,
        tags: "informational");   // Does not degrade overall platform health on failure

// --- Standard ASP.NET Core services ---
builder.Services.AddHealthChecks();

var app = builder.Build();

// --- Middleware pipeline ---
app.UseRouting();

// Register Hello module API endpoints and the module page route.
app.MapHelloEndpoints();

// Platform health aggregation endpoint — called by the MLM health poller.
app.MapHealthChecks("/health/ready");

app.Run();
