namespace NetworkImitator.NetworkComponents;

public class Message
{
    public string FromIP { get; }
    public string OriginalSenderIp { get; }
    public string ToIP { get; }
    public string Content { get; }

    public Message(string fromIP, string toIP, string content, string originalSenderIp)
    {
        FromIP = fromIP;
        ToIP = toIP;
        Content = content;
        OriginalSenderIp = originalSenderIp;
    }
    
    public Message CreateWithModifiedIPs(string newFromIP, string newToIP)
    {
        return new Message(newFromIP, newToIP, Content, OriginalSenderIp);
    }
}