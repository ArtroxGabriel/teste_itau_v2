using System;

using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Entities;

public class IREvent
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public IREventType Type { get; private set; }
    public decimal BaseValue { get; private set; }
    public decimal IRValue { get; private set; }
    public bool PublishedToKafka { get; private set; }
    public DateTime EventDate { get; private set; }

    // Navigation
    public virtual Client? Client { get; private set; }

    protected IREvent() { } // EF Constructor

    public IREvent(long clienteId, IREventType type, decimal baseValue, decimal irValue)
    {
        ClienteId = clienteId;
        Type = type;
        BaseValue = baseValue;
        IRValue = irValue;
        PublishedToKafka = false;
        EventDate = DateTime.UtcNow;
    }

    public void MarkAsPublished() => PublishedToKafka = true;
}