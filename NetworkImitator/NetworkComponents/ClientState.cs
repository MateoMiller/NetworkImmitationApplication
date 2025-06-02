namespace NetworkImitator.NetworkComponents;

public enum ClientState
{
    Finished,
    ProcessingData,
    ProcessedData,
    CompressingData,
    SendingData,
    WaitingForResponse,
}