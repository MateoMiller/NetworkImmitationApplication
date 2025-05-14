using System;
using System.Collections.Generic;

namespace NetworkImitator.NetworkComponents.Metrics;

public class ServerMetrics
{
    public string ServerIp { get; }
    public int ProcessingLoad { get; set; }
    public int QueuedMessagesCount { get; set; }
    public int TotalLoad { get; set; }
    public Dictionary<string, Server.ProcessingState> ClientContextStates { get; } = new();
    
    public ServerMetrics(string serverIp, int processingLoad, int queuedMessagesCount, int totalLoad)
    {
        ServerIp = serverIp;
        ProcessingLoad = processingLoad;
        QueuedMessagesCount = queuedMessagesCount;
        TotalLoad = totalLoad;
    }
}
