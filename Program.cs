
using AngouriMath;
using NewtonV2.Classes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Globalization;
using static System.Console;
using Complex = System.Numerics.Complex;

namespace NewtonV2;
class Program
{
    static void Main(string[] args) {

        var programRuntime = Stopwatch.StartNew();
        string expression_input = "";
        double xmin = -2, xmax = 2;
        double ymin = -2, ymax = 2;
        string fileOutPath = "";
        int width = 0, height = 0, maxIter = 5;
        bool darkTheme = false;

        // Parse flags
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--expr":
                    expression_input = args[++i];
                    break;

                case "--res":
                    if (!TryParseResolution(args[++i], out width, out height))
                    {
                        WriteLine("Error: resolution must be WIDTHxHEIGHT, e.g. 1920x1080");
                        ShowHelp();
                    }
                    break;

                case "--iter":
                    if (!int.TryParse(args[++i], out maxIter) || maxIter <= 0)
                    {
                        WriteLine("Error: --iter must be a positive integer.");
                        ShowHelp();
                    }
                    break;

                case "--xmin":
                    xmin = double.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;

                case "--xmax":
                    xmax = double.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;

                case "--ymin":
                    ymin = double.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;

                case "--ymax":
                    ymax = double.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;

                case "--out":
                    fileOutPath = Path.Combine(args[++i], "fractal.png");
                    break;

                case "--dark":
                    darkTheme = true;
                    break;

                default:
                    WriteLine($"Unknown argument: {args[i]}");
                    ShowHelp();
                    return;
            }
        }

        // Required flags
        if (string.IsNullOrWhiteSpace(expression_input))
        {
            WriteLine("Error: --expr is required.");
            ShowHelp();
        }

        if (!expression_input.Contains("z"))
        {
            WriteLine("Error: expression must be a function of 'z'.");
            ShowHelp();
        }

        if (width <= 0 || height <= 0)
        {
            WriteLine("Error: --res WIDTHxHEIGHT is required.");
            ShowHelp();
        }

        if (maxIter <= 0)
        {
            WriteLine("Error: --iter is required.");
            ShowHelp();
        }

        // Output path default
        if (string.IsNullOrEmpty(fileOutPath))
        {
            fileOutPath = Path.Combine(Directory.GetCurrentDirectory(), "fractal.png");
        }


        string plain = LatexConverter.LatexToPlain(expression_input);

        WriteLine($"Parsed resolution: {width} x {height}");

        WriteLine("Creating bitmap buffer...");
        var image = new Image<Rgba32>(width, height);

        WriteLine("Parsing...");
        Entity fExpr = MathS.FromString(plain);
        WriteLine("Parsed expression: " + fExpr.ToString());

        var sw = Stopwatch.StartNew();

        WriteLine("Differentiating...");
        Entity dfExpr = fExpr.Differentiate("z");

        WriteLine($"Derivative took {sw.ElapsedMilliseconds / 1000} s");
        WriteLine("Parsed derivative: " + dfExpr.ToString());

        Func<Complex, Complex> f = null!;
        Func<Complex, Complex> df = null!;

        try
        {
            f = fExpr.Compile<Complex, Complex>("z");
            df = dfExpr.Compile<Complex, Complex>("z");
            // continue as normal
        }
        catch (KeyNotFoundException)
        {
            WriteLine("Error: invalid expression. Unknown symbol or malformed LaTeX. Expression must be f(z)");
            ShowHelp();
        }
        catch (Exception ex)
        {
            WriteLine($"Error: failed to parse expression: {ex.Message}");
            Environment.Exit(1);
        }

        List<Complex> discoveredRoots = new List<Complex>();
        WriteLine("Rendering fractal...");

        // Simple color palette (extendable)
        Rgba32[] palette =
        [
            new Rgba32(255, 80, 80, 255),    // red
            new Rgba32(80, 255, 80, 255),    // green
            new Rgba32(80, 80, 255, 255),    // blue
            new Rgba32(255, 255, 80, 255),   // yellow
            new Rgba32(255, 80, 255, 255),   // magenta
            new Rgba32(80, 255, 255, 255),   // cyan
            new Rgba32(255, 255, 255, 255),  // white
            new Rgba32(255, 160, 80, 255),   // orange
            new Rgba32(160, 255, 80, 255),   // lime
            new Rgba32(80, 255, 160, 255),   // aquamarine
            new Rgba32(80, 160, 255, 255),   // sky blue
            new Rgba32(160, 80, 255, 255),   // violet
            new Rgba32(255, 80, 160, 255),   // hot pink
            new Rgba32(255, 160, 255, 255),  // light magenta
            new Rgba32(160, 160, 255, 255)  // periwinkle
        ];
        Rgba32 divergeColor = new(0, 0, 0); // black

        // Simple darkcolor palette (extendable)
        Rgba32[] darkPalette =
        [
            new Rgba32(0, 180, 255, 100),    // electric cyan
            new Rgba32(80, 0, 200, 100),     // deep violet
            new Rgba32(160, 0, 255, 100),    // cosmic magenta
            new Rgba32(0, 100, 160, 100),    // rich blue
            new Rgba32(60, 0, 120, 100),     // indigo shadow
            new Rgba32(30, 30, 60, 100),     // near-black blue
            new Rgba32(200, 200, 255, 100),  // pale highlight
            new Rgba32(0, 140, 200, 100),    // deep aqua
            new Rgba32(40, 0, 160, 100),     // royal violet
            new Rgba32(120, 0, 200, 100),    // purple flare
            new Rgba32(0, 60, 120, 100),     // midnight blue
            new Rgba32(20, 20, 80, 100),     // navy shadow
            new Rgba32(100, 40, 160, 100),   // plum
            new Rgba32(0, 160, 220, 100),    // bright teal
            new Rgba32(150, 80, 220, 100)    // lavender glow
        ];
        Rgba32 darkDivergeColor = new(200, 200, 200); // grey

        sw = Stopwatch.StartNew();

        Parallel.For(0, height, py =>
        {
            for (int px = 0; px < width; px++)
            {
                // Map pixel → complex plane
                Complex z0 = PixelToComplex(
                    px, py,
                    width, height,
                    xmin, xmax,
                    ymin, ymax
                );

                // Run Newton iteration
                var (root, smooth) = FindRoot(f, df, z0, maxIter);

                // Classify the root
                int rootIndex;

                if (root == null)
                {
                    rootIndex = -1; // Diverged
                }
                else
                {
                    Complex clean = new Complex(
                        Math.Round(root.Value.Real, 2),
                        Math.Round(root.Value.Imaginary, 2)
                    );

                    lock (discoveredRoots)
                    {
                        rootIndex = ClassifyRoot(clean, discoveredRoots);
                    }
                }

                // Smooth shading
                if (rootIndex == -1)
                {
                    image[px, py] = darkTheme ? darkDivergeColor : divergeColor;
                }
                else
                {
                    // Base color from palette
                    var baseColor = darkTheme ? darkPalette[rootIndex % darkPalette.Length] : palette[rootIndex % palette.Length];

                    if (!double.IsFinite(smooth)) smooth = 0;
                    double t = Math.Clamp(smooth / maxIter, 0.0, 1.0);

                    // Gentle gamma curve (keeps midtones intact)
                    double gamma = darkTheme ? 0.6 : 1.1;
                    t = Math.Pow(t, gamma);

                    byte hr, hg, hb, a = 255;
                    // Create a softened highlight
                    if (!darkTheme)
                    {
                        hr = (byte)(baseColor.R + 20); if (hr > 255) hr = 255;
                        hg = (byte)(baseColor.G + 20); if (hg > 255) hg = 255;
                        hb = (byte)(baseColor.B + 20); if (hb > 255) hb = 255;
                    }
                    else {
                        hr = (byte)(baseColor.R + (255 - baseColor.R) * 0.2);
                        hg = (byte)(baseColor.G + (255 - baseColor.G) * 0.2);
                        hb = (byte)(baseColor.B + (255 - baseColor.B) * 0.2);
                        a = 100;
                    }
                    
                    // Blend base → highlight
                    byte r = (byte)(baseColor.R * (1 - t) + hr * t);
                    byte g = (byte)(baseColor.G * (1 - t) + hg * t);
                    byte b = (byte)(baseColor.B * (1 - t) + hb * t);
                    

                    image[px, py] = new Rgba32(r, g, b, a);
                }
            }
        });

        WriteLine($"Rendering took {sw.ElapsedMilliseconds / 1000} s");

        WriteLine("Saving image...");

        string path = string.IsNullOrEmpty(fileOutPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "fractal.png")
            : fileOutPath;

        // Ensure directory exists
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            WriteLine("Target directory do not exists. Creating it now...");
            Directory.CreateDirectory(dir);
        }

        image.Save(path);
        image.Dispose();

        WriteLine($"Saved at {path}, opening it now...");
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

        WriteLine($"Program finished in {sw.ElapsedMilliseconds / 1000} s, exiting...");
    }

    private static void ShowHelp()
    {
        WriteLine("Usage:");
        WriteLine("  NewtonV2.exe --expr \"<expression>\" --res <WIDTHxHEIGHT> --iter <maxIteration> [options]");
        WriteLine();
        WriteLine("Required arguments:");
        WriteLine("  --expr <expression>");
        WriteLine("                   A valid LaTeX-style complex expression in variable 'z'.");
        WriteLine("                   Example: \"(z^3)-1\", \"sin(z) + z^2\", \"(z^5) - (3*z) + 1\"");
        WriteLine();
        WriteLine("  --res <WIDTHxHEIGHT>");
        WriteLine("                   Output image resolution.");
        WriteLine("                   Example: 1920x1080, 3840x2160, 800x800");
        WriteLine();
        WriteLine("  --iter <maxIteration>");
        WriteLine("                   Maximum Newton iterations per pixel.");
        WriteLine("                   Higher values give smoother boundaries but increase render time.");
        WriteLine();
        WriteLine("Optional viewport arguments:");
        WriteLine("  --xmin <value>   Minimum real-axis value (default: -2)");
        WriteLine("  --xmax <value>   Maximum real-axis value (default:  2)");
        WriteLine("  --ymin <value>   Minimum imaginary-axis value (default: -2)");
        WriteLine("  --ymax <value>   Maximum imaginary-axis value (default:  2)");
        WriteLine("                   All values may be integers or decimals (e.g., -1.5, 2.5).");
        WriteLine();
        WriteLine("Optional output argument:");
        WriteLine("  --out <directory>");
        WriteLine("                    Directory where the output image will be saved.");
        WriteLine(@"                   Defaults to: <AppDir>\fractal.png");
        WriteLine(@"                   Example: --out C:\Users\username\Documents");
        WriteLine();
        WriteLine("  --dark");
        WriteLine("                    Changes colour-scheme to darker tones.");
        WriteLine();
        WriteLine("Examples:");
        WriteLine("  Use default range:");
        WriteLine("    NewtonV2.exe --expr \"(z^3)-1\" --res 3840x2160 --iter 20");
        WriteLine();
        WriteLine("  Use custom range:");
        WriteLine("    NewtonV2.exe --expr \"(z^3)-1\" --res 3840x2160 --iter 20 --xmin -4 --xmax 4 --ymin -4 --ymax 4");
        WriteLine();
        WriteLine("  Custom output directory with dark mode:");
        WriteLine("    NewtonV2.exe --expr \"(z^3)-1\" --res 1920x1080 --iter 30 --out D:\\Temp --dark");
        WriteLine();

        Environment.Exit(0);
    }

    static (Complex? root, double smooth) FindRoot(
        Func<Complex, Complex> f,
        Func<Complex, Complex> df,
        Complex z0,
        int maxIter = 20,
        double eps = 1e-6)
    {
        Complex z = z0;
        double smooth = 0;

        for (int i = 0; i < maxIter; i++)
        {
            Complex fz = f(z);
            double absF = Complex.Abs(fz);

            if (absF < eps)
            {
                // Smooth iteration count
                smooth = i + 1 - Math.Log(Math.Log(1.0 / absF)) / Math.Log(2.0);
                return (z, smooth);
            }

            Complex dfz = df(z);
            if (Complex.Abs(dfz) < eps)
                return (null, 0);

            z = z - fz / dfz;
        }

        return (null, 0);
    }

    static Complex PixelToComplex(int px, int py, int width, int height,
                              double xmin, double xmax,
                              double ymin, double ymax)
    {
        double x = xmin + (xmax - xmin) * px / (width - 1);
        double y = ymin + (ymax - ymin) * py / (height - 1);
        return new Complex(x, y);
    }

    static int ClassifyRoot(Complex root, List<Complex> knownRoots, double eps = 0.05)
    {
        // Compare with existing roots
        for (int i = 0; i < knownRoots.Count; i++)
        {
            if (Complex.Abs(knownRoots[i] - root) < eps)
                return i; // existing root
        }

        // New root discovered
        knownRoots.Add(root);
        return knownRoots.Count - 1;
    }

    // I know it could be simpler, smarter and better but who cares if it works right?
    static bool TryParseResolution(string input, out int width, out int height)
    {
        width = height = 0;

        var parts = input.Split('x', 'X');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out width) &&
               int.TryParse(parts[1], out height) &&
               width > 0 &&
               height > 0;
    }


}