using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace NetworkImitator.NetworkComponents;

public static class CompressionUtil
{
    public static byte[] Compress(byte[] data)
    {
        using var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return compressedStream.ToArray();
    } 

    public static byte[] Decompress(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var decompressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            gzipStream.CopyTo(decompressedStream);
        }
        return decompressedStream.ToArray();
    }

    public static (byte[] decompressedData, TimeSpan Elapsed) DecompressAndMeasure(this byte[] compressedData)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var decompressedData = Decompress(compressedData);
        
        stopwatch.Stop();

        return (decompressedData, stopwatch.Elapsed);
    }

    public static (byte[] compressedData, TimeSpan Elapsed) CompressAndMeasure(this byte[] compressedData)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        var decompressedData = Compress(compressedData);
        
        stopwatch.Stop();

        return (decompressedData, stopwatch.Elapsed);
    }
}
