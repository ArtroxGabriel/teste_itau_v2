using MediatR;

namespace ItauCompraProgramada.Application.Admin.Queries.GetMasterCustody;

public record MasterCustodyItem(string Ticker, int Quantidade, decimal PrecoMedio);

public record GetMasterCustodyResult(List<MasterCustodyItem> Custodias);

public record GetMasterCustodyQuery() : IRequest<GetMasterCustodyResult>;