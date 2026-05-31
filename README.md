# cloudsmith-module-hello

Reference module for [CloudSmith](https://cloudsmith.cloud) — the open-source cloud platform management solution for Hyper-V and Azure Local environments.

This module is intentionally minimal. It exists to:

1. Prove the module install, uninstall, and nav slot registration lifecycle end-to-end.
2. Serve as a copy-paste starting point for new CloudSmith modules.
3. Exercise the CloudSmith SDK contracts so the platform team can catch regressions.

---

## What this module does

| Feature | Detail |
|---|---|
| Nav slot | Registers "Hello World" in the primary nav sidebar (`nav.primary`) |
| API endpoint | `GET /api/hello` — returns a JSON greeting |
| Module page | `GET /modules/hello` — HTML page rendered in the portal nav slot |
| Health check | `hello-module` — always Healthy; surfaced on the Platform Health page |
| Background job | Heartbeat timer — logs a message every 60 seconds |

---

## Using this as a module template

### Step 1 — Fork this repository

```
gh repo fork cloudsmith-cloud/cloudsmith-module-hello --clone --org <your-github-org>
```

Rename the fork to `<your-org>-module-<name>`.

### Step 2 — Rename the module

Replace every occurrence of `hello` / `Hello` with your module name:

- `src/CloudSmith.Module.Hello/` directory → rename to `src/CloudSmith.Module.<YourName>/`
- `CloudSmith.Module.Hello.csproj` → `CloudSmith.Module.<YourName>.csproj`
- `HelloModule.cs`, `HelloHealthCheck.cs`, `HelloEndpoints.cs` → rename files and class names
- `Program.cs` — update `MapCloudSmithModule<HelloModule>()` and `MapHelloEndpoints()`

### Step 3 — Update module.json

Edit `module.json` and set:

```json
{
  "metadata": {
    "id": "<your-org>-module-<name>",
    "name": "<Your Module Display Name>",
    "version": "0.1.0",
    "description": "<One sentence description>",
    "author": "<Your Name or Org>",
    "publisher": "<your-github-org>"
  },
  "spec": {
    "image": "ghcr.io/<your-github-org>/<your-org>-module-<name>:latest",
    "apiBasePath": "/api/<name>",
    "navSlots": [
      {
        "slotId": "nav.primary",
        "id": "<name>",
        "label": "<Your Label>",
        "icon": "<LucideIconName>",
        "route": "/modules/<name>",
        "order": 200
      }
    ]
  }
}
```

Available `icon` values are Lucide icon names — see [lucide.dev/icons](https://lucide.dev/icons/).

### Step 4 — Add your module logic

| File | Purpose |
|---|---|
| `HelloModule.cs` | Lifecycle: ConfigureServices → ConfigureAsync → StartAsync → StopAsync → DisposeAsync |
| `HelloEndpoints.cs` | Minimal API routes — all module API and page endpoints |
| `HelloHealthCheck.cs` | Health check reported to the Platform Health page |

The inline comments in each file explain the contract touchpoints.

### Step 5 — Push and install

The CI workflow (`ci.yml`) builds and pushes to `ghcr.io/<your-org>/<repo>:latest` on every push to main.

To install in a running CloudSmith instance:

```
POST /api/modules/install
{
  "imageRef": "ghcr.io/<your-org>/<repo>:latest",
  "manifestUrl": "https://raw.githubusercontent.com/<your-org>/<repo>/main/module.json"
}
```

After install, the Hello World nav item appears in the portal sidebar within 5 seconds.

To uninstall:

```
DELETE /api/modules/<module-id>
```

---

## ICloudSmithModule lifecycle

Every module implements five lifecycle methods called sequentially by the Module Lifecycle Manager (MLM):

```
ConfigureServices(IServiceCollection)
    → ConfigureAsync(IModuleContext, CancellationToken)
        → StartAsync(CancellationToken)
            → [module serves traffic]
        → StopAsync(CancellationToken)
    → DisposeAsync()
```

| Method | When called | What to do |
|---|---|---|
| `ConfigureServices` | Host builder construction — DI container not yet built | Register DI services (repositories, HTTP clients, etc.) |
| `ConfigureAsync` | After DI container is built, before any StartAsync | Obtain logger, subscribe to event bus, cache warm-up |
| `StartAsync` | After host is fully started and ready for traffic | Start background timers and workers |
| `StopAsync` | On SIGTERM or host stop | Signal workers to stop, flush buffers (30s grace period) |
| `DisposeAsync` | After StopAsync completes (or times out) | Dispose IDisposable fields and event bus subscriptions |

The MLM guarantees all `ConfigureAsync` calls complete before any `StartAsync` fires.

---

## Module manifest schema

`module.json` is validated against `https://cloudsmith.cloud/schemas/module-manifest/v1.json` at install time. Required fields:

| Field | Type | Description |
|---|---|---|
| `metadata.id` | `string` | Stable kebab-case identifier — must match `ModuleInfo.Id` in code |
| `metadata.name` | `string` | Human-readable display name |
| `metadata.version` | `string` | SemVer 2.0 version |
| `metadata.description` | `string` | One-sentence description |
| `metadata.author` | `string` | Module author name or org |
| `spec.sdkVersion` | `string` | SemVer range for required CloudSmith.Sdk version |
| `spec.image` | `string` | Fully-qualified OCI image reference |
| `spec.apiBasePath` | `string` | URL prefix for this module's API routes |
| `spec.healthEndpoint` | `string` | Path the MLM calls to check module health |

Optional fields: `spec.navSlots`, `spec.permissionsRequired`, `spec.healthChecks`, `spec.backgroundJobs`.

---

## License

Apache-2.0 — see [LICENSE](LICENSE).
