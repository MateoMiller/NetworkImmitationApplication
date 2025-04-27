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
    
    /// <summary>
    /// Создает новое сообщение с измененными IP-адресами
    /// </summary>
    /// <param name="newFromIP">Новый IP отправителя</param>
    /// <param name="newToIP">Новый IP получателя</param>
    /// <returns>Новый экземпляр сообщения с измененными адресами</returns>
    public Message CreateWithModifiedIPs(string newFromIP, string newToIP)
    {
        return new Message(newFromIP, newToIP, Content);
    }
}