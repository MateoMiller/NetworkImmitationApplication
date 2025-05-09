namespace NetworkImitator.NetworkComponents;

public class Message
{
    public string FromIP { get; }
    public string OriginalSenderIp { get; }
    public string ToIP { get; }
    public string Content { get; }
    public int SizeInBytes { get; }

    public Message(string fromIP, string toIP, string content, string originalSenderIp)
    {
        FromIP = fromIP;
        ToIP = toIP;
        Content = content;
        OriginalSenderIp = originalSenderIp;
        SizeInBytes = CalculateMessageSize(content);
    }

    public Message CreateWithModifiedIPs(string newFromIP, string newToIP)
    {
        return new Message(newFromIP, newToIP, Content, OriginalSenderIp);
    }
    
    private static int CalculateMessageSize(string content)
    {
        return content.Length;
    }
}