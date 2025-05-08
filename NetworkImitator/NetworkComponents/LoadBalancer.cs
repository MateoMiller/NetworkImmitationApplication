using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents
{
    public partial class LoadBalancer : Component
    {
        [ObservableProperty] private LoadBalancerAlgorithm _algorithm;

        private int _currentServerIndex;
        private readonly List<Server> _servers = [];
        
        public LoadBalancer(double x, double y, LoadBalancerAlgorithm algorithm, MainViewModel viewModel) : base(viewModel, x, y)
        {
            _algorithm = algorithm;
        }

        public override BitmapImage Image => new(Images.LoadBalancerUri);

        public override void ReceiveData(Connection connection, Message message)
        {
            if (_servers.Count == 0)
            {
                Console.WriteLine("LoadBalancer: No servers available to handle the request.");
                connection.TransferData(new Message(IP, message.FromIP, "No servers available to handle the request.", message.OriginalSenderIp));
                return;
            }

            if (message.OriginalSenderIp != message.FromIP)
            {
                HandleServerToLoadBalancerMessage(message);
            } 
            else if (message.ToIP == IP)
            {
                HandleClientToLoadBalancerMessage(message);
            } 
            else
            {
                Console.WriteLine($"LoadBalancer: Unexpected message from {message.FromIP} to {message.ToIP}");
            }
        }

        private void HandleClientToLoadBalancerMessage(Message message)
        {
            var server = SelectServer();
            var connection = Connections.FirstOrDefault(x => x.GetComponent(server?.IP) != null);
            
            if (connection != null)
            {
                var modifiedMessage = message.CreateWithModifiedIPs(IP, server!.IP);
                connection.TransferData(modifiedMessage);
            }
        }

        private void HandleServerToLoadBalancerMessage(Message message)
        {
            var clientConnection = Connections.FirstOrDefault(x => x.GetComponent(message.OriginalSenderIp) != null);
            if (clientConnection != null)
            {
                var responseMessage = new Message(IP, message.OriginalSenderIp, message.Content, message.OriginalSenderIp);
                clientConnection.TransferData(responseMessage);
            }
            else
            {
                Console.WriteLine($"LoadBalancer: Failed to find connection to client {message.OriginalSenderIp}");
            }
        }

        public override void ProcessTick(TimeSpan elapsed)
        { }

        private Server? SelectServer()
        {
            return Algorithm switch
            {
                LoadBalancerAlgorithm.RoundRobin => SelectRoundRobin(),
                LoadBalancerAlgorithm.LeastConnections => SelectLeastConnections(),
                _ => throw new InvalidOperationException($"Unknown algorithm: {Algorithm}")
            };
        }

        private Server? SelectRoundRobin()
        {
            if (_servers.Count == 0) 
                return null;
            var server = _servers[_currentServerIndex];
            _currentServerIndex = (_currentServerIndex + 1) % _servers.Count;
            return server;
        }

        private Server? SelectLeastConnections()
        {
            return _servers.OrderBy(s => s.GetProcessingLoad()).FirstOrDefault();
        }

        protected override void OnNewConnection(Connection connection)
        {
            base.OnNewConnection(connection);
            if (connection.FirstComponent is Server server)
            {
                RegisterServer(server);
            }
            else if (connection.SecondComponent is Server server2)
            {
                RegisterServer(server2);
            }
        }
        
        private void RegisterServer(Server server)
        {
            if (!_servers.Contains(server))
            {
                _servers.Add(server);
                Console.WriteLine($"LoadBalancer: Registered server with IP {server.IP}. Total servers: {_servers.Count}");
            }
        }
    }
}
