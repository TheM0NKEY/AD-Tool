# Monkey App Icon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the default WPF app icon with a simple flat cartoon monkey face shown on the `.exe` and taskbar.

**Architecture:** Drop a multi-size `.ico` file into `ADTool/Assets/` and set `<ApplicationIcon>` in the `.csproj`. WPF automatically uses `ApplicationIcon` as both the embedded exe icon and the runtime window/taskbar icon — no XAML or ViewModel changes needed.

**Tech Stack:** ImageMagick CLI for PNG→ICO conversion, .NET 8 WPF / MSBuild `ApplicationIcon` property.

---

## Prerequisites (manual — do before Task 1)

Generate a **1024×1024 PNG with transparent background** using any AI image generator with this prompt:

> A simple flat cartoon monkey face icon. Round head, big friendly eyes, tan muzzle, brown fur. Solid transparent background. Minimal detail, clean shapes, suitable for a small app icon. No text. Square composition.

Save the output as `monkey.png` somewhere accessible (e.g. your Desktop).

---

## File Structure

| Action | Path | Purpose |
|---|---|---|
| Create | `ADTool/Assets/monkey.ico` | Multi-size ICO asset embedded in the exe |
| Modify | `ADTool/ADTool.csproj` | Add `<ApplicationIcon>` property |

---

### Task 1: Convert PNG to ICO

**Files:**
- Create: `ADTool/Assets/monkey.ico`

- [ ] **Step 1: Create the Assets folder**

```powershell
New-Item -ItemType Directory -Force "C:\Users\jackw\Documents\Programming\AD Tool\ADTool\Assets"
```

Expected: directory created (or already exists — no error either way).

- [ ] **Step 2: Convert PNG to multi-size ICO**

Replace `C:\path\to\monkey.png` with the actual path to your generated PNG.

```powershell
magick "C:\path\to\monkey.png" -define icon:auto-resize=256,48,32,16 "C:\Users\jackw\Documents\Programming\AD Tool\ADTool\Assets\monkey.ico"
```

Expected: `ADTool/Assets/monkey.ico` created, roughly 100–300 KB.

- [ ] **Step 3: Verify the ICO contains the expected sizes**

```powershell
magick identify "C:\Users\jackw\Documents\Programming\AD Tool\ADTool\Assets\monkey.ico"
```

Expected output (four lines, one per size):

```
...monkey.ico[0] ICO 256x256 ...
...monkey.ico[1] ICO 48x48 ...
...monkey.ico[2] ICO 32x32 ...
...monkey.ico[3] ICO 16x16 ...
```

- [ ] **Step 4: Commit the asset**

```powershell
cd "C:\Users\jackw\Documents\Programming\AD Tool"
git add ADTool/Assets/monkey.ico
git commit -m "assets: add cartoon monkey app icon"
```

---

### Task 2: Wire the icon into the project

**Files:**
- Modify: `ADTool/ADTool.csproj`

- [ ] **Step 1: Add `<ApplicationIcon>` to the csproj**

Open `ADTool/ADTool.csproj`. Inside the first `<PropertyGroup>` block, add one line after `<AssemblyName>ADTool</AssemblyName>`:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net8.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <UseWPF>true</UseWPF>
  <AssemblyName>ADTool</AssemblyName>
  <RootNamespace>ADTool</RootNamespace>
  <ApplicationIcon>Assets\monkey.ico</ApplicationIcon>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <PublishSingleFile>true</PublishSingleFile>
  <ApplicationManifest>app.manifest</ApplicationManifest>
</PropertyGroup>
```

- [ ] **Step 2: Build to confirm no errors**

```powershell
cd "C:\Users\jackw\Documents\Programming\AD Tool"
dotnet build ADTool/ADTool.csproj
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Run the app and verify the icon**

```powershell
dotnet run --project ADTool/ADTool.csproj
```

Check:
- The taskbar button shows the cartoon monkey while the app is running.
- In File Explorer, navigate to `ADTool/bin/Debug/net8.0-windows/win-x64/` and confirm `ADTool.exe` shows the monkey icon.

- [ ] **Step 4: Commit**

```powershell
git add ADTool/ADTool.csproj
git commit -m "feat: set cartoon monkey as app icon"
```
