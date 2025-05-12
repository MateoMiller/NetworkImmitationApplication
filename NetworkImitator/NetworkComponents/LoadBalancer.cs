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
                connection.TransferData(new Message(IP, message.FromIP, "No servers available to handle the request."u8.ToArray(), message.OriginalSenderIp));
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
                if (_servers.Any(s => s.IP == mappedServerIp))
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
                var server = SelectServer();
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
                    var modifiedMessage = message.CreateWithModifiedIPs(IP, serverIp);
                    connection.TransferData(modifiedMessage);
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
                var responseMessage = new Message(IP, message.OriginalSenderIp, message.Content, message.OriginalSenderIp);
                clientConnection.TransferData(responseMessage);
            }
            else
            {
                Console.WriteLine($"LoadBalancer: Failed to find active connection to client {message.OriginalSenderIp}");
                
                // Если соединение с клиентом неактивно, удаляем маппинг
                if (_clientServerMapping.ContainsKey(message.OriginalSenderIp))
                {
                    _clientServerMapping.Remove(message.OriginalSenderIp);
                    Console.WriteLine($"LoadBalancer: соединение с клиентом {message.OriginalSenderIp} неактивно, маппинг удален");
                }
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
            return _servers.OrderBy(s => s.GetTotalLoad).FirstOrDefault();
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
        
        public override void OnConnectionDisconnected(Connection connection)
        {
            // Проверяем, был ли это сервер
            if (connection.FirstComponent is Server server)
            {
                HandleServerDisconnection(server);
            }
            else if (connection.SecondComponent is Server server2)
            {
                HandleServerDisconnection(server2);
            }
            
            // Проверяем, был ли это клиент
            var clientIp = connection.FirstComponent.IP != IP ? connection.FirstComponent.IP : 
                          connection.SecondComponent.IP != IP ? connection.SecondComponent.IP : null;
            
            if (clientIp != null && _clientServerMapping.ContainsKey(clientIp))
            {
                _clientServerMapping.Remove(clientIp);
                Console.WriteLine($"LoadBalancer: соединение с клиентом {clientIp} разорвано, маппинг удален");
            }
        }
        
        private void HandleServerDisconnection(Server server)
        {
            // Удаляем сервер из списка доступных
            if (_servers.Contains(server))
            {
                _servers.Remove(server);
                Console.WriteLine($"LoadBalancer: сервер {server.IP} удален из списка доступных. Осталось серверов: {_servers.Count}");
                
                // Удаляем маппинги к этому серверу
                var clientsToRemove = _clientServerMapping
                    .Where(kvp => kvp.Value == server.IP)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var clientIp in clientsToRemove)
                {
                    _clientServerMapping.Remove(clientIp);
                    Console.WriteLine($"LoadBalancer: удален маппинг клиента {clientIp} к отключенному серверу {server.IP}");
                }
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
