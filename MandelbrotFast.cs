using System.Diagnostics;
using System.Numerics;
using System.Text;

// Fast version: all cores + SIMD
static class MandelbrotFast
{
    public static void Run()
    {
        var input = Input.Load("input.txt");
        if (input == null) return;

        int width = input.Int("width"), height = input.Int("height"), maxN = input.Int("maxN");
        double minR = input.Double("minR"), maxR = input.Double("maxR");
        double minI = input.Double("minI"), maxI = input.Double("maxI");
        string fileName = input.Text("file");

        //Format chosen by the file extension
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".png" or ".ppm")) throw new InvalidDataException($"File '{fileName}' must have a .png or .ppm extension");
        if (File.Exists(fileName)) fileName = fileName[..^ext.Length] + "_new" + ext;

        var sw = Stopwatch.StartNew();
        double stepR = (maxR - minR) / width, stepI = (maxI - minI) / height;
        var iters = new int[width * height];

        Parallel.For(0, height, y => ComputeRow(iters.AsSpan(y * width, width), y * stepI + minI, minR, stepR, maxN));
        var computeTime = sw.Elapsed;

        if (ext == ".png") WritePng(fileName, iters, width, height, maxN);
        else WritePpm(fileName, iters, width, height, maxN);

        Console.WriteLine($"Saved {fileName} | compute: {computeTime.TotalMilliseconds:F0} ms | total: {sw.Elapsed.TotalMilliseconds:F0} ms");
        Console.WriteLine("Done!");
    }

    //n is in 0..maxN, so the color for each n is computed once and kept as ready-made bytes
    static void WritePng(string fileName, int[] iters, int width, int height, int maxN)
    {
        var lut = new byte[(maxN + 1) * 3];
        for (int n = 0; n <= maxN; n++)
        {
            var (r, g, b) = Color(n, maxN);
            (lut[n * 3], lut[n * 3 + 1], lut[n * 3 + 2]) = ((byte)r, (byte)g, (byte)b);
        }

        Png.Write(fileName, width, height, (y, row) =>
        {
            for (int x = 0; x < width; x++) lut.AsSpan(iters[y * width + x] * 3, 3).CopyTo(row[(x * 3)..]);
        });
    }

    static void WritePpm(string fileName, int[] iters, int width, int height, int maxN)
    {
        var lut = new byte[maxN + 1][];
        for (int n = 0; n <= maxN; n++)
        {
            var (r, g, b) = Color(n, maxN);
            lut[n] = Encoding.ASCII.GetBytes($"{r} {g} {b} ");
        }

        using var fout = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
        fout.Write(Encoding.ASCII.GetBytes($"P3\n{width} {height}\n255\n"));
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++) fout.Write(lut[iters[y * width + x]]);
            fout.WriteByte((byte)'\n');
        }
    }

    //Color mapping - change it and experiment (leave exactly one variant uncommented)
    //n = iteration count (0..maxN), n == maxN means the point is inside the set
    internal static (int r, int g, int b) Color(int n, int maxN)
    {
        //Black inside of the set - works with every variant below
        //if (n == maxN) return (0, 0, 0);

        int r = n * n % 256;
        int g = 28 % 256;
        int b = n % 256;

        //int r = (int)Math.Sin(n) * 3 % 256;
        //int g = n / 2 % 256;
        //int b = (int)Math.Cos(n) * n % 256;

        //int r = (int)Math.Sin(n) * n % 256;
        //int g = n * 2 % 256;
        //int b = (int)Math.Cos(n) % 256;

        //int r = n * n % 256;
        //int g = n * n % 256;
        //int b = n * n % 256;

        // --- Interesting palettes ---
        // Variants with "t" use a logarithmic scale (t from 0 to 1), so they work with any maxN and zoom.

        //Rainbow - smooth sine transitions, cycle every ~63 iterations (change 0.1 to change band density)
        //int r = (int)(Math.Sin(0.1 * n) * 127 + 128);
        //int g = (int)(Math.Sin(0.1 * n + 2.094) * 127 + 128);
        //int b = (int)(Math.Sin(0.1 * n + 4.188) * 127 + 128);

        //Fire - black -> red -> yellow -> white
        //double t = Math.Log(n + 1) / Math.Log(maxN + 1);
        //int r = (int)(255 * Math.Clamp(t * 3, 0, 1));
        //int g = (int)(255 * Math.Clamp(t * 3 - 1, 0, 1));
        //int b = (int)(255 * Math.Clamp(t * 3 - 2, 0, 1));

        //Ice - navy -> sky blue -> white
        //double t = Math.Log(n + 1) / Math.Log(maxN + 1);
        //int r = (int)(255 * t * t);
        //int g = (int)(255 * t);
        //int b = (int)(255 * Math.Sqrt(t));

        //Bernstein polynomials - classic smooth navy/orange palette
        //double t = Math.Log(n + 1) / Math.Log(maxN + 1);
        //int r = (int)(9 * (1 - t) * t * t * t * 255);
        //int g = (int)(15 * (1 - t) * (1 - t) * t * t * 255);
        //int b = (int)(8.5 * (1 - t) * (1 - t) * (1 - t) * t * 255);

        //Ultra Fractal - blue-gold, 5 colors blending smoothly, cycle every 64 iterations
        //int[,] pal = { { 0, 7, 100 }, { 32, 107, 203 }, { 237, 255, 255 }, { 255, 170, 0 }, { 0, 2, 0 } };
        //double pos = n % 64 / 64.0 * 5;
        //int i0 = (int)pos, i1 = (i0 + 1) % 5;
        //double f = pos - i0;
        //int r = (int)(pal[i0, 0] + (pal[i1, 0] - pal[i0, 0]) * f);
        //int g = (int)(pal[i0, 1] + (pal[i1, 1] - pal[i0, 1]) * f);
        //int b = (int)(pal[i0, 2] + (pal[i1, 2] - pal[i0, 2]) * f);

        //Psychedelic - each channel jumps at a different rate
        //int r = n * 7 % 256;
        //int g = n * 13 % 256;
        //int b = n * 23 % 256;

        //Grayscale
        //int r = (int)(255 * Math.Log(n + 1) / Math.Log(maxN + 1));
        //int g = r;
        //int b = r;

        //Zebra - black and white stripes every iteration
        //int r = n % 2 * 255;
        //int g = r;
        //int b = r;

        return (r, g, b);
    }

    static void ComputeRow(Span<int> row, double ci, double minR, double stepR, int maxN)
    {
        int lanes = Vector<double>.Count; // 4 on AVX2, 8 on AVX-512
        var vci = new Vector<double>(ci);
        var four = new Vector<double>(4.0);
        Span<double> crs = stackalloc double[lanes];

        int x = 0;
        for (; x <= row.Length - lanes; x += lanes)
        {
            for (int k = 0; k < lanes; k++) crs[k] = (x + k) * stepR + minR;
            var cr = new Vector<double>(crs);
            Vector<double> zr = Vector<double>.Zero, zi = Vector<double>.Zero;
            var count = Vector<long>.Zero;

            for (int i = 0; i < maxN; i++)
            {
                var zr2 = zr * zr;
                var zi2 = zi * zi;
                var active = Vector.LessThan(zr2 + zi2, four); // -1 where the point has not escaped yet
                if (Vector.EqualsAll(active, Vector<long>.Zero)) break;
                count -= active;
                //Escaped points are frozen so the result matches the scalar version exactly
                var mask = Vector.AsVectorDouble(active);
                var newZi = 2.0 * zr * zi + vci;
                zr = Vector.ConditionalSelect(mask, zr2 - zi2 + cr, zr);
                zi = Vector.ConditionalSelect(mask, newZi, zi);
            }

            for (int k = 0; k < lanes; k++) row[x + k] = (int)count[k];
        }

        //End of the row that does not fill a whole vector
        for (; x < row.Length; x++) row[x] = FindMandelbrot(x * stepR + minR, ci, maxN);
    }

    internal static int FindMandelbrot(double cr, double ci, int maxIterations)
    {
        int i = 0;
        double zr = 0.0, zi = 0.0;
        while (i < maxIterations && zr * zr + zi * zi < 4.0)
        {
            double temp = zr * zr - zi * zi + cr;
            zi = 2.0 * zr * zi + ci;
            zr = temp;
            i++;
        }
        return i;
    }
}
