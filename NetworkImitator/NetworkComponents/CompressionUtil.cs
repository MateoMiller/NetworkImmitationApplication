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

    /// <summary>
    /// Создает сжатое сообщение на основе исходного контента
    /// </summary>
    public static Message CreateCompressedMessage(string fromIP, string toIP, byte[] content, string originalSenderIp)
    {
        var originalSize = content.Length;
        var compressedContent = Compress(content);
        
        // Сжимаем только если это дает выигрыш в размере
        if (compressedContent.Length < originalSize)
        {
            return new Message(fromIP, toIP, compressedContent, originalSenderIp, true, true);
        }
        
        // Иначе отправляем без сжатия
        return new Message(fromIP, toIP, content, originalSenderIp);
    }
    
    
    /// <summary>
    /// Распаковывает сжатое сообщение, если оно сжато (без измерения времени)
    /// </summary>
    public static Message DecompressMessageIfNeeded(Message message)
    {
        if (!message.IsCompressed)
        {
            return message;
        }
        
        byte[] decompressedContent = Decompress(message.Content);
        return new Message(
            message.FromIP, 
            message.ToIP, 
            decompressedContent, 
            message.OriginalSenderIp);
    }
}
