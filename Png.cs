using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

// Minimal PNG writer (RGB, 8 bits per channel) without external libraries
static class Png
{
    // fillRow(y, row) fills the row with bytes R G B R G B ...
    public static void Write(string path, int width, int height, Action<int, Span<byte>> fillRow)
    {
        // ponytail: whole compressed image in RAM and a single IDAT chunk (~2 GB limit after compression), split into IDATs on the fly for larger images
        var idat = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionLevel.Fastest, leaveOpen: true))
        {
            var row = new byte[1 + width * 3]; // first byte of each row = filter 0 (none)
            for (int y = 0; y < height; y++)
            {
                fillRow(y, row.AsSpan(1));
                z.Write(row);
            }
        }

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bits per channel
        header[9] = 2; // RGB

        using var f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20);
        f.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Chunk(f, "IHDR", header);
        Chunk(f, "IDAT", idat.GetBuffer().AsSpan(0, (int)idat.Length));
        Chunk(f, "IEND", []);
    }

    static void Chunk(Stream s, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buf, data.Length);
        s.Write(buf);
        var t = Encoding.ASCII.GetBytes(type);
        s.Write(t);
        s.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(buf, ~Crc(Crc(~0u, t), data));
        s.Write(buf);
    }

    static readonly uint[] crcTable = Enumerable.Range(0, 256).Select(n =>
    {
        uint c = (uint)n;
        for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    static uint Crc(uint c, ReadOnlySpan<byte> data)
    {
        foreach (var b in data) c = crcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c;
    }
}
