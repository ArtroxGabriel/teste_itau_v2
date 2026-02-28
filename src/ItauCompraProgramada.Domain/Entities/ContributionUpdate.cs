using System;

namespace ItauCompraProgramada.Domain.Entities;

public class ContributionUpdate
{
    public long Id { get; private set; }
    public long ClientId { get; private set; }
    public decimal OldValue { get; private set; }
    public decimal NewValue { get; private set; }
    public DateTime UpdateDate { get; private set; }

    protected ContributionUpdate() { }

    public ContributionUpdate(long clientId, decimal oldValue, decimal newValue)
    {
        ClientId = clientId;
        OldValue = oldValue;
        NewValue = newValue;
        UpdateDate = DateTime.UtcNow;
    }
}