# UI Redesign — Design Spec
**Date:** 2026-05-28  
**Status:** Approved

---

## Overview

Restyle the AD Tool WPF desktop app with a dark, professional look: Dark Slate theme, Electric Blue accent, progress-pill step indicator, top-bar layout. All MVVM code (ViewModels, Models, Services) is untouched. Only XAML and a new `Theme.xaml` resource dictionary change.

---

## Design System

### Colour Palette

| Token | Hex | Usage |
|---|---|---|
| `BackgroundDeep` | `#1E1E1E` | Window background, page background |
| `BackgroundPanel` | `#252526` | Content panels, DataGrid body |
| `BackgroundChrome` | `#2D2D30` | Title bar, step bar chrome, toolbar |
| `BackgroundAlt` | `#222222` | Alternating DataGrid rows |
| `BorderSubtle` | `#3C3C3C` | Borders between panels, DataGrid rows |
| `BorderStrong` | `#555555` | Default button borders |
| `Accent` | `#4FC3F7` | Active step, primary button border/text, links, highlights |
| `AccentBg` | `#1A3350` | Primary button background, active row tint |
| `AccentBgHover` | `#1E3D60` | Primary button hover |
| `Success` | `#4EC994` | Validation ✓, completed step circle border |
| `SuccessBg` | `#1C3A2A` | Completed step circle background |
| `Error` | `#F48771` | Validation ✗, error row tint border |
| `ErrorBg` | `#2A1A1A` | Error row background, danger button background |
| `ErrorBorder` | `#6A3333` | Danger button border |
| `Warning` | `#E9C46A` | Warning banners (reserved) |
| `TextPrimary` | `#CCCCCC` | Body text, DataGrid cell content |
| `TextMuted` | `#777777` | Descriptions, hints, inactive step labels |
| `TextDim` | `#555555` | Very inactive elements, separators |
| `TextHeader` | `#999999` | DataGrid column headers (uppercase) |

### Typography

- **Font family:** `Segoe UI Variable` with fallback `Segoe UI`
- **Body / cell text:** 12px, `TextPrimary`
- **Titles (tool name, home heading):** 16–20px, weight 600–700, `#E0E0E0`
- **Section labels / hints:** 11px, `TextMuted`
- **DataGrid column headers:** 10px, uppercase, letter-spacing 0.4px, `TextHeader`
- **Step label (active):** 11px, weight 600, `Accent`
- **Step label (inactive):** 11px, `TextDim`
- **Button text:** 11px, weight 600

### Corner Radius

- Cards, content panels, dialogs: **8px**
- Buttons, input fields, banners: **5px**
- Step pill circles: **50%** (fully round)
- DataGrid border: **5px** (outside wrapper)

### Spacing

- Window padding: **16px** all sides
- Card internal padding: **20px**
- Row height (DataGrid): **32px** minimum
- Gap between toolbar buttons: **8px**

---

## Implementation Approach

A single `Theme.xaml` `ResourceDictionary` merged into `App.xaml`. All colours defined as `SolidColorBrush` resources. All component styles (`Button`, `DataGrid`, `DataGridColumnHeader`, `DataGridRow`, `TextBox`, `Border`, `ScrollViewer`) defined once. Views reference named styles — no inline colours.

### File structure

```
ADTool/
  Themes/
    Theme.xaml          ← new: all brushes + style definitions
  App.xaml              ← merge Theme.xaml here
  Views/                ← all existing XAML files restyled to use named styles
```

### Resource naming convention

Brushes: `{Token}Brush` — e.g. `AccentBrush`, `BackgroundPanelBrush`  
Styles: `{Component}Style` where not set as implicit (default) style

---

## Component Specifications

### Window (`MainWindow.xaml`)

- `Background="{StaticResource BackgroundDeepBrush}"`
- Window chrome uses native WPF title bar (no custom chrome)
- Default size 960×640, min 720×480 — unchanged

### Home Screen (`HomeView.xaml`)

- Full-bleed `BackgroundDeep` background
- Centred `StackPanel`, heading "AD Tool" at 20px weight 700 `#E0E0E0`, subtitle at 12px `TextMuted`
- Two cards side-by-side, each: `BackgroundPanel` bg, `BorderSubtle` border, 8px radius, `border-top: 3px solid AccentBrush`, `20px` internal padding
- Icon placeholder: 32×32px `AccentBg` rounded square with simple shape indicator
- Launch button: outline-primary style (`AccentBg` bg, `Accent` border, `Accent` text, `Launch →`)

### Tool Shell — UPNToolView & AttrToolView

**Title bar row:**
- `BackgroundChrome` bg, `BorderSubtle` bottom border, 8px vertical padding
- Left: `← Home` hyperlink-style in `Accent` colour (triggers `ReturnHomeCommand` / equivalent back navigation)
- Centre: tool name in 12px weight 500 `TextPrimary`

**Step pill row:**
- `BackgroundPanel` bg, `BorderSubtle` bottom border, 11px vertical padding, 16px horizontal padding
- Steps laid out horizontally with a `28px × 1px` `BorderSubtle` connector line between each
- **Pending step:** 22px circle, `#2A2A2C` bg, `BorderStrong` border, number in `TextDim`
- **Active step:** 22px circle, `Accent` bg, number in `#1A1A1A` weight 700; label in `Accent` weight 600
- **Completed step:** 22px circle, `SuccessBg` bg, `Success` border, `✓` in `Success`; label in `TextDim`
- The step pill row is driven by the current `StepNumber` property on the shell ViewModel — a single `StepIndicator` UserControl is shared by both tool shells

**Content area:**
- `BackgroundDeep` bg, `16px` margin all sides
- `ContentControl` bound to `CurrentStep`

### DataGrid (implicit style applied globally)

- Background: `BackgroundPanel`
- Column header: `BackgroundChrome` bg, `TextHeader` foreground, 10px uppercase, `BorderSubtle` bottom border, 8px horizontal padding
- Row: 32px min height, `BackgroundPanel` bg; alternating rows use `BackgroundAlt`
- Selected row: `AccentBg` bg, `Accent` left border (2px)
- Cell padding: 8px horizontal, 6px vertical
- Grid lines: `BorderSubtle` horizontal only (no vertical grid lines)
- `CanUserResizeRows="False"`, `SelectionUnit="FullRow"`

**Status rows (Validate and Execute views):**
- Valid / success: no special row colour (default)
- Invalid / error: `ErrorBg` row background, `ErrorBorder` left border (2px)

### Buttons

Three named styles defined in `Theme.xaml`:

| Style key | Background | Border | Text |
|---|---|---|---|
| `PrimaryButtonStyle` | `AccentBg` | `Accent` | `Accent` |
| `DefaultButtonStyle` | `BackgroundChrome` | `BorderStrong` | `TextPrimary` |
| `DangerButtonStyle` | `ErrorBg` | `ErrorBorder` | `Error` |

All: 5px corner radius, 6px vertical / 14px horizontal padding, 11px weight 600 text, `Hand` cursor.  
Hover state: `AccentBgHover` for primary; `#363636` for default.  
Disabled state: 40% opacity.

`Button` implicit style defaults to `DefaultButtonStyle`. Primary and danger buttons opt in explicitly.

### TextBox (implicit style)

- `BackgroundChrome` bg, `BorderSubtle` border, 5px radius
- `TextPrimary` foreground, 12px
- Focus border: `Accent`
- Padding: 5px horizontal, 4px vertical

### Status banners (info, warning, error)

Used in Validate and Execute views to show progress or error summaries.

| Type | Background | Border | Text |
|---|---|---|---|
| Info | `#1A2A3A` | `#2A4A6A` | `#88C8E8` |
| Success | `#1A2A1A` | `#2A4A2A` | `#88C888` |
| Error | `ErrorBg` | `ErrorBorder` | `Error` |
| Warning | `#2A2A1A` | `#4A4A1A` | `Warning` |

5px radius, 8px vertical / 12px horizontal padding, 11px text, `BorderSubtle` left side thickened to 3px as accent stripe.

### Dialogs — AdBrowserDialog & AddColumnDialog

- Window background: `BackgroundDeep`
- Interior panels: `BackgroundPanel` with `BorderSubtle` borders
- Title: 14px weight 600 `TextPrimary`
- Buttons follow the standard three styles above
- `CornerRadius` on outer border: 4px (WPF `WindowStyle.None` not required — use standard chrome)

### Expander (error detail in Execute view)

- Header text: `Error` colour
- Expanded content panel: `ErrorBg` bg, `ErrorBorder` border, `ErrorBg` tint, 5px radius
- Arrow/toggle inherits implicit Expander style from `Theme.xaml`

---

## Step Indicator UserControl

To avoid duplicating the pill row markup across both tool shells, extract it into a shared `StepIndicatorControl` UserControl.

**Properties:**
- `Steps` — `IReadOnlyList<string>` step labels (set once, e.g. `["Input","Validate","Preview","Execute"]`)
- `CurrentStep` — `int` (1-based), bound to `CurrentStepNumber` on the shell ViewModel

Steps `1 … CurrentStep-1` render as completed (green ✓). Step `CurrentStep` renders as active (blue filled). Steps after render as pending (grey border).

**Behaviour:** purely visual, no commands. The shell ViewModel drives step transitions via existing `GoTo()`.

---

## Minimal ViewModel Additions Required

These are the only ViewModel changes needed to support the new UI — no logic changes:

- **`UPNToolViewModel` and `AttributeToolViewModel`:** add `CurrentStepNumber` (`int`, 1-based) computed from the current position in `_steps`. Used by `StepIndicatorControl`.
- **`UPNToolViewModel` and `AttributeToolViewModel`:** expose `ReturnHomeCommand` (`RelayCommand`) wrapping the existing `returnHome` callback. Used by the `← Home` link in the title bar.

---

## Screens Not Changing (structure)

- `AdBrowserDialog` structure (tree + list layout) — restyled only
- All Models and Services — zero changes
- Window size / min size — unchanged
- All existing data bindings and business-logic commands — unchanged

---

## Out of Scope

- Light/dark mode toggle
- Custom window chrome (no `WindowStyle.None`)
- Animations or transitions between steps
- Font installation (Segoe UI Variable ships with Windows 11; graceful fallback to Segoe UI on Windows 10)
