namespace NetworkImitator.NetworkComponents;

public class Message
{
    public string FromIP { get; }
    public string ToIP { get; }
    public string Content { get; }

    public Message(string fromIP, string toIP, string content)
    {
        FromIP = fromIP;
        ToIP = toIP;
        Content = content;
    }
}