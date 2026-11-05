using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BT.Application.Features.ControlPlane.Tenants.Queries;
using BT.SharedKernel.Dtos.Common;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BT.Infrastructure.Features.ControlPlane.Tenants.QueryHandlers;

public class GetAllTenantsQueryHandler : IRequestHandler<GetAllTenantsQuery, AppResponse<List<TenantResponse>>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;

    public GetAllTenantsQueryHandler(IControlPlaneUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppResponse<List<TenantResponse>>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var rawTenants = await _unitOfWork.Tenants.FindAll()
            .Include(t => t.Modules)
            .Include(t => t.AdministratorInvitations)
            .AsNoTracking()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var tenants = rawTenants
            .Select(t => new TenantResponse
            {
                AdministratorOnboarding = t.AdministratorInvitations.Count == 0 ? "Existing tenant" : t.AdministratorInvitations.Any(item => item.Status != BT.Domain.Features.ControlPlane.Tenants.Enums.AdministratorInvitationStatus.Delivered) ? "Administrator invitation pending" : "Administrator invitation delivered",
                Id = t.Id,
                Revision = t.Revision,
                Identifier = t.Identifier,
                DisplayName = t.DisplayName,
                HostName = t.HostName,
                ContactEmail = t.ContactEmail,
                MaxUsers = t.MaxUsers,
                SubscriptionTier = t.SubscriptionTier.ToDisplayString(),
                Status = t.Status.ToDisplayString(),
                DeploymentStampId = t.DeploymentStampId,
                DatabaseProvider = t.DatabaseProvider,
                DatabaseConnectionString = t.DatabaseConnectionString != null ? "********" : null,
                EnabledModules = t.Modules?.Where(m => m.IsActive).Select(m => m.ModuleKey).ToList() ?? [],
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToList();

        return AppResponses.Success(tenants);
    }
}
