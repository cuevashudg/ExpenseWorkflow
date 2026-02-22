using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Workflow.Domain.Entities;
using Workflow.Infrastructure.Data;

namespace Workflow.Api.Authorization;

public class UserLookupService : IUserLookupService
{
    private readonly WorkflowDbContext _db;
    public UserLookupService(WorkflowDbContext db)
    {
        _db = db;
    }

    public Task<ApplicationUser?> GetUserByIdAsync(Guid userId)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }
}
