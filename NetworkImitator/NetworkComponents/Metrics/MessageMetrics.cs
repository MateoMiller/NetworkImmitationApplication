using System;

namespace NetworkImitator.NetworkComponents.Metrics;

public enum MessageProcessingState
{
    Created,
    InTransit,
    Received,
    Processing,
    Decompressing,
    Processed,
    Acknowledged,
    Failed
}

public class MessageMetrics
{
    public int MessageId { get; }
    public string FromIP { get; }
    public string ToIP { get; }
    public string OriginalSenderIp { get; }
    public MessageProcessingState State { get; set; }
    public string ProcessorType { get; set; } // "Client" или "Server"
    public string ProcessorIp { get; set; }
    public int SizeInBytes { get; }
    public bool IsCompressed { get; }
    public bool IsFinalMessage { get; }
    public DateTime CreatedAt { get; }
    public DateTime? LastUpdatedAt { get; private set; }
    public TimeSpan ProcessingTime => LastUpdatedAt.HasValue ? LastUpdatedAt.Value - CreatedAt : TimeSpan.Zero;
    
    public MessageMetrics(Message message, MessageProcessingState state, string processorType, string processorIp)
    {
        MessageId = message.MessageId;
        FromIP = message.FromIP;
        ToIP = message.ToIP;
        OriginalSenderIp = message.OriginalSenderIp;
        SizeInBytes = message.SizeInBytes;
        IsCompressed = message.IsCompressed;
        IsFinalMessage = message.IsFinalMessage;
        
        State = state;
        ProcessorType = processorType;
        ProcessorIp = processorIp;
        CreatedAt = DateTime.Now;
        UpdateLastActivityTime();
    }

    private void UpdateLastActivityTime()
    {
        LastUpdatedAt = DateTime.Now;
    }
}
