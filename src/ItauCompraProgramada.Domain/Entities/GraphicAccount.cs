using System;

using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Entities;

public class GraphicAccount
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public string AccountNumber { get; private set; } = null!;
    public AccountType Type { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual Client? Client { get; private set; }
    public virtual ICollection<Custody> Custodies { get; private set; } = new List<Custody>();

    protected GraphicAccount() { } // EF Constructor

    public GraphicAccount(long clienteId, string accountNumber, AccountType type, decimal initialBalance = 0)
    {
        ClienteId = clienteId;
        AccountNumber = accountNumber;
        Type = type;
        Balance = initialBalance;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddBalance(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Amount must be positive.");
        Balance += amount;
    }

    public void SubtractBalance(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("Amount must be positive.");
        if (Balance < amount) throw new InvalidOperationException("Insufficient balance.");
        Balance -= amount;
    }
}