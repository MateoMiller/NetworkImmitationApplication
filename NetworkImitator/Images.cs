namespace NetworkImitator;

public static class Images
{
    public static Uri ServerImageUri => new("Images/server.jpg", UriKind.Relative);
    public static Uri PcImageUri => new("Images/client.jpg", UriKind.Relative);
    public static Uri LoadBalancerUri => new("Images/loadBalancer.png", UriKind.Relative);
}