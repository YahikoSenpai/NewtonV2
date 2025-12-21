
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
        string expression_input;
        string resInput;
        int maxIter;
        double xmin = -2, xmax = 2;
        double ymin = -2, ymax = 2;

        // Need at least 3 arguments
        if (args.Length < 3)
        {
            ShowHelp();
        }

        // Required args
        expression_input = args[0];
        if (string.IsNullOrWhiteSpace(expression_input))
        {
            WriteLine("Error: <expression> cannot be empty.");
            ShowHelp();
        }

        if (!expression_input.Contains("z"))
        {
            WriteLine("Error: expression must contain variable 'z'.");
            ShowHelp();
        }


        resInput = args[1];
        if (!TryParseResolution(resInput, out int width, out int height))
        {
            WriteLine("Error: resolution must be in WIDTHxHEIGHT format, e.g. 1920x1080");
            ShowHelp();
        }

        if (!int.TryParse(args[2], out maxIter) || maxIter <= 0)
        {
            WriteLine("Error: <maxIteration> must be a positive integer.");
            ShowHelp();
        }

        // Optional range
        if (args.Length == 7)
        {
            if (!double.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out xmin) ||
                !double.TryParse(args[4], NumberStyles.Float, CultureInfo.InvariantCulture, out xmax) ||
                !double.TryParse(args[5], NumberStyles.Float, CultureInfo.InvariantCulture, out ymin) ||
                !double.TryParse(args[6], NumberStyles.Float, CultureInfo.InvariantCulture, out ymax))
            {
                WriteLine("Error: xmin xmax ymin ymax must be valid decimal numbers.");
                ShowHelp();
            }

        }
        else if (args.Length != 3)
        {
            ShowHelp();
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
        Rgba32[] palette = new Rgba32[]
        {
            new Rgba32(255, 80, 80),   // red
            new Rgba32(80, 255, 80),   // green
            new Rgba32(80, 80, 255),   // blue
            new Rgba32(255, 255, 80),  // yellow
            new Rgba32(255, 80, 255),  // magenta
            new Rgba32(80, 255, 255),  // cyan
            new Rgba32(255, 255, 255), // white
        };
        Rgba32 divergeColor = new Rgba32(0, 0, 0); // black

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
                    image[px, py] = divergeColor;
                }
                else
                {
                    // Base color from palette
                    var baseColor = palette[rootIndex % palette.Length];

                    if (!double.IsFinite(smooth)) smooth = 0;
                    double t = Math.Clamp(smooth / maxIter, 0.0, 1.0);

                    // Gentle gamma curve (keeps midtones intact)
                    double gamma = 1.1;
                    t = Math.Pow(t, gamma);

                    // Create a softened highlight (not pure white)
                    byte hr = (byte)(baseColor.R + 20); if (hr > 255) hr = 255;
                    byte hg = (byte)(baseColor.G + 20); if (hg > 255) hg = 255;
                    byte hb = (byte)(baseColor.B + 20); if (hb > 255) hb = 255;

                    // Blend base → highlight
                    byte r = (byte)(baseColor.R * (1 - t) + hr * t);
                    byte g = (byte)(baseColor.G * (1 - t) + hg * t);
                    byte b = (byte)(baseColor.B * (1 - t) + hb * t);

                    image[px, py] = new Rgba32(r, g, b);
                }
            }
        });

        WriteLine($"Rendering took {sw.ElapsedMilliseconds / 1000} s");

        WriteLine("Saving image...");
        image.Save("fractal.png");
        image.Dispose();
        WriteLine("Saved as fractal.png, opening it now...");

        Process.Start(new ProcessStartInfo("fractal.png") { UseShellExecute = true });

        WriteLine($"Program finished in {sw.ElapsedMilliseconds / 1000} s, exiting...");
    }

    private static void ShowHelp()
    {
        WriteLine("Usage:");
        WriteLine("  NewtonV2.exe \"<expression>\" <resolution> <maxIteration> [xmin xmax ymin ymax]");
        WriteLine();
        WriteLine("Arguments:");
        WriteLine("  <expression>     A valid LaTeX-style complex expression in variable 'z'.");
        WriteLine("                   Example: \"(z^3)-1\", \"sin(z) + z^2\", \"(z^5) - (3*z) + 1\"");
        WriteLine();
        WriteLine("  <resolution>     Output image size in WIDTHxHEIGHT format.");
        WriteLine("                   Example: 1920x1080, 3840x2160, 800x800");
        WriteLine();
        WriteLine("  <maxIteration>   Maximum Newton iterations per pixel.");
        WriteLine("                   Higher values give smoother boundaries but increase render time.");
        WriteLine();
        WriteLine("Optional range arguments:");
        WriteLine("  xmin xmax ymin ymax");
        WriteLine("                   Defines the complex-plane viewport.");
        WriteLine("                   All values may be integers or decimals (e.g., -1.5, 2.5).");
        WriteLine("                   Defaults to: -2 2 -2 2");
        WriteLine("                   Example: -4 4 -4 4");
        WriteLine();
        WriteLine("Examples:");
        WriteLine("  Use default range:");
        WriteLine("    NewtonV2.exe \"(z^3)-1\" 3840x2160 20");
        WriteLine();
        WriteLine("  Use custom range:");
        WriteLine("    NewtonV2.exe \"(z^3)-1\" 3840x2160 20 -4 4 -4 4");
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