using System.Net;
using System.Text.Json;
using FluentValidation;

namespace ItauCompraProgramada.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, erro, codigo) = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            KeyNotFoundException keyNotFoundEx => HandleKeyNotFoundException(keyNotFoundEx),
            InvalidOperationException invalidOpEx => HandleInvalidOperationException(invalidOpEx),
            _ => ((int)HttpStatusCode.InternalServerError, "Erro interno no servidor.", "ERRO_INTERNO")
        };

        context.Response.StatusCode = statusCode;

        var result = JsonSerializer.Serialize(new { erro, codigo });
        await context.Response.WriteAsync(result);
    }

    private static (int StatusCode, string Erro, string Codigo) HandleValidationException(ValidationException ex)
    {
        var firstError = ex.Errors.FirstOrDefault();
        var message = firstError?.ErrorMessage ?? "Erro de validação.";
        var code = firstError?.ErrorCode;

        // Fallback code mappings if ErrorCode wasn't explicitly set in FluentValidation
        if (string.IsNullOrEmpty(code))
        {
            code = message switch
            {
                var msg when msg.Contains("CPF", StringComparison.OrdinalIgnoreCase) => "CLIENTE_CPF_DUPLICADO",
                var msg when msg.Contains("mínimo", StringComparison.OrdinalIgnoreCase) || msg.Contains("minimo", StringComparison.OrdinalIgnoreCase) => "VALOR_MENSAL_INVALIDO",
                var msg when msg.Contains("soma", StringComparison.OrdinalIgnoreCase) || msg.Contains("percentuais", StringComparison.OrdinalIgnoreCase) => "PERCENTUAIS_INVALIDOS",
                var msg when msg.Contains("5 ativos", StringComparison.OrdinalIgnoreCase) || msg.Contains("exatamente 5", StringComparison.OrdinalIgnoreCase) => "QUANTIDADE_ATIVOS_INVALIDA",
                var msg when msg.Contains("inativo", StringComparison.OrdinalIgnoreCase) => "CLIENTE_JA_INATIVO",
                _ => "ERRO_VALIDACAO"
            };
        }

        return ((int)HttpStatusCode.BadRequest, message, code);
    }

    private static (int StatusCode, string Erro, string Codigo) HandleKeyNotFoundException(KeyNotFoundException ex)
    {
        var message = ex.Message;
        var code = message switch
        {
            var msg when msg.Contains("Cliente", StringComparison.OrdinalIgnoreCase) => "CLIENTE_NAO_ENCONTRADO",
            var msg when msg.Contains("Cesta", StringComparison.OrdinalIgnoreCase) => "CESTA_NAO_ENCONTRADA",
            var msg when msg.Contains("Cotação", StringComparison.OrdinalIgnoreCase) || msg.Contains("Cotacao", StringComparison.OrdinalIgnoreCase) => "COTACAO_NAO_ENCONTRADA",
            _ => "NAO_ENCONTRADO"
        };
        return ((int)HttpStatusCode.NotFound, message, code);
    }

    private static (int StatusCode, string Erro, string Codigo) HandleInvalidOperationException(InvalidOperationException ex)
    {
        var message = ex.Message;
        
        // Specific checks for 404/409 scenarios based on requirements
        if (message.Contains("Cesta", StringComparison.OrdinalIgnoreCase) || message.Contains("Nenhuma cesta", StringComparison.OrdinalIgnoreCase))
            return ((int)HttpStatusCode.NotFound, message, "CESTA_NAO_ENCONTRADA");

        if (message.Contains("já executada", StringComparison.OrdinalIgnoreCase) || message.Contains("ja executada", StringComparison.OrdinalIgnoreCase))
            return ((int)HttpStatusCode.Conflict, message, "COMPRA_JA_EXECUTADA");

        if (message.Contains("Cotação", StringComparison.OrdinalIgnoreCase) || message.Contains("Cotacao", StringComparison.OrdinalIgnoreCase))
            return ((int)HttpStatusCode.NotFound, message, "COTACAO_NAO_ENCONTRADA");

        if (message.Contains("inativo", StringComparison.OrdinalIgnoreCase))
            return ((int)HttpStatusCode.BadRequest, message, "CLIENTE_JA_INATIVO");

        if (message.Contains("Kafka", StringComparison.OrdinalIgnoreCase))
            return ((int)HttpStatusCode.InternalServerError, message, "KAFKA_INDISPONIVEL");

        return ((int)HttpStatusCode.BadRequest, message, "OPERACAO_INVALIDA");
    }
}
