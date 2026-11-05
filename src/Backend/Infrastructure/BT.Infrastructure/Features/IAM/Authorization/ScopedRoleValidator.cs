using BT.Domain.Features.IAM.Users.Entities;
using BT.Persistence.Features.IAM.DataContext;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.IAM.Authorization;

internal sealed class ScopedRoleValidator(IamDBContext context) : IRoleValidator<AppRole>
{
    public async Task<IdentityResult> ValidateAsync(RoleManager<AppRole> manager, AppRole role)
    {
        var normalized = manager.NormalizeKey(role.Name);

        if (string.IsNullOrWhiteSpace(normalized) || role.Name!.Length > 80)
            return IdentityResult.Failed(new IdentityError { Code = "InvalidRoleName", Description = "Enter a role name of at most 80 characters." });
        var duplicate = await context.Roles.IgnoreQueryFilters().AnyAsync(existing => existing.Id != role.Id
            && existing.NormalizedName == normalized && (role.IsDefault || existing.IsDefault || existing.TenantId == role.TenantId)).ConfigureAwait(false);

        return duplicate ? IdentityResult.Failed(new IdentityError { Code = "DuplicateRoleName", Description = "This role name is reserved in the selected scope." }) : IdentityResult.Success;
    }
}
