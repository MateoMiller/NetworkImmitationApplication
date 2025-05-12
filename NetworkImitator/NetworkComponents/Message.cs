namespace NetworkImitator.NetworkComponents;

public class Message
{
    public string FromIP { get; }
    public string OriginalSenderIp { get; }
    public string ToIP { get; }
    public byte[] Content { get; }
    public int SizeInBytes { get; }

    public Message(string fromIP, string toIP, byte[] content, string originalSenderIp)
    {
        FromIP = fromIP;
        ToIP = toIP;
        Content = content;
        OriginalSenderIp = originalSenderIp;
        SizeInBytes = content.Length;
    }

    public Message CreateWithModifiedIPs(string newFromIP, string newToIP)
    {
        return new Message(newFromIP, newToIP, Content, OriginalSenderIp);
    }
}