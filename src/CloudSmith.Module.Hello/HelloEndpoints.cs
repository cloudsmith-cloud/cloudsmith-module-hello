// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CloudSmith.Module.Hello;

/// <summary>
/// Minimal API endpoint registration for the Hello World module.
///
/// The platform's module hosting infrastructure scans the module assembly for classes
/// that extend IEndpointRouteBuilder at load time and calls MapHelloEndpoints
/// automatically. Endpoints are mounted under the module's API base path (/api/hello
/// by default, configured via module.json spec.apiBasePath).
///
/// Endpoint route groups:
///   GET  /api/hello         — greeting endpoint (demonstrates module API surface)
///   GET  /modules/hello     — module page HTML (demonstrates nav slot target route)
///   GET  /health            — module-local health probe (used by container orchestrator)
///
/// All API endpoints in a real module should:
///   - Require authentication via .RequireAuthorization() or a policy
///   - Use typed request/response records (not anonymous objects)
///   - Return ProblemDetails on error (not raw strings)
///   - Be documented with Swagger/OpenAPI attributes
///
/// The Hello module intentionally omits auth on all endpoints to simplify
/// end-to-end install testing without requiring a valid session token.
/// Production modules MUST protect their endpoints.
/// </summary>
public static class HelloEndpoints
{
    /// <summary>
    /// Register all Hello module routes.
    /// Called automatically by the SDK hosting layer via assembly scanning.
    /// </summary>
    public static void MapHelloEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/hello")
            .WithTags("Hello Module");

        // --- GET /api/hello ---
        // Returns a JSON greeting. This is the primary module API endpoint.
        // After the module is installed, the platform proxies requests under
        // /api/hello to this module container.
        // After uninstall, the platform removes the proxy route and all
        // requests to /api/hello return HTTP 404.
        group.MapGet("/", () => Results.Ok(new HelloResponse(
            Message: "Hello from CloudSmith Modules!",
            ModuleId: HelloModule.ModuleInfoStatic.Id,
            Version: HelloModule.ModuleInfoStatic.Version,
            Timestamp: DateTimeOffset.UtcNow)))
            .WithName("HelloGet")
            .WithSummary("Returns a greeting from the Hello World module.");

        // --- GET /modules/hello ---
        // The nav slot registered in module.json routes to this path.
        // The portal renders this endpoint inside its iframe/micro-frontend slot.
        app.MapGet("/modules/hello", () => Results.Content(
            content: HelloPageHtml(),
            contentType: "text/html"))
            .WithName("HelloPage")
            .WithSummary("Hello World module page — rendered in the portal nav slot.");

        // --- GET /health ---
        // Module-local liveness probe used by Docker / Kubernetes readiness checks.
        // This is separate from the platform health check (HelloHealthCheck) which
        // reports to the CloudSmith Platform Health page.
        // The Dockerfile HEALTHCHECK instruction calls this endpoint.
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
            .WithName("HelloHealth")
            .WithSummary("Module-local liveness probe.");
    }

    private static string HelloPageHtml() => """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Hello World — CloudSmith Module</title>
          <style>
            body { font-family: system-ui, sans-serif; background: #0f172a; color: #f1f5f9;
                   display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }
            .card { background: #1e293b; border-radius: 12px; padding: 2rem 3rem; text-align: center;
                    box-shadow: 0 4px 24px rgba(0,0,0,0.4); }
            h1 { color: #38bdf8; margin-bottom: 0.5rem; }
            p  { color: #94a3b8; margin: 0.25rem 0; }
            code { background: #0f172a; padding: 0.1rem 0.4rem; border-radius: 4px; font-size: 0.85rem; }
          </style>
        </head>
        <body>
          <div class="card">
            <h1>Hello from CloudSmith Modules!</h1>
            <p>This page is served by the <code>cloudsmith-module-hello</code> reference module.</p>
            <p>Module version: <code>0.1.0</code></p>
            <p>If you can see this page, the nav slot and module proxy are working correctly.</p>
          </div>
        </body>
        </html>
        """;
}

/// <summary>
/// Response payload for GET /api/hello.
/// </summary>
/// <param name="Message">Human-readable greeting.</param>
/// <param name="ModuleId">The module identifier from ModuleInfo.</param>
/// <param name="Version">The module version from ModuleInfo.</param>
/// <param name="Timestamp">UTC timestamp of when the response was generated.</param>
public sealed record HelloResponse(
    string Message,
    string ModuleId,
    string Version,
    DateTimeOffset Timestamp);
