# Mandelbrot Fractal Generator (C#)

Renders a region of the Mandelbrot set and saves it as a `.png` or `.ppm` image. It computes on all CPU cores using SIMD and uses little RAM.

## Download (no .NET installation needed)

1. Go to [Releases](https://github.com/dodskygge/MandelbrotGenerator/releases/latest) and download the zip for your system:
   - `MandelbrotGenerator-win-x64.zip` – Windows
   - `MandelbrotGenerator-linux-x64.zip` – Linux
   - `MandelbrotGenerator-osx-arm64.zip` – macOS (Apple Silicon)
2. Unzip it. The folder contains the program and `input.txt` with the settings.
3. Run `MandelbrotGenerator.exe` (Windows, double-click) or `./MandelbrotGenerator` from a terminal (Linux/macOS; run `chmod +x MandelbrotGenerator` if needed).

The image is saved in the same folder. The program reads `input.txt` from the current folder, so run it from the folder it is in.

## Running from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet run -c Release
```

## Settings

Everything is set in `input.txt`: image size, iteration count, the region to render and the output file name. Each value is explained in a comment in the file. If the output file already exists, the image is saved as `<name>_new.png` (or `_new.ppm`).

Note: the default 12000×12000 is a large image and takes a while to compute. For a quick test, lower `width` and `height`.

## Colors

Colors are set in the `Color` function in `MandelbrotFast.cs` (requires running from source). Comment out the three active `int r/g/b` lines and uncomment the palette you want.

## Image format

The format is chosen by the file extension in the `file` setting in `input.txt`:

- `.png` (default)
- `.ppm` – plain text and very large (12000×12000 is over 1 GB). You can open it in IrfanView, GIMP or XnView.
