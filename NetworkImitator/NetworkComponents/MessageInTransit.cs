namespace NetworkImitator.NetworkComponents;

public class MessageInTransit
{
    public Message Message { get; }
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
    
    public MessageInTransit(Message message)
    {
        Message = message;
    }
}