# NewtonV2 — Newton Fractal Renderer

## Usage

```
NewtonV2.exe --expr "<expression>" --res <WIDTHxHEIGHT> --iter <maxIteration> [options]
```

---

## Required Arguments

### `--expr <expression>`
A valid LaTeX‑style complex expression in the variable **`z`**.

Examples:
- `(z^3)-1`
- `sin(z) + z^2`
- `(z^5) - (3*z) + 1`

---

### `--res <WIDTHxHEIGHT>`
Output image resolution.

Examples:
- `1920x1080`
- `3840x2160`
- `800x800`

---

### `--iter <maxIteration>`
Maximum Newton iterations per pixel.

Higher values produce smoother boundaries but increase render time.

---

## Optional Viewport Arguments

These define the complex‑plane region to render.

```
--xmin <value>
--xmax <value>
--ymin <value>
--ymax <value>
```

Defaults:
- `--xmin -2`
- `--xmax 2`
- `--ymin -2`
- `--ymax 2`

Values may be integers or decimals (e.g., `-1.5`, `2.5`).

Example:
```
--xmin -4 --xmax 4 --ymin -4 --ymax 4
```

---

## Optional Output Argument

### `--out <directory>`
Directory where the output image will be saved.

Defaults to:
```
<AppDir>\fractal.png
```

Example:
```
--out C:\Users\username\Documents
```

---

## Examples

### Use default range
```
NewtonV2.exe --expr "(z^3)-1" --res 3840x2160 --iter 20
```

### Use custom range
```
NewtonV2.exe --expr "(z^3)-1" --res 3840x2160 --iter 20 --xmin -4 --xmax 4 --ymin -4 --ymax 4
```

### Custom output directory
```
NewtonV2.exe --expr "(z^3)-1" --res 1920x1080 --iter 30 --out D:\Temp
```