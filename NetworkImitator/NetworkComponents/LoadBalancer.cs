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
        
        private readonly Dictionary<string, string> _clientServerMapping = new();
        
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
                connection.TransferData(new Message(Random.Shared.Next(), IP, message.FromIP, "No servers available to handle the request."u8.ToArray(), message.OriginalSenderIp));
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
            string? serverIp = null;
            if (_clientServerMapping.TryGetValue(message.FromIP, out var mappedServerIp))
            {
                if (_servers.Any(s => s.IP == mappedServerIp) && !message.IsFinalMessage)
                {
                    serverIp = mappedServerIp;
                }
                else
                {
                    _clientServerMapping.Remove(message.FromIP);
                }
            }
            
            if (serverIp == null)
            {
                //TODO Я заочно написал, что здесь есть метрика
                var server = SelectServer(message);
                if (server != null)
                {
                    serverIp = server.IP;
                    // Сохраняем новое сопоставление
                    _clientServerMapping[message.FromIP] = serverIp;
                }
            }
            
            if (serverIp != null)
            {
                var connection = Connections.FirstOrDefault(x => x.IsActive && x.GetComponent(serverIp) != null);
                
                if (connection != null)
                {
                    var modifiedMessage = message with
                    {
                        FromIP = IP,
                        ToIP = serverIp
                    };
                    connection.TransferData(modifiedMessage);
                    //Если IsFinal, то удаляем маппинг
                }
                else
                {
                    _clientServerMapping.Remove(message.FromIP);
                    Console.WriteLine($"LoadBalancer: соединение с сервером {serverIp} неактивно, маппинг удален");
                }
            }
        }

        private void HandleServerToLoadBalancerMessage(Message message)
        {
            var clientConnection = Connections.FirstOrDefault(x => x.IsActive && x.GetComponent(message.OriginalSenderIp) != null);
            if (clientConnection != null)
            {
                var responseMessage = message with
                {
                    FromIP = IP,
                    ToIP = message.OriginalSenderIp
                };
                clientConnection.TransferData(responseMessage);
            }
            else
            {
                Console.WriteLine($"LoadBalancer: Failed to find active connection to client {message.OriginalSenderIp}");
            }
        }

        public override void ProcessTick(TimeSpan elapsed)
        { }

        private Server? SelectServer(Message message)
        {
            return Algorithm switch
            {
                LoadBalancerAlgorithm.RoundRobin => SelectRoundRobin(),
                LoadBalancerAlgorithm.LeastConnections => SelectLeastConnections(),
                LoadBalancerAlgorithm.IpHashing => SelectIpHashing(message),
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
            return _servers.OrderBy(s => s.GetTotalLoad).FirstOrDefault();
        }
        
        private Server? SelectIpHashing(Message message)
        {
            if (_servers.Count == 0)
                return null;
            var hash = message.FromIP.GetHashCode(); 
            return _servers[Math.Abs(hash) % _servers.Count];
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
