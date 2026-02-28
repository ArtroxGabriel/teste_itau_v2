using System;

namespace ItauCompraProgramada.Domain.Entities;

public class StoredEvent
{
    public long Id { get; private set; }
    public string EventName { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public string? ResponsePayload { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public string? Status { get; private set; }

    protected StoredEvent() { }

    public StoredEvent(string eventName, string payload, string correlationId, string? status = "Succeeded", string? responsePayload = null)
    {
        EventName = eventName;
        Payload = payload;
        CorrelationId = correlationId;
        Timestamp = DateTime.UtcNow;
        Status = status;
        ResponsePayload = responsePayload;
    }
}
