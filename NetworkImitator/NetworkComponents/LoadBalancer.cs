using System.Windows.Media;
using System.Windows.Media.Imaging;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents
{
    public class LoadBalancer : Component
    {
        private readonly List<Server> servers = new();
        private int currentServerIndex;
        private readonly LoadBalancerAlgorithm algorithm;

        public LoadBalancer(double x, double y, LoadBalancerAlgorithm algorithm, MainViewModel viewModel) : base(viewModel, x, y)
        {
            this.algorithm = algorithm;
        }

        public override BitmapImage Image => new(Images.LoadBalancerUri);

        public override Brush GetBrush()
        {
            return servers.Count > 0 ? Brushes.LightBlue : Brushes.Gray;
        }

        public void AddServer(Server server)
        {
            servers.Add(server);
        }

        public override void ReceiveData(Message message)
        {
            if (servers.Count == 0)
            {
                Console.WriteLine("LoadBalancer: No servers available to handle the request.");
                return;
            }

            //TODO
            //1 -> 2 -> 3 -> 4
            //4 -> 3 -> 2 -> 1
            
            var server = SelectServer();
            if (server != null)
            {
                Console.WriteLine($"LoadBalancer: Forwarding data to server at ({server.X}, {server.Y})");
                server.ReceiveData(message);
            }
        }

        public override void ProcessTick(TimeSpan elapsed)
        { }

        private Server? SelectServer()
        {
            return algorithm switch
            {
                LoadBalancerAlgorithm.RoundRobin => SelectRoundRobin(),
                LoadBalancerAlgorithm.LeastConnections => SelectLeastConnections(),
                _ => throw new InvalidOperationException($"Unknown algorithm: {algorithm}")
            };
        }

        private Server? SelectRoundRobin()
        {
            if (servers.Count == 0) 
                return null;
            var server = servers[currentServerIndex];
            currentServerIndex = (currentServerIndex + 1) % servers.Count;
            return server;
        }

        private Server? SelectLeastConnections()
        {
            return servers.OrderBy(s => s.GetProcessingLoad()).FirstOrDefault();
        }
    }
}
