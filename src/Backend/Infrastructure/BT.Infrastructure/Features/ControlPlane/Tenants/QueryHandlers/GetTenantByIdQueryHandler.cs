using System;
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

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;

    public GetTenantByIdQueryHandler(IControlPlaneUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AppResponse<TenantResponse>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Tenants.FindAll()
            .Include(t => t.Modules)
            .Include(t => t.AdministratorInvitations)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        var response = new TenantResponse
        {
            AdministratorOnboarding = tenant.AdministratorInvitations.Count == 0 ? "Existing tenant" : tenant.AdministratorInvitations.Any(item => item.Status != BT.Domain.Features.ControlPlane.Tenants.Enums.AdministratorInvitationStatus.Delivered) ? "Administrator invitation pending" : "Administrator invitation delivered",
            Id = tenant.Id,
            Revision = tenant.Revision,
            Identifier = tenant.Identifier,
            DisplayName = tenant.DisplayName,
            HostName = tenant.HostName,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            SubscriptionTier = tenant.SubscriptionTier.ToDisplayString(),
            Status = tenant.Status.ToDisplayString(),
            DeploymentStampId = tenant.DeploymentStampId,
            DatabaseProvider = tenant.DatabaseProvider,
            DatabaseConnectionString = tenant.DatabaseConnectionString != null ? "********" : null,
            EnabledModules = tenant.Modules?.Where(m => m.IsActive).Select(m => m.ModuleKey).ToList() ?? [],
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(response);
    }
}
