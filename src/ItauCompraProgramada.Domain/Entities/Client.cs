using System;

namespace ItauCompraProgramada.Domain.Entities;

public class Client
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public string Cpf { get; private set; }
    public string Email { get; private set; }
    public decimal MonthlyContribution { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime AdhesionDate { get; private set; }

    // Navigation
    public virtual GraphicAccount? GraphicAccount { get; private set; }

    protected Client() { } // EF Constructor

    public Client(string name, string cpf, string email, decimal monthlyContribution)
    {
        if (monthlyContribution < 100m)
            throw new ArgumentException("Monthly contribution must be at least R$ 100,00.");

        Name = name;
        Cpf = cpf;
        Email = email;
        MonthlyContribution = monthlyContribution;
        IsActive = true;
        AdhesionDate = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void UpdateContribution(decimal newValue)
    {
        if (newValue < 100m)
            throw new ArgumentException("Monthly contribution must be at least R$ 100,00.");
        MonthlyContribution = newValue;
    }
}
