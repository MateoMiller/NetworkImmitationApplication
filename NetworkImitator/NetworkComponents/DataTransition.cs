namespace NetworkImitator.NetworkComponents;

public class DataTransition
{
    public Connection Connection { get; set; }
    public Component Reciever { get; set; }
    public byte[] Content { get; set; }
}