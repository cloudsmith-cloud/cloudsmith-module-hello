// Copyright 2026 CloudSmith Contributors
// SPDX-License-Identifier: Apache-2.0

using CloudSmith.Sdk.Modules;
using CloudSmith.Sdk.Slots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloudSmith.Module.Hello;

/// <summary>
/// CloudSmith Hello World reference module.
///
/// This module is the canonical template for building CloudSmith modules.
/// It exercises every lifecycle hook in the ICloudSmithModule contract:
///
///   1. ConfigureServices — register module-owned DI services
///   2. ConfigureAsync   — subscribe to events, cache warm-up, background setup
///   3. StartAsync       — start background workers / timers
///   4. StopAsync        — graceful shutdown of background workers
///   5. DisposeAsync     — release unmanaged resources and IDisposable subscriptions
///
/// Nav slot registration is declared via the <see cref="CloudSmithSlotContributionAttribute"/>
/// on this class. The platform's Module Lifecycle Manager (MLM) reads the attribute at load
/// time and registers the nav entry into the portal before StartAsync fires.
///
/// To use this as a template:
///   1. Fork or copy this repository.
///   2. Replace every occurrence of "Hello" / "hello" with your module name.
///   3. Update module.json with your module id, version, and nav slot details.
///   4. Add your API endpoints in HelloEndpoints.cs (see inline comments there).
///   5. Add your health check logic in HelloHealthCheck.cs.
///   6. Push to a public GitHub repo — the CI workflow handles ghcr.io publishing.
/// </summary>
[CloudSmithSlotContribution(CloudSmithSlots.NavPrimary, Order = 100)]
[CloudSmithSlotContribution(CloudSmithSlots.ModulePage)]
public sealed class HelloModule : ICloudSmithModule
{
    // ModuleInfo is the compile-time identity of the module.
    // The MLM validates Info.Id against "metadata.id" in module.json at load time.
    // A mismatch causes CS-MOD-5001 and aborts module loading.
    public static readonly ModuleInfo ModuleInfoStatic = new(
        Id: "cloudsmith-module-hello",
        Version: "0.1.0",
        SdkMinVersion: ">=0.1.0 <1.0.0");

    // ICloudSmithModule.Info must return the same value on every call — use the static field.
    public ModuleInfo Info => ModuleInfoStatic;

    // Logger is populated in ConfigureAsync once IModuleContext is available.
    private ILogger<HelloModule>? _logger;

    // Background heartbeat timer — started in StartAsync, stopped in StopAsync.
    private Timer? _heartbeatTimer;

    // Cancellation source for the heartbeat background worker.
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Step 1 of 5 — register module-owned services into the platform DI container.
    ///
    /// The DI container is NOT yet built at this point — do not call sp.GetRequiredService here.
    /// Register services that your API endpoints and background workers will consume.
    /// Only register wrappers around platform API clients if the corresponding permission
    /// is declared in module.json under spec.permissionsRequired.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // The Hello module has no module-specific services beyond the module itself.
        // A real module would register repositories, domain services, and HTTP clients here.
        // Example:
        //   services.AddScoped<IMyRepository, MyRepository>();
        //   services.AddHttpClient<IExternalServiceClient, ExternalServiceClient>();
    }

    /// <summary>
    /// Step 2 of 5 — one-time initialization after the DI container is built.
    ///
    /// The MLM guarantees this is called for ALL modules before StartAsync fires for ANY module.
    /// Use this for:
    ///   - Obtaining a logger from the context (required for structured logging)
    ///   - Subscribing to IPlatformEventBus events
    ///   - Schema verification or cache warm-up
    ///   - Any async setup that must complete before the module starts serving traffic
    ///
    /// Throwing from this method transitions the module to Failed state (CS-MOD-5004).
    /// The host continues loading other modules regardless.
    /// </summary>
    public Task ConfigureAsync(IModuleContext context, CancellationToken ct)
    {
        // Obtain a Serilog-backed logger pre-enriched with module_id and module_version.
        // Always obtain the logger from the context — never new up ILogger directly.
        _logger = context.GetLogger<HelloModule>();

        _logger.LogInformation(
            "Hello module configured. Module={ModuleId} Version={Version} OrgId={OrgId}",
            Info.Id,
            Info.Version,
            context.OrgId ?? "(platform-wide)");

        // Subscribe to platform events here using context.EventBus.Subscribe<TEvent>(...).
        // Save the returned IDisposable to a field and dispose it in DisposeAsync.
        // Example:
        //   _clusterCreatedSub = context.EventBus.Subscribe<ClusterCreatedEvent>(OnClusterCreated);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 3 of 5 — start background workers after the host is fully ready for traffic.
    ///
    /// The Hello module starts a heartbeat timer that logs a message every 60 seconds.
    /// This demonstrates how to implement a periodic background job without taking a
    /// dependency on a full hosted service framework.
    ///
    /// Throwing from this method logs CS-MOD-5002 but does NOT stop the host.
    /// </summary>
    public Task StartAsync(CancellationToken ct)
    {
        _logger?.LogInformation("Hello module starting. Nav slot '{NavSlot}' registered.", "Hello World");

        // Start the heartbeat timer. The timer fires every 60 seconds.
        // Use _cts.Token so the worker stops cleanly in StopAsync.
        _heartbeatTimer = new Timer(
            callback: _ => Heartbeat(),
            state: null,
            dueTime: TimeSpan.FromSeconds(60),
            period: TimeSpan.FromSeconds(60));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 4 of 5 — graceful shutdown.
    ///
    /// The platform waits up to 30 seconds for this method to return.
    /// Signal background workers to stop, flush write buffers, and complete in-flight requests.
    /// If the cancellation token fires before you return, CS-MOD-5003 is logged and teardown
    /// proceeds without waiting.
    /// </summary>
    public async Task StopAsync(CancellationToken ct)
    {
        _logger?.LogInformation("Hello module stopping.");

        // Signal the heartbeat worker to stop.
        await _cts.CancelAsync();

        // Dispose the timer — no more callbacks will fire after this returns.
        if (_heartbeatTimer is not null)
        {
            await _heartbeatTimer.DisposeAsync();
            _heartbeatTimer = null;
        }
    }

    /// <summary>
    /// Step 5 of 5 — async cleanup called after StopAsync completes (or times out).
    ///
    /// Release unmanaged resources and dispose all IDisposable subscriptions obtained
    /// from IPlatformEventBus.Subscribe. The MLM calls this regardless of whether
    /// StopAsync succeeded.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _cts.Dispose();
        _heartbeatTimer?.Dispose();

        // Dispose event bus subscriptions here.
        // Example:
        //   _clusterCreatedSub?.Dispose();

        return ValueTask.CompletedTask;
    }

    // --- Private helpers ---

    private void Heartbeat()
    {
        if (_cts.IsCancellationRequested)
            return;

        _logger?.LogInformation(
            "Hello module heartbeat. Module={ModuleId} Version={Version}",
            Info.Id,
            Info.Version);
    }
}
