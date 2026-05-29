# Monkey App Icon — Design Spec
**Date:** 2026-05-29  
**Status:** Approved

---

## Overview

Replace the default WPF app icon with a simple flat cartoon monkey face. The icon appears on the `.exe` file in Windows Explorer, the pinned taskbar entry, and the runtime window taskbar button.

---

## Asset Creation

### Image generation

Use an AI image generator (e.g. ChatGPT image gen, DALL-E, Midjourney) with this prompt:

> A simple flat cartoon monkey face icon. Round head, big friendly eyes, tan muzzle, brown fur. Solid transparent background. Minimal detail, clean shapes, suitable for a small app icon. No text. Square composition.

Target output: **1024×1024 PNG with transparent background**.

### ICO conversion

Convert the PNG to a multi-size `.ico` using ImageMagick:

```
magick monkey.png -define icon:auto-resize=256,48,32,16 monkey.ico
```

This embeds four sizes (256, 48, 32, 16) into a single `.ico` file, which Windows uses to pick the best resolution per context.

---

## Project Integration

### File location

```
ADTool/
  Assets/
    monkey.ico    ← new
```

### `ADTool.csproj` change

Add `<ApplicationIcon>` to the existing `PropertyGroup`:

```xml
<PropertyGroup>
  ...
  <ApplicationIcon>Assets\monkey.ico</ApplicationIcon>
</PropertyGroup>
```

WPF reads `ApplicationIcon` at both build time (embeds in the `.exe`) and runtime (sets the window/taskbar icon automatically). No changes to `MainWindow.xaml` or any ViewModel are required.

---

## Out of Scope

- In-app usage (home screen card, about dialog)
- Light/dark variants
- Animated or high-DPI specific assets beyond the four standard ICO sizes
