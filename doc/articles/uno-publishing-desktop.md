---
uid: uno.publishing.desktop
---

# Publishing Your App For Desktop

## Preparing For Publish

- [Profile your app with Visual Studio](https://learn.microsoft.com/en-us/visualstudio/profiling)
- [Profile using dotnet-trace and SpeedScope](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

## Publish Using Visual Studio 2022

- In the debugger toolbar drop-down, select the `net8.0-desktop` target framework
- Once the project has reloaded, right-click on the project and select **Publish**
- Select the **Folder** target for your publication then click **Next**
- Select the **Folder** target again then **Next**
- Choose an output folder then click **Finish**
- The profile is created, you can now **Close** the dialog
- In the opened editor, click `Show all settings`
- Set **Configuration** to `Release`
- Set **Target framework** to `net8.0-desktop`
- You can set **Deployment mode** to either `Framework-dependent` or `Self-contained`
  - If `Self-contained` is chosen and you're targeting Windows, **Target runtime** must match the installed .NET SDK runtime identifier
    as cross-publishing self-contained WPF apps (e.g. win-x64 to win-arm64) is not supported for now.
- You can set **Target runtime**, make sure it honors the above limitation, if it applies.
- Click **Save**
- Click **Publish**

## Publish Using The CLI

On Windows/macOS/Linux, open a terminal in your `csproj` folder and run:

```shell
dotnet publish -f net8.0-desktop
```

If you wish to do a self-contained publish, run the following instead:

```shell
dotnet publish -f net8.0-desktop -r {{RID}} -p:SelfContained=true
```

Where `{{RID}}` specifies the chosen OS and Architecture (e.g. win-x64). When targeting Windows, cross-publishing to architectures other than the currently running one is not supported.

### macOS App Bundles

We now support generating `.app` bundles on macOS machines. From the CLI run:

```shell
dotnet publish -f net8.0-desktop -p:PackageFormat=app
```

You can also do a self-contained publish with:

```shell
dotnet publish -f net8.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=app
```

Where `{{RID}}` is either `osx-x64` or `osx-arm64`.

> [!NOTE]
> Code signing is planned but not supported yet.

### Snap Packages

We support creating .snap packages on **Ubuntu 20.04** or later.

#### Requirements

The following must be installed and configured:

- snapd
- snaps (with `snap install`):
  - core20 on Ubuntu 20.04
  - core22 on Ubuntu 22.04
  - core24 on Ubuntu 24.04
  - multipass
  - lxd
    - current user must be part of the `lxd` group
    - `lxd init --minimal` or similar should be run
  - snapcraft

> [!NOTE]
> Docker may interfere with Lxd causing network connectivity issues, for solutions see: https://documentation.ubuntu.com/lxd/en/stable-5.0/howto/network_bridge_firewalld/#prevent-connectivity-issues-with-lxd-and-docker

#### Generate a Snap file

To generate a snap file, run the following:

```shell
dotnet publish -f net8.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=snap
```

Where `{{RID}}` is either `linux-x64` or `linux-arm64`. The generated snap file is located in the `publish` folder.

Uno Platform generates snap manifests in classic confinement mode and a `.desktop` file by default.

If you wish to customize your snap manifest, you will need to pass the following MSBuild properties:

- `SnapManifest`
- `DesktopFile`

The `.desktop` filename MUST conform to the Desktop File spec.

If you wish, you can generate a default snap manifest and desktop file by running the command above, then tweak them.

> [!NOTE]
> .NET 9 publishing and cross-publishing are not supported as of Uno 5.5, we will support .NET 9 publishing soon.

#### Publish your Snap Package

You can install your app on your machine using the following:

```bash
sudo snap install MyApp_1.0_amd64.snap --dangerous –classic
```

You can also publish your app to the [Snap store](https://snapcraft.io/store).

## Limitations

- NativeAOT is not yet supported
- R2R is not yet supported
- Single file publish is not yet supported

> [!NOTE]
> Publishing is a [work in progress](https://github.com/unoplatform/uno/issues/16440)

## Links

- [Snapcraft.yaml schema](https://snapcraft.io/docs/snapcraft-yaml-schema)
- [Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/latest)
