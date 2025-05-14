namespace NetworkImitator.NetworkComponents;

public enum ClientState
{
    ProcessingData,
    ProcessedData,
    CompressingData,
    SendingData,
    WaitingForResponse,
}