namespace NetworkImitator.NetworkComponents.Metrics;

public class ConnectionMetrics
{
    public ConnectionMetrics(string connectionName, TimeSpan elapsedTime, int messagesCount, int totalMessagesSize)
    {
        ConnectionName = connectionName;
        ElapsedTime = elapsedTime;
        MessagesCount = messagesCount;
        TotalMessagesSize = totalMessagesSize;
    }

    public string ConnectionName { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public int MessagesCount { get; set; }
    public int TotalMessagesSize { get; set; }
}