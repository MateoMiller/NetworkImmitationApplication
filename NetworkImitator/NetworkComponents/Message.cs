using NetworkImitator.NetworkComponents.Metrics;

namespace NetworkImitator.NetworkComponents;

public record Message
{
    public int MessageId { get; set; } = Guid.NewGuid().GetHashCode();
    public string FromIP { get; set; }
    public string OriginalSenderIp { get; }
    public string ToIP { get; set; }
    public byte[] Content { get; }
    public int SizeInBytes { get; }
    public bool IsCompressed { get; }

    public bool IsFinalMessage { get; set; }

    public Message(string fromIP, string toIP, byte[] content, string originalSenderIp, bool isFinalMessage = true, bool isCompressed = false)
    {
        FromIP = fromIP;
        ToIP = toIP;
        Content = content;
        OriginalSenderIp = originalSenderIp;
        IsFinalMessage = isFinalMessage;
        SizeInBytes = content.Length;
        IsCompressed = isCompressed;
        
        // Регистрация метрики сообщения при создании
        var messageMetrics = new MessageMetrics(this, MessageProcessingState.Created, 
            "Client", fromIP);
        MetricsCollector.Instance.AddMessageMetrics(messageMetrics);
    }
    
    // Метод для обновления состояния метрик сообщения
    public void UpdateMessageState(MessageProcessingState state, string processorType, string processorIp)
    {
        /*var metrics = MetricsCollector.Instance.GetMessageMetrics(MessageId);
        if (metrics != null)
        {
            metrics.UpdateState(state);
        }
        else
        {
            metrics = new MessageMetrics(this, state, processorType, processorIp);
            MetricsCollector.Instance.AddOrUpdateMessageMetrics(metrics);
        }*/
    }
}