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

    private MetricsCollector() { }

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
            
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении метрик: {ex.Message}", "Ошибка", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    private void SaveClientMetricsToCsv(string filePath)
    {
        var clientMetrics = GetAllClientMetrics();
        if (!clientMetrics.Any())
            return;
        
        var csv = new StringBuilder();
        csv.AppendLine("ClientIp,State,TimeInCurrentState,TotalElapsedTime,QueuedMessagesCount,FileTransferProgress,FileTransferStatus");
        foreach (var metric in clientMetrics)
        {
            csv.AppendLine($"{metric.ClientIp},{metric.State},{metric.TimeInCurrentState.TotalMilliseconds},{metric.TotalElapsedTime.TotalMilliseconds},{metric.QueuedMessagesCount},{metric.FileTransferProgress},\"{metric.FileTransferStatus}\"");
        }
        
        File.WriteAllText(filePath, csv.ToString());
    }
    
    private void SaveServerMetricsToCsv(string filePath)
    {
        var serverMetrics = GetAllServerMetrics();
        if (!serverMetrics.Any())
            return;
        
        var csv = new StringBuilder();
        csv.AppendLine("ServerIp,ProcessingLoad,QueuedMessagesCount,TotalLoad,ClientContextStates");
        foreach (var metric in serverMetrics)
        {
            var clientContextsStr = string.Join(";", metric.ClientContextStates.Select(x => $"{x.Key}:{x.Value}"));
            csv.AppendLine($"{metric.ServerIp},{metric.ProcessingLoad},{metric.QueuedMessagesCount},{metric.TotalLoad},\"{clientContextsStr}\"");
        }
        
        File.WriteAllText(filePath, csv.ToString());
    }
    
    private void SaveMessageMetricsToCsv(string filePath)
    {
        var messageMetrics = GetAllMessageMetrics();
        if (!messageMetrics.Any())
            return;
        
        var csv = new StringBuilder();
        csv.AppendLine("MessageId,FromIP,ToIP,OriginalSenderIp,State,ProcessorType,ProcessorIp,SizeInBytes,IsCompressed,IsFinalMessage,CreatedAt,LastUpdatedAt,ProcessingTime");
        foreach (var metric in messageMetrics)
        {
            csv.AppendLine($"{metric.MessageId},{metric.FromIP},{metric.ToIP},{metric.OriginalSenderIp},{metric.State},{metric.ProcessorType},{metric.ProcessorIp},{metric.SizeInBytes},{metric.IsCompressed},{metric.IsFinalMessage},{metric.CreatedAt},{metric.LastUpdatedAt?.ToString() ?? "null"},{metric.ProcessingTime.TotalMilliseconds}");
        }
        
        File.WriteAllText(filePath, csv.ToString());
    }
}
