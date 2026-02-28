using FluentAssertions;

using ItauCompraProgramada.Application.Taxes.Services;

using Xunit;

namespace ItauCompraProgramada.UnitTests.Application.Taxes.Services;

public class TaxServiceTests
{
    private readonly TaxService _taxService;

    public TaxServiceTests()
    {
        _taxService = new TaxService();
    }

    [Theory]
    [InlineData(100, 0.005)] // 100 * 0.00005 = 0.005
    [InlineData(1000, 0.05)] // 1000 * 0.00005 = 0.05
    public void CalculateIrDedoDuro_ShouldApplyCorrectAliquota(decimal operationValue, decimal expectedIr)
    {
        var result = _taxService.CalculateIrDedoDuro(operationValue);
        result.Should().Be(expectedIr);
    }

    [Fact]
    public void CalculateProfitTax_Isento_WhenSalesUnder20k()
    {
        decimal totalSalesInMonth = 19999.99m;
        decimal totalProfit = 1000m;

        var result = _taxService.CalculateProfitTax(totalSalesInMonth, totalProfit);

        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateProfitTax_Tributado_WhenSalesOver20k()
    {
        decimal totalSalesInMonth = 20000.01m;
        decimal totalProfit = 1000m; // 20% of 1000 = 200

        var result = _taxService.CalculateProfitTax(totalSalesInMonth, totalProfit);

        result.Should().Be(200m);
    }

    [Fact]
    public void CalculateProfitTax_Zero_WhenLossEvenIfSalesOver20k()
    {
        decimal totalSalesInMonth = 25000m;
        decimal totalProfit = -500m; // Loss

        var result = _taxService.CalculateProfitTax(totalSalesInMonth, totalProfit);

        result.Should().Be(0m);
    }
}