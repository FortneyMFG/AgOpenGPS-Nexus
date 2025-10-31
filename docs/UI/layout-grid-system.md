# Nexus Layout Grid and Dock System

This document captures the grid, dock, and persistence rules used by the Nexus layout
demo. Treat it as the reference when you translate the prototype into Avalonia/C# or any
other runtime host.

## 1. Global Coordinate System

- All usable UI pixels live inside a single rectangle that is `W × H` in size. There is no
  separate "workspace" versus "dock" world—everything occupies the same plane.
- Logical coordinates use a bottom-left origin. Positive *X* extends to the right and
  positive *Y* extends upward. Flip *Y* when mapping to toolkits that render from the
  top-left (Avalonia, HTML, etc.).

## 2. Grid Derivation

The grid is recomputed whenever the viewport or density preferences change.

### 2.1 Inputs

- `W`, `H`: usable width and height in pixels.
- `densityFactor`: global zoom. Values `< 1` create more, narrower columns; values `> 1`
  create fewer, wider columns.
- `minTapPx`: the smallest allowed column width in pixels (glove-friendly safety net).

### 2.2 Column Count

1. `AR = W / H` (aspect ratio).
2. `aspectCols = 6.6667 * AR + 0.6667` to bias landscape displays toward more columns.
3. `scaledCols = aspectCols / densityFactor` (smaller `densityFactor` ⇒ more columns).
4. `physicalMaxCols = max(1, floor(W / minTapPx))` to honor tap ergonomics.
5. `columns = round(scaledCols)` and clamp to `[3, 16]`.
6. `columns = min(columns, physicalMaxCols)` and enforce `columns ≥ 3`.

### 2.3 Grid Unit Sizes

- Horizontal grid width: `gridSizeX = W / columns`.
- Choose a whole number of half-height steps that tiles the viewport: `halfStepsY =
  max(1, round((2 * H) / gridSizeX))`.
- Vertical grid height: `gridSizeY = (2 * H) / halfStepsY`.
- Approximate whole rows available: `approxFullRows = halfStepsY / 2.0` (useful for
  docking clamps).

Every element snaps to these global units: integer column widths and row/half-row
heights.

## 3. Docks

A dock is a strip attached to one screen edge. Thickness is measured in grid units.

- Header (top) and bottom docks consume vertical units (`headerRows`, `bottomRows`).
- Left and right docks consume horizontal units (`leftCols`, `rightCols`).
- Example defaults: header `1.0` row (with a minimum of `0.5`), bottom `1.0` rows,
  side docks `1.0` columns. Side docks can collapse to `0`.

### 3.1 Pixel Conversion

```
headerPxH = headerRows * gridSizeY
bottomPxH = bottomRows * gridSizeY
leftPxW   = leftCols   * gridSizeX
rightPxW  = rightCols  * gridSizeX
```

### 3.2 Dock Rectangles (bottom-left origin)

- Header: `(x0, y0, x1, y1) = (0, H - headerPxH, W, H)`
- Bottom: `(0, 0, W, bottomPxH)`
- Left: `(0, bottomPxH, leftPxW, H - headerPxH)`
- Right: `(W - rightPxW, bottomPxH, W, H - headerPxH)`

These rectangles never overlap and create a frame. The remaining work area is the
rectangle bounded by `(leftPxW, bottomPxH)` and `(W - rightPxW, H - headerPxH)`; in the
demo it is drawn as a dashed overlay for diagnostics.

## 4. Safety Clamps

### 4.1 Vertical

If `headerRows + bottomRows` exceeds `approxFullRows`, scale both to fit:

```
scale = approxFullRows / (headerRows + bottomRows)
headerRows *= scale
bottomRows *= scale
```

Enforce `headerRows ≥ 0.5` afterward and clamp `bottomRows` to the remaining rows.

### 4.2 Horizontal

If `leftCols + rightCols` exceeds `columns`, scale both by `columns / (leftCols +
rightCols)`.

Recompute pixel sizes after each clamp.

## 5. Rendering Rules

The HTML demo draws in this order (multiply by a preview scale factor when shrinking the
canvas):

1. Global grid: cyan column lines, yellow row lines, faint half-row guides, and column/row
   indices.
2. Dock rectangles: translucent fills with no overlap, coordinates converted to
   top-left-origin space before rendering (`cssLeft = x0 * scale`, `cssTop = (H - y1) *
   scale`, etc.).
3. Work-area outline: dashed green rectangle showing remaining space.

## 6. Demo Controls

The prototype exposes the following controls:

- Viewport: `W` and `H` numeric inputs.
- Grid behavior: `densityFactor` slider and `minTapPx` slider.
- Dock thickness: sliders for header rows (min `0.5`), bottom rows (min `0`), left columns
  (min `0`), and right columns (min `0`).

Changing any value triggers the recomputation pipeline above.

## 7. Persistence Model

Store layout preferences in grid units so they translate to any resolution:

- `densityFactor`
- `minTapPx` (per device class or layout)
- Dock definitions, e.g.

```json
{
  "headerDock": { "rows": 1.0, "minRows": 0.5 },
  "bottomDock": { "rows": 1.0 },
  "leftDock":   { "cols": 1.0 },
  "rightDock":  { "cols": 1.0 }
}
```

Future panel placements will use the same grid units (`xCols`, `yRows`, `wCols`,
`hRows`) plus optional `dock` bindings and `zIndex` ordering.

## 8. Reimplementation Checklist

When porting to Avalonia or another host:

1. Read the viewport (`W`, `H`) and user preferences.
2. Derive the global grid and grid unit sizes.
3. Clamp dock units.
4. Build dock rectangles and (optionally) the residual work area.
5. Render elements in grid order, flipping *Y* for toolkit coordinates.
6. Persist only density/tap values and dock/panel grid units—never raw pixels.

This reproduces the demo behavior across screen sizes and hardware classes.
