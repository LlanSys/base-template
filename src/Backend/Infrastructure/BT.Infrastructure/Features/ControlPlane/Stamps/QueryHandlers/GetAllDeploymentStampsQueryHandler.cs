using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BT.Application.Features.ControlPlane.Stamps.Queries;
using BT.SharedKernel.Dtos.Common;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using BT.SharedKernel.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.ControlPlane.Stamps.QueryHandlers;

public class GetAllDeploymentStampsQueryHandler : IRequestHandler<GetAllDeploymentStampsQuery, AppResponse<List<DeploymentStampResponse>>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;

    public GetAllDeploymentStampsQueryHandler(IControlPlaneUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppResponse<List<DeploymentStampResponse>>> Handle(GetAllDeploymentStampsQuery request, CancellationToken cancellationToken)
    {
        var rawStamps = await _unitOfWork.DeploymentStamps.FindAll()
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var stamps = rawStamps
            .Select(s => new DeploymentStampResponse
            {
                Id = s.Id,
                Revision = s.Revision,
                IsActive = s.IsActive,
                Name = s.Name,
                TargetResourceGroup = s.TargetResourceGroup,
                IsolationTier = s.IsolationTier.ToDisplayString(),
                KeyVaultUri = s.KeyVaultUri,
                DatabaseProvider = s.DatabaseProvider,
                DatabaseConnectionString = s.DatabaseConnectionString != null ? "********" : null,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToList();

        return AppResponses.Success(stamps);
    }
}
