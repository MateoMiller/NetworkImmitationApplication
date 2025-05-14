using System;

namespace NetworkImitator.NetworkComponents.Metrics;

public class ClientMetrics
{
    public string ClientIp { get; }
    public ClientState State { get; set; }
    public TimeSpan TimeInCurrentState { get; set; }
    public TimeSpan TotalElapsedTime { get; set; }
    public int QueuedMessagesCount { get; set; }
    public double FileTransferProgress { get; set; }
    public string FileTransferStatus { get; set; }
    
    public ClientMetrics(string clientIp, ClientState state, TimeSpan timeInCurrentState, 
        TimeSpan totalElapsedTime, int queuedMessagesCount = 0, 
        double fileTransferProgress = 0, string fileTransferStatus = "")
    {
        ClientIp = clientIp;
        State = state;
        TimeInCurrentState = timeInCurrentState;
        TotalElapsedTime = totalElapsedTime;
        QueuedMessagesCount = queuedMessagesCount;
        FileTransferProgress = fileTransferProgress;
        FileTransferStatus = fileTransferStatus;
    }
}
