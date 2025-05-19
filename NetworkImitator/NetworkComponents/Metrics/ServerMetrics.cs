using System;
using System.Collections.Generic;

namespace NetworkImitator.NetworkComponents.Metrics;

public class ServerMetrics
{
    public string ServerIp { get; }
    public TimeSpan TotalElapsedTime { get; set; }
    public int ProcessingLoad { get; set; }
    public int QueuedMessagesCount { get; set; }
    public int TotalLoad { get; set; }
    
    public ServerMetrics(string serverIp, TimeSpan totalElapsedTime, int processingLoad, int queuedMessagesCount, int totalLoad)
    {
        ServerIp = serverIp;
        TotalElapsedTime = totalElapsedTime;
        ProcessingLoad = processingLoad;
        QueuedMessagesCount = queuedMessagesCount;
        TotalLoad = totalLoad;
    }
}
