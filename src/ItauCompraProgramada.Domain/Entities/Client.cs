using System;

using ItauCompraProgramada.Domain.Enums;

namespace ItauCompraProgramada.Domain.Entities;

public class Client
{
    public long Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Cpf { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public decimal MonthlyContribution { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime AdhesionDate { get; private set; }
    public int NextPurchaseDay { get; private set; }

    // Navigation
    public virtual GraphicAccount? GraphicAccount { get; private set; }
    public virtual IReadOnlyCollection<ContributionUpdate> ContributionHistory => _contributionHistory.AsReadOnly();
    private readonly List<ContributionUpdate> _contributionHistory = [];

    protected Client() { } // EF Constructor

    public Client(string name, string cpf, string email, decimal monthlyContribution, int? purchaseDay = null)
    {
        if (monthlyContribution < 100m)
            throw new ArgumentException("Monthly contribution must be at least R$ 100,00.");

        Name = name;
        Cpf = cpf;
        Email = email;
        MonthlyContribution = monthlyContribution;
        IsActive = true;
        AdhesionDate = DateTime.UtcNow;

        // Set next purchase day (5, 15, or 25)
        NextPurchaseDay = purchaseDay ?? CalculateNextPurchaseDay(AdhesionDate);

        // Auto-create GraphicAccount
        var accountNumber = "ACC-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        GraphicAccount = new GraphicAccount(0, accountNumber, AccountType.Filhote, monthlyContribution);
    }

    private static int CalculateNextPurchaseDay(DateTime adhesionDate)
    {
        if (adhesionDate.Day < 5) return 5;
        if (adhesionDate.Day < 15) return 15;
        if (adhesionDate.Day < 25) return 25;
        return 5; // Next month
    }

    public void Deactivate() => IsActive = false;
    public void UpdateContribution(decimal newValue)
    {
        if (newValue < 100m)
            throw new ArgumentException("Monthly contribution must be at least R$ 100,00.");
            
        var oldValue = MonthlyContribution;
        MonthlyContribution = newValue;
        _contributionHistory.Add(new ContributionUpdate(Id, oldValue, newValue));
    }
}