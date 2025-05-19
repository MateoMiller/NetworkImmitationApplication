using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace NetworkImitator.NetworkComponents.Metrics;

public class MetricsCollector
{
    private static readonly Lazy<MetricsCollector> _instance = new(() => new MetricsCollector());

    public static MetricsCollector Instance => _instance.Value;

    private readonly List<ClientMetrics> _currentClientMetrics = new();
    private readonly List<ServerMetrics> _currentServerMetrics = new();
    private readonly List<MessageMetrics> _currentMessageMetrics = new();
    private readonly List<ConnectionMetrics> _currentConnectionMetrics = new();

    private MetricsCollector()
    {
    }

    public void AddConnectionMetrics(ConnectionMetrics metrics)
    {
        _currentConnectionMetrics.Add(metrics);
    }

    public IReadOnlyCollection<ConnectionMetrics> GetAllConnectionMetrics()
    {
        return _currentConnectionMetrics;
    }

    
    public void AddClientMetrics(ClientMetrics metrics)
    {
        _currentClientMetrics.Add(metrics);
    }

    public void AddServerMetrics(ServerMetrics metrics)
    {
        _currentServerMetrics.Add(metrics);
    }

    public void AddMessageMetrics(MessageMetrics metrics)
    {
        _currentMessageMetrics.Add(metrics);
    }

    public IReadOnlyCollection<ClientMetrics> GetAllClientMetrics()
    {
        return _currentClientMetrics;
    }

    public IReadOnlyCollection<ServerMetrics> GetAllServerMetrics()
    {
        return _currentServerMetrics;
    }

    public IReadOnlyCollection<MessageMetrics> GetAllMessageMetrics()
    {
        return _currentMessageMetrics;
    }

    public bool SaveMetricsToFile(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            SaveClientMetricsToCsv(Path.Combine(folderPath, $"client_metrics_{timestamp}.csv"));
            SaveServerMetricsToCsv(Path.Combine(folderPath, $"server_metrics_{timestamp}.csv"));
            SaveMessageMetricsToCsv(Path.Combine(folderPath, $"message_metrics_{timestamp}.csv"));
            SaveConnectionMetrics(Path.Combine(folderPath, $"connection_metrics_{timestamp}.csv"));

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении метрик: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void SaveClientMetricsToCsv(string filePath)
    {
        var clientMetrics = GetAllClientMetrics();
        if (!clientMetrics.Any())
            return;

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("ClientIp,State,TimeInCurrentState,TotalElapsedTime,QueuedMessagesCount,FileTransferProgress,FileTransferStatus");
        foreach (var metric in clientMetrics)
        {
            writer.WriteLine($"{metric.ClientIp},{metric.State},{metric.TimeInCurrentState.TotalMilliseconds},{metric.TotalElapsedTime.TotalMilliseconds},{metric.QueuedMessagesCount},{metric.FileTransferProgress},\"{metric.FileTransferStatus}\"");
        }
    }
    
    private void SaveServerMetricsToCsv(string filePath)
    {
        var serverMetrics = GetAllServerMetrics();
        if (!serverMetrics.Any())
            return;

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("ServerIp,TotalElapsedTime,ProcessingLoad,QueuedMessagesCount,TotalLoad");
        foreach (var metric in serverMetrics)
        {
            writer.WriteLine($"{metric.ServerIp},{metric.TotalElapsedTime.TotalMilliseconds},{metric.ProcessingLoad},{metric.QueuedMessagesCount},{metric.TotalLoad}");
        }
    }

    private void SaveMessageMetricsToCsv(string filePath)
    {
        var messageMetrics = GetAllMessageMetrics();
        if (!messageMetrics.Any())
            return;

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("MessageId,FromIP,ToIP,OriginalSenderIp,State,ProcessorType,SizeInBytes,IsCompressed,IsFinalMessage,TotalElapsed");
        foreach (var metric in messageMetrics)
        {
            writer.WriteLine($"{metric.MessageId},{metric.FromIP},{metric.ToIP},{metric.OriginalSenderIp},{metric.State},{metric.ProcessorType},{metric.SizeInBytes},{metric.IsCompressed},{metric.IsFinalMessage},{metric.TotalElapsed.TotalMilliseconds}");
        }
    }

    private void SaveConnectionMetrics(string filePath)
    {
        var connectionMetrics = GetAllConnectionMetrics();
        if (!connectionMetrics.Any())
            return;

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine("ConnectionName,ElapsedTime,MessagesCount,TotalMessagesSize");
        foreach (var metric in connectionMetrics)
        {
            writer.WriteLine($"{metric.ConnectionName},{metric.ElapsedTime.TotalMilliseconds},{metric.MessagesCount},{metric.TotalMessagesSize}");
        }
    }

}
