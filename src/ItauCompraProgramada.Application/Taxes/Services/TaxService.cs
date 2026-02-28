using System;

namespace ItauCompraProgramada.Application.Taxes.Services;

public class TaxService
{
    private const decimal IR_DEDO_DURO_ALIQUOTA = 0.00005m; // 0.005%
    private const decimal PROFIT_TAX_ALIQUOTA = 0.20m;     // 20%
    private const decimal ISENCAO_LIMIT = 20000m;          // R$ 20.000,00

    public decimal CalculateIrDedoDuro(decimal operationValue)
    {
        // RN-053: Aliquota 0,005% sobre o valor total da operacao
        return Math.Round(operationValue * IR_DEDO_DURO_ALIQUOTA, 4);
    }

    public decimal CalculateProfitTax(decimal totalSalesInMonth, decimal totalProfit)
    {
        // RN-058: Se o total de vendas <= R$ 20.000,00: ISENTO
        if (totalSalesInMonth <= ISENCAO_LIMIT) return 0m;

        // RN-061: Se houver prejuizo, o IR e R$ 0,00
        if (totalProfit <= 0) return 0m;

        // RN-059: Se o total de vendas > R$ 20.000,00: 20% sobre o lucro liquido total
        return Math.Round(totalProfit * PROFIT_TAX_ALIQUOTA, 2);
    }
}