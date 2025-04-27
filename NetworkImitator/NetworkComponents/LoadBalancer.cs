using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using NetworkImitator.UI;

namespace NetworkImitator.NetworkComponents
{
    public class LoadBalancer : Component
    {
        private int currentServerIndex;
        private readonly List<Server> _servers = [];
        private readonly LoadBalancerAlgorithm algorithm;
        
        // Словарь для отслеживания соответствий между IP серверов и оригинальными IP клиентов
        private readonly Dictionary<string, string> _serverToClientMap = new Dictionary<string, string>();

        public LoadBalancer(double x, double y, LoadBalancerAlgorithm algorithm, MainViewModel viewModel) : base(viewModel, x, y)
        {
            this.algorithm = algorithm;
        }

        public override BitmapImage Image => new(Images.LoadBalancerUri);

        public override void ReceiveData(Connection connection, Message message)
        {
            if (_servers.Count == 0)
            {
                Console.WriteLine("LoadBalancer: No servers available to handle the request.");
                connection.TransferData(new Message(IP, message.FromIP, "No servers available to handle the request."));
                return;
            }

            if (_serverToClientMap.ContainsKey(message.FromIP))
            {
                HandleServerToLoadBalancerMessage(message);
            } 
            else if (message.ToIP == IP)
            {
                HandleClientToLoadBalancerMessage(message);
            } 
            else
            {
                Console.WriteLine($"Something went wrong. Message from {message.FromIP} to {message.ToIP} is not expected.");
            }
        }

        private void HandleClientToLoadBalancerMessage(Message message)
        {
            var server = GetServerForClient(message.FromIP);
            var connection = Connections.FirstOrDefault(x => x.GetComponent(server.IP) != null);
            
            if (connection != null)
            {
                var modifiedMessage = message.CreateWithModifiedIPs(IP, server.IP);
                
                Console.WriteLine($"LoadBalancer: Forwarding data from client {message.FromIP} to server at {server.IP}");
                Console.WriteLine($"LoadBalancer: Changed message IPs from {message.FromIP}->{message.ToIP} to {modifiedMessage.FromIP}->{modifiedMessage.ToIP}");
                
                connection.TransferData(modifiedMessage);
            }
        }

        // Обработка сообщения от сервера к балансировщику
        private void HandleServerToLoadBalancerMessage(Message message)
        {
            if (_serverToClientMap.TryGetValue(message.FromIP, out var clientIP))
            {
                var modifiedMessage = message.CreateWithModifiedIPs(IP, clientIP);
                SendMessageToClient(modifiedMessage);
            }
        }

        private void SendMessageToClient(Message message)
        {
            var connection = Connections.FirstOrDefault(x => x.GetComponent(message.ToIP) != null);
            if (connection != null)
            {
                connection.TransferData(message);
            }
            else
            {
                Console.WriteLine($"LoadBalancer: Failed to find client with IP {message.ToIP}");
            }
        }

        private Server GetServerForClient(string clientIP)
        {
            var server = SelectServer();
            if (server != null)
            {
                _serverToClientMap[server.IP] = clientIP;
                Console.WriteLine($"LoadBalancer: Assigned server {server.IP} for client {clientIP}");
            }
            
            return server;
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
            if (_servers.Count == 0) 
                return null;
            var server = _servers[currentServerIndex];
            currentServerIndex = (currentServerIndex + 1) % _servers.Count;
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
