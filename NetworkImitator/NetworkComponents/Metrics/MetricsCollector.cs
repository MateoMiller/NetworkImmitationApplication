using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;

namespace NetworkImitator.NetworkComponents.Metrics
{
    public class MetricsCollector
    {
        private static readonly Lazy<MetricsCollector> _instance = new(() => new MetricsCollector());
        public static MetricsCollector Instance => _instance.Value;

        private readonly string _connectionString;
        private readonly ConcurrentQueue<ClientMetrics> _clientMetricsBuffer = new();
        private readonly ConcurrentQueue<ServerMetrics> _serverMetricsBuffer = new();
        private readonly ConcurrentQueue<MessageMetrics> _messageMetricsBuffer = new();
        private readonly ConcurrentQueue<ConnectionMetrics> _connectionMetricsBuffer = new();

        private const int BATCH_SIZE = 5000;
        private readonly Timer _flushTimer;
        private readonly object _flushLock = new();

        private MetricsCollector()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var folderPath = $"C:\\Users\\yurii\\Desktop\\метрики\\{timestamp}";
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var dbPath = Path.Combine(folderPath, "network_metrics.db");
            _connectionString = $"Data Source={dbPath}";

            InitializeDatabase();
            
            _flushTimer = new Timer(AutoFlush, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.25));
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTablesCommand = connection.CreateCommand();
            createTablesCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS ClientMetrics (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClientIp TEXT NOT NULL,
                    State INTEGER NOT NULL,
                    TimeInCurrentStateNs INTEGER NOT NULL,
                    TotalElapsedTimeNs INTEGER NOT NULL,
                    QueuedMessagesCount INTEGER NOT NULL,
                    FileTransferProgress REAL NOT NULL,
                    FileTransferStatus TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ServerMetrics (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ServerIp TEXT NOT NULL,
                    TotalElapsedTimeNs INTEGER NOT NULL,
                    ProcessingLoad INTEGER NOT NULL,
                    QueuedMessagesCount INTEGER NOT NULL,
                    TotalLoad INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS MessageMetrics (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MessageId INTEGER NOT NULL,
                    OriginalSenderIp TEXT NOT NULL,
                    State INTEGER NOT NULL,
                    ProcessorType INTEGER NOT NULL,
                    SizeInBytes INTEGER NOT NULL,
                    IsCompressed INTEGER NOT NULL,
                    IsFinalMessage INTEGER NOT NULL,
                    TotalElapsedNs INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ConnectionMetrics (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConnectionName TEXT NOT NULL,
                    ElapsedTimeNs INTEGER NOT NULL,
                    MessagesCount INTEGER NOT NULL,
                    TotalMessagesSize INTEGER NOT NULL
                );
            ";

            createTablesCommand.ExecuteNonQuery();
        }

        public void AddClientMetrics(ClientMetrics metric)
        {
            _clientMetricsBuffer.Enqueue(metric);
            CheckAndFlushIfNeeded();
        }

        public void AddServerMetrics(ServerMetrics metric)
        {
            _serverMetricsBuffer.Enqueue(metric);
            CheckAndFlushIfNeeded();
        }

        public void AddMessageMetrics(MessageMetrics metric)
        {
            _messageMetricsBuffer.Enqueue(metric);
            CheckAndFlushIfNeeded();
        }

        public void AddConnectionMetrics(ConnectionMetrics metric)
        {
            _connectionMetricsBuffer.Enqueue(metric);
            CheckAndFlushIfNeeded();
        }

        private void CheckAndFlushIfNeeded()
        {
            if (_clientMetricsBuffer.Count >= BATCH_SIZE ||
                _serverMetricsBuffer.Count >= BATCH_SIZE ||
                _messageMetricsBuffer.Count >= BATCH_SIZE ||
                _connectionMetricsBuffer.Count >= BATCH_SIZE)
            {
                Task.Run(FlushAllMetrics);
            }
        }

        private void AutoFlush(object state)
        {
            Task.Run(FlushAllMetrics);
        }

        private void FlushAllMetrics()
        {
            lock (_flushLock)
            {
                try
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();
                    using var transaction = connection.BeginTransaction();

                    FlushClientMetrics(connection, transaction);
                    FlushServerMetrics(connection, transaction);
                    FlushMessageMetrics(connection, transaction);
                    FlushConnectionMetrics(connection, transaction);

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error flushing metrics to database: {ex.Message}");
                }
            }
        }

        private void FlushClientMetrics(SqliteConnection connection, SqliteTransaction transaction)
        {
            if (_clientMetricsBuffer.IsEmpty) return;

            var metrics = new List<ClientMetrics>();
            while (_clientMetricsBuffer.TryDequeue(out var metric) && metrics.Count < BATCH_SIZE)
            {
                metrics.Add(metric);
            }

            if (metrics.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO ClientMetrics (ClientIp, State, TimeInCurrentStateNs, TotalElapsedTimeNs, QueuedMessagesCount, FileTransferProgress, FileTransferStatus) VALUES");
            for (var i = 0; i < metrics.Count; i++)
            {
                var m = metrics[i];
                sb.Append($"('{m.ClientIp}', {(int)m.State}, {m.TimeInCurrentState.TotalNanoseconds}, {m.TotalElapsedTime.TotalNanoseconds}, {m.QueuedMessagesCount}, '{m.FileTransferProgress}', '{m.FileTransferStatus}')");
                if (i < metrics.Count - 1)
                    sb.Append(",");
            }
            sb.Append(";");
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }

        private void FlushServerMetrics(SqliteConnection connection, SqliteTransaction transaction)
        {
            if (_serverMetricsBuffer.IsEmpty) return;

            var metrics = new List<ServerMetrics>();
            while (_serverMetricsBuffer.TryDequeue(out var metric) && metrics.Count < BATCH_SIZE)
            {
                metrics.Add(metric);
            }

            if (metrics.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO ServerMetrics (ServerIp, TotalElapsedTimeNs, ProcessingLoad, QueuedMessagesCount, TotalLoad) VALUES");
            for (int i = 0; i < metrics.Count; i++)
            {
                var m = metrics[i];
                sb.Append($"('{m.ServerIp}', {m.TotalElapsedTime.TotalNanoseconds}, {m.ProcessingLoad}, {m.QueuedMessagesCount}, {m.TotalLoad})");
                if (i < metrics.Count - 1)
                    sb.Append(",");
            }
            sb.Append(";");
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }

        private void FlushMessageMetrics(SqliteConnection connection, SqliteTransaction transaction)
        {
            if (_messageMetricsBuffer.IsEmpty) return;

            var metrics = new List<MessageMetrics>();
            while (_messageMetricsBuffer.TryDequeue(out var metric) && metrics.Count < BATCH_SIZE)
            {
                metrics.Add(metric);
            }

            if (metrics.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO MessageMetrics (MessageId, OriginalSenderIp, State, ProcessorType, SizeInBytes, IsCompressed, IsFinalMessage, TotalElapsedNs) VALUES");
            for (int i = 0; i < metrics.Count; i++)
            {
                var m = metrics[i];
                sb.Append($"({m.MessageId}, '{m.OriginalSenderIp}', {(int)m.State}, {(int)m.ProcessorType}, {m.SizeInBytes}, {(m.IsCompressed ? 1 : 0)}, {(m.IsFinalMessage ? 1 : 0)}, {m.TotalElapsed.TotalNanoseconds})");
                if (i < metrics.Count - 1)
                    sb.Append(",");
            }
            sb.Append(";");
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }

        private void FlushConnectionMetrics(SqliteConnection connection, SqliteTransaction transaction)
        {
            if (_connectionMetricsBuffer.IsEmpty) return;

            var metrics = new List<ConnectionMetrics>();
            while (_connectionMetricsBuffer.TryDequeue(out var metric) && metrics.Count < BATCH_SIZE)
            {
                metrics.Add(metric);
            }

            if (metrics.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("INSERT INTO ConnectionMetrics (ConnectionName, ElapsedTimeNs, MessagesCount, TotalMessagesSize) VALUES");
            for (int i = 0; i < metrics.Count; i++)
            {
                var m = metrics[i];
                sb.Append($"('{m.ConnectionName}', {m.ElapsedTime.TotalNanoseconds}, {m.MessagesCount}, {m.TotalMessagesSize})");
                if (i < metrics.Count - 1)
                    sb.Append(",");
            }
            sb.Append(";");
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sb.ToString();
            command.ExecuteNonQuery();
        }

        public bool SaveMetricsToFile()
        {
            try
            {
                FlushAllMetrics();
                _flushTimer.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении метрик: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        ~MetricsCollector()
        {
            _flushTimer?.Dispose();
            try
            {
                FlushAllMetrics();
            }
            catch
            {
                // ignored
            }
        }
    }
}