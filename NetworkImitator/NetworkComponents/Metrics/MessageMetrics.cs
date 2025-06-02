namespace NetworkImitator.NetworkComponents.Metrics;

public enum MessageProcessingState
{
    Created,
    InTransit,
    Received,
    Compressing,
    Decompressing,
    Processing,
    Processed,
}

public enum MessageProcessor
{
    Client,
    Server,
    LoadBalancer,
    Connection
}

public class MessageMetrics
{
    public int MessageId { get; }
    //public string FromIP { get; }
    //public string ToIP { get; }
    public string OriginalSenderIp { get; }
    public int SizeInBytes { get; }
    public bool IsCompressed { get; }
    public bool IsFinalMessage { get; }

    public MessageProcessingState State { get; set; }
    public MessageProcessor ProcessorType { get; set; }

    public TimeSpan TotalElapsed { get; }

    public MessageMetrics(Message message, MessageProcessingState state, MessageProcessor processorType, TimeSpan totalElapsed)
    {
        MessageId = message.MessageId;
      //  FromIP = message.FromIP;
       // ToIP = message.ToIP;
        OriginalSenderIp = message.OriginalSenderIp;
        SizeInBytes = message.SizeInBytes;
        IsCompressed = message.IsCompressed;
        IsFinalMessage = message.IsFinalMessage;
        
        State = state;
        ProcessorType = processorType;
        TotalElapsed = totalElapsed;
    }
}