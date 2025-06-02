namespace NetworkImitator.NetworkComponents;

public record Message
{
    public int MessageId { get; set; } = Guid.NewGuid().GetHashCode();
    public int Identification { get; }
    public string FromIP { get; set; }
    public string OriginalSenderIp { get; }
    public string ToIP { get; set; }
    public byte[] Content { get; }
    public int SizeInBytes { get; }
    public bool IsCompressed { get; }

    public bool IsFinalMessage { get; set; }

    public Message(int identification, string fromIP, string toIP, byte[] content, string originalSenderIp, bool isFinalMessage = true, bool isCompressed = false)
    {
        Identification = identification;
        FromIP = fromIP;
        ToIP = toIP;
        Content = content;
        OriginalSenderIp = originalSenderIp;
        IsFinalMessage = isFinalMessage;
        SizeInBytes = content.Length;
        IsCompressed = isCompressed;
    }
}