﻿# Application Launch Monitoring

## What it is
- Small in-memory helper used by the Uno Remote Control Dev Server to correlate “I launched an app” with “that app connected back.”
- You tell it that an app was launched, then you report when a matching app connects. It matches them 1:1 in launch order and handles timeouts.
- When a launched application fails to connect, it is reported as a timeout thru the OnTimeout callback.
- It is thread-safe / Disposable.
- It used the MVID as the key. This is the _Module Version ID_ of the app (head) assembly, which is unique per build. More info: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.module.moduleversionid
- It uses the MVID as the key. This is the _Module Version ID_ of the app (head) assembly, which is unique per build. More info: https://learn.microsoft.com/en-us/dotnet/api/system.reflection.module.moduleversionid

## How to use
### 1) Create the monitor (optionally with callbacks and a custom timeout):

```csharp
using Uno.UI.RemoteControl.Server.AppLaunch;

var options = new ApplicationLaunchMonitor.Options
{
    // Default is 60s
    Timeout = TimeSpan.FromSeconds(30),
    OnRegistered = ev => logger.LogInformation($"Launch registered: {ev.Mvid} ({ev.Platform})"),
    OnConnected = ev => logger.LogInformation($"Connected: {ev.Mvid} ({ev.Platform})"),
    OnTimeout   = ev => logger.LogWarning($"Timed out: {ev.Mvid} ({ev.Platform})")
};

using var monitor = new ApplicationLaunchMonitor(options: options);
```

### 2) When you start a target app, register the launch:
```csharp
monitor.RegisterLaunch(mvid, "Wasm", isDebug: true);
```

### 3) When the app connects back to the dev server, report the connection:
```csharp
monitor.ReportConnection(mvid, "Wasm", isDebug: true);
```

That’s it. The monitor pairs the connection with the oldest pending launch for the same (mvid, platform, isDebug). If no connection arrives before the timeout, OnTimeout is invoked.

## Notes
- Platform matching is case-sensitive ("Wasm" != "wasm").
- Platform must not be null or empty (ArgumentException).
- Registrations are consumed in FIFO order per key.
- Always dispose the monitor (use "using" as shown).

### Testing / Time control

- The constructor accepts an optional `TimeProvider` which the monitor uses for timeout scheduling. Tests commonly inject `Microsoft.Extensions.Time.Testing.FakeTimeProvider` and advance time with `fake.Advance(...)` to trigger timeouts instantly.
- Example for tests:

```csharp
var fake = new FakeTimeProvider(DateTimeOffset.UtcNow);
using var monitor = new ApplicationLaunchMonitor(timeProvider: fake, options: options);
// register, then fake.Advance(TimeSpan.FromSeconds(11)); // triggers timeout callbacks
```

## Where it lives
- Project: Uno.UI.RemoteControl.Server
- Folder: AppLaunch
- File: `ApplicationLaunchMonitor.cs` (implementation) + this ApplicationLaunchMonitor.md (short guide)
