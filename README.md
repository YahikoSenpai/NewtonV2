# NewtonV2 — Newton Fractal Renderer

## Usage
NewtonV2.exe `"<expression>"` `<resolution>` `<maxIteration>` [xmin xmax ymin ymax]

---

## Arguments

### `<expression>`
A valid LaTeX-style complex expression in the variable **`z`**.

Examples:
- `(z^3)-1`
- `sin(z) + z^2`
- `(z^5) - (3*z) + 1`

---

### `<resolution>`
Output image size in `WIDTHxHEIGHT` format.

Examples:
- `1920x1080`
- `3840x2160`
- `800x800`

---

### `<maxIteration>`
Maximum Newton iterations per pixel.

Higher values produce smoother boundaries but increase render time.

---

## Optional Range Arguments
xmin xmax ymin ymax

Defines the complex-plane viewport.

- All values may be integers or decimals (e.g., `-1.5`, `2.5`)
- Defaults to: `-2 2 -2 2`

Example:
-4 4 -4 4

---

## Examples

### Use default range
NewtonV2.exe "(z^3)-1" 3840x2160 20

### Use custom range
NewtonV2.exe "(z^3)-1" 3840x2160 20 -4 4 -4 4
