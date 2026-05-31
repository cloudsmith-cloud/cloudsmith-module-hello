// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CloudSmith.Module.Hello;

/// <summary>
/// Health check for the Hello World module.
///
/// The MLM registers this check with the platform health aggregator via
/// ICloudSmithModuleBuilder.AddHealthCheck in Program.cs.
/// It surfaces on the platform GET /health/ready endpoint and on the
/// Platform Health portal page under the name "hello-module".
///
/// Health check tags control how platform monitoring treats a failure:
///   "informational" — failure is reported but does not degrade overall health status
///   "degradable"    — failure sets overall health to Degraded (HTTP 200)
///   "critical"      — failure sets overall health to Unhealthy (HTTP 503)
///
/// The Hello module is tagged "informational" — it is a reference module and
/// its health does not affect platform availability.
///
/// A real module should return Unhealthy when its required backing services
/// (database, external APIs) are unavailable, and use "critical" or "degradable"
/// tags appropriate to its impact on the platform.
/// </summary>
public sealed class HelloHealthCheck : IHealthCheck
{
    /// <summary>
    /// The platform-level health check name used to identify this check in the
    /// /health/ready response and in the Platform Health portal page.
    /// Must match the name passed to AddHealthCheck in Program.cs.
    /// </summary>
    public const string Name = "hello-module";

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // The Hello module is always healthy — it has no external dependencies.
        // A real module would check database connectivity, external API reachability,
        // or internal queue depth here.
        //
        // Examples of real checks:
        //   var reachable = await _dbContext.Database.CanConnectAsync(cancellationToken);
        //   return reachable
        //       ? HealthCheckResult.Healthy("Database connection OK")
        //       : HealthCheckResult.Unhealthy("Cannot reach database");
        return Task.FromResult(HealthCheckResult.Healthy("Hello module is running."));
    }
}
