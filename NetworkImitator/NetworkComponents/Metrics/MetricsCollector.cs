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

    
    private StreamWriter _clientWriter;
    private StreamWriter _serverWriter;
    private StreamWriter _messageWriter;
    private StreamWriter _connectionWriter;

    private MetricsCollector()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var folderPath = $"C:\\Users\\yurii\\Desktop\\метрики\\{timestamp}";

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var clientPath = Path.Combine(folderPath, $"client_metrics.csv");
        var serverPath = Path.Combine(folderPath, $"server_metrics.csv");
        var messagePath = Path.Combine(folderPath, $"message_metrics.csv");
        var connectionPath = Path.Combine(folderPath, $"connection_metrics.csv");
        
        _clientWriter = new StreamWriter(clientPath, false, Encoding.UTF8);
        _serverWriter = new StreamWriter(serverPath, false, Encoding.UTF8);
        _messageWriter = new StreamWriter(messagePath, false, Encoding.UTF8);
        _connectionWriter = new StreamWriter(connectionPath, false, Encoding.UTF8);

        _clientWriter.WriteLine("ClientIp,State,TimeInCurrentState,TotalElapsedTime,QueuedMessagesCount,FileTransferProgress");
        _serverWriter.WriteLine("ServerIp,TotalElapsedTime,ProcessingLoad,QueuedMessagesCount,TotalLoad");
        _connectionWriter.WriteLine("ConnectionName,ElapsedTime,MessagesCount,TotalMessagesSize");
        _messageWriter.WriteLine("MessageId,OriginalSenderIp,State,ProcessorType,SizeInBytes,IsCompressed,IsFinalMessage,TotalElapsed");
    }

    public void AddConnectionMetrics(ConnectionMetrics metric)
    {
        _connectionWriter.WriteLine($"{metric.ConnectionName},{metric.ElapsedTime.TotalNanoseconds},{metric.MessagesCount},{metric.TotalMessagesSize}");
    }
    
    public void AddClientMetrics(ClientMetrics metric)
    {
        _clientWriter.WriteLine($"{metric.ClientIp},{metric.State},{metric.TimeInCurrentState.TotalNanoseconds},{metric.TotalElapsedTime.TotalNanoseconds},{metric.QueuedMessagesCount},\"{metric.FileTransferProgress}\"");
    }

    public void AddServerMetrics(ServerMetrics metric)
    {
        _serverWriter.WriteLine($"{metric.ServerIp},{metric.TotalElapsedTime.TotalNanoseconds},{metric.ProcessingLoad},{metric.QueuedMessagesCount},{metric.TotalLoad}");
    }

    public void AddMessageMetrics(MessageMetrics metric)
    {
        _messageWriter.WriteLine($"{metric.MessageId},{metric.OriginalSenderIp},{metric.State},{metric.ProcessorType},{metric.SizeInBytes},{metric.IsCompressed},{metric.IsFinalMessage},{metric.TotalElapsed.TotalNanoseconds}");
    }

    public bool SaveMetricsToFile()
    {
        try
        {
            _clientWriter.Dispose();
            _serverWriter.Dispose();
            _messageWriter.Dispose();
            _connectionWriter.Dispose();

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении метрик: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}
