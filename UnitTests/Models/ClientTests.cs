/*using System.Reflection;
using NetworkImitator.NetworkComponents;
using NetworkImitator.UI;
using FluentAssertions;

namespace UnitTests.Models;

[TestFixture]
public class ClientTests
{
    private MainViewModel _mainViewModel;
    private Client _client;
    private Server _server;
    private Connection _connection;
    private List<Message> _capturedMessages;

    [SetUp]
    public void Setup()
    {
        _mainViewModel = new MainViewModel();
        _client = new Client(0, 0, 100, _mainViewModel);
        _server = new Server(100, 100, 50, 10, _mainViewModel);
        _capturedMessages = new List<Message>();
        
        _connection = new Connection
        {
            FirstComponent = _client,
            SecondComponent = _server
        };
        
        _client.ConnectTo(_connection);
        _server.ConnectTo(_connection);
    }

    [Test]
    public void QueueContent_ShouldChunkDataInto65535ByteMessages()
    {
        var dataSize = Client.MaxPacketSize * 3 + 1024;
        var testData = new byte[dataSize];
        Random.Shared.NextBytes(testData);
        
        _client.QueueContent(_server, testData);
        
        var messages = new List<Message>();
        while (_client._messagesQueue.Count > 0)
        {
            messages.Add(_client._messagesQueue.Dequeue());
        }
        
        messages.Should().HaveCount(4);
        
        messages[0].SizeInBytes.Should().Be(Client.MaxPacketSize);
        messages[1].SizeInBytes.Should().Be(Client.MaxPacketSize);
        messages[2].SizeInBytes.Should().Be(Client.MaxPacketSize);
        
        messages[3].SizeInBytes.Should().Be(1024);
        
        messages[0].IsFinalMessage.Should().BeFalse();
        messages[1].IsFinalMessage.Should().BeFalse();
        messages[2].IsFinalMessage.Should().BeFalse();
        messages[3].IsFinalMessage.Should().BeTrue();
        
        messages.Sum(m => m.SizeInBytes).Should().Be(dataSize);
    }
    
    [Test]
    public void CompressionEnabled_ShouldCreateCompressedMessages()
    {
        _client.IsCompressingEnabled = true;
        
        var dataSize = 1024;
        var testData = new byte[dataSize];
        Random.Shared.NextBytes(testData);
        
        _client.QueueContent(_server, testData);
        
        var message = _client._messagesQueue.Dequeue();
        
        message.IsCompressed.Should().BeTrue();
    }
    
    [Test]
    public void ClientState_ShouldChangeToCompressingData_WhenCompressionEnabled()
    {
        _client.IsCompressingEnabled = true;
        
        var dataSize = 1024;
        var testData = new byte[dataSize];
        Random.Shared.NextBytes(testData);
        
        _client.QueueContent(_server, testData);
        
        _client._context.State.Should().Be(ClientState.CompressingData);
    }
    
    [Test]
    public void ReceiveData_ShouldChangeState_FromWaitingForResponseToProcessingData()
    {
        _client._context.ChangeState(ClientState.WaitingForResponse);
        
        var message = new Message(_server.IP, _client.IP, new byte[10], _server.IP);
        
        _client.ReceiveData(_connection, message);
        
        _client._context.State.Should().Be(ClientState.ProcessingData);
    }
    
    [Test]
    public void FileTransferProgress_ShouldBeUpdated_WhenSendingFile()
    {
        var fileData = new byte[1024];
        Random.Shared.NextBytes(fileData);
        
        _client._fileData = fileData;
        _client.FileSizeBytes = fileData.Length;
        _client._currentFilePosition = 0;
        
        _client._currentFilePosition = fileData.Length / 2;
        _client.UpdateFileTransferProgress();
        
        _client.FileTransferProgress.Should().Be(50.0);
        _client.FileTransferStatus.Should().Contain("50");
    }
    
    [Test]
    public void PingMode_ShouldContinueSending_AfterResponseReceived()
    {
        _client.ClientMode = ClientMode.Ping;
        
        _client._context.ChangeState(ClientState.WaitingForResponse);
        
        var message = new Message(_server.IP, _client.IP, new byte[10], _server.IP);
        
        _client.ReceiveData(_connection, message);
        
        _client._context.State.Should().Be(ClientState.ProcessingData);
    }
    
    [Test]
    public void ProcessTick_ShouldTransitionFromProcessingToProcessedData_AfterTimeElapsed()
    {
        // Arrange
        _client._context.ChangeState(ClientState.ProcessingData);
        
        // Act - Process tick with elapsed time equal to processing time
        var processingTime = TimeSpan.FromMilliseconds(_client.SendingPacketPeriod);
        _client.ProcessTick(processingTime);
        
        // Assert
        _client._context.State.Should().Be(ClientState.ProcessedData);
    }
    
    [Test]
    public void ProcessTick_ShouldSendMessage_WhenInProcessedDataState()
    {
        // Arrange
        _client._context.ChangeState(ClientState.ProcessedData);
        _client.ClientMode = ClientMode.Ping; // Use ping mode to avoid waiting for responses
        
        // Add some test data to the queue
        var testData = new byte[1024];
        Random.Shared.NextBytes(testData);
        _client.QueueContent(_server, testData);

        // Count messages in queue before processing
        var messageCountBefore = _client._messagesQueue.Count;
        
        // Act
        _client.ProcessTick(TimeSpan.FromMilliseconds(100));
        
        // Assert
        // The message queue should now be empty or have fewer messages,
        // indicating that messages were sent during ProcessTick
        _client._messagesQueue.Count.Should().BeLessThan(messageCountBefore);
    }
    
    [Test]
    public void ProcessTick_ShouldUpdateMessageQueue_WhenQueueIsEmpty()
    {
        // Arrange
        _client._context.ChangeState(ClientState.ProcessedData);
        _client.ClientMode = ClientMode.Ping; // Use ping mode to avoid waiting for responses
        
        // Make sure queue is empty
        while (_client._messagesQueue.Count > 0)
        {
            _client._messagesQueue.Dequeue();
        }
        
        // Act
        _client.ProcessTick(TimeSpan.FromMilliseconds(100));
        
        // Assert
        // In ping mode, ProcessTick should have populated the queue
        // Verify that we generated at least some data
        _client._messagesQueue.Should().NotBeEmpty();
    }
    
    [Test]
    public void ProcessTick_ShouldChangeToWaitingForResponse_WhenSendingMessageInFileTransferMode()
    {
        // Arrange
        _client._context.ChangeState(ClientState.ProcessedData);
        _client.ClientMode = ClientMode.FileTransfer; // Use file transfer mode which waits for responses
        
        // Add some test data to the queue
        var testData = new byte[1024];
        Random.Shared.NextBytes(testData);
        _client.QueueContent(_server, testData);
        
        // Act
        _client.ProcessTick(TimeSpan.FromMilliseconds(100));
        
        // Assert
        // After sending a message in file transfer mode, client should wait for response
        _client._context.State.Should().Be(ClientState.WaitingForResponse);
    }
    
    [Test]
    public void ProcessTick_ShouldChangeToCompressingData_WhenCompressionEnabled()
    {
        // Arrange
        _client._context.ChangeState(ClientState.ProcessedData);
        _client.ClientMode = ClientMode.Ping; // To generate messages automatically
        _client.IsCompressingEnabled = true;
        
        // Make sure queue is empty
        while (_client._messagesQueue.Count > 0)
        {
            _client._messagesQueue.Dequeue();
        }
        
        // Act
        _client.ProcessTick(TimeSpan.FromMilliseconds(100));
        
        // Assert - Compression should be triggered when generating new messages
        _client._context.State.Should().Be(ClientState.CompressingData);
    }
} */