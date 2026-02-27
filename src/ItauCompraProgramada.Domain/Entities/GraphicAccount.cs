using System;
using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Entities;

public class GraphicAccount
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public string AccountNumber { get; private set; }
    public AccountType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual Client? Client { get; private set; }
    public virtual ICollection<Custody> Custodies { get; private set; } = new List<Custody>();

    protected GraphicAccount() { } // EF Constructor

    public GraphicAccount(long clienteId, string accountNumber, AccountType type)
    {
        ClienteId = clienteId;
        AccountNumber = accountNumber;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }
}
