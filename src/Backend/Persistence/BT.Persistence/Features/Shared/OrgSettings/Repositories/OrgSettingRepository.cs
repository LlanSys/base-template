using BT.Domain.Features.Shared.OrgSettings.Contracts.Repositories;
using BT.Domain.Features.Shared.OrgSettings.Entities;
using BT.Persistence.Common.Repositories;
using BT.Persistence.Features.Shared.DataContext;

using Microsoft.EntityFrameworkCore;

namespace BT.Persistence.Features.Shared.OrgSettings.Repositories;

public class OrgSettingRepository(SharedDBContext dbContext)
    : Repository<OrgSetting>(dbContext), IOrgSettingRepository
{
    private readonly SharedDBContext _dbContext = dbContext;
}
