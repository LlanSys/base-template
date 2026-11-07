using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.ReferenceData.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.ReferenceData.Queries;

public sealed record GetReferenceCatalogQuery(string CatalogType)
    : IRequest<AppResponse<IReadOnlyList<ReferenceCatalogItemResponse>>>, ICachableRequest
{
    public string CacheGroup => "iam-reference-data";
    public string Discriminator => CacheKeys.Discriminator(new { CatalogType, Version = 2 });
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}

