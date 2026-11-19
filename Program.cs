using System.Globalization;

// Settings in input.txt, logic in MandelbrotFast.cs
try
{
    MandelbrotFast.Run();
}
catch (Exception e) when (e is FormatException or InvalidDataException or IOException)
{
    Console.WriteLine($"Error: {e.Message}");
}

// Keep the window open when started by double-click
if (!Console.IsInputRedirected)
{
    Console.WriteLine("Press Enter to close.");
    Console.ReadLine();
}

// Settings file in "key = value" format, '#' starts a comment
sealed class Input
{
    readonly Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
    readonly string path;

    Input(string path) => this.path = path;

    public static Input? Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Cannot read {path}! It must be in the folder you run the program from.");
            return null;
        }

        var input = new Input(path);
        foreach (var line in File.ReadLines(path))
        {
            var text = line.Split('#')[0];
            int eq = text.IndexOf('=');
            if (eq > 0) input.values[text[..eq].Trim()] = text[(eq + 1)..].Trim();
        }
        return input;
    }

    public string Text(string key) =>
        values.TryGetValue(key, out var v) ? v : throw new InvalidDataException($"Missing '{key}' in {path}");

    public int Int(string key) => int.Parse(Text(key));

    // A comma also works as the decimal separator (-0,9978)
    public double Double(string key) => double.Parse(Text(key).Replace(',', '.'), CultureInfo.InvariantCulture);
}
