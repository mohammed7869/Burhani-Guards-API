using BurhaniGuards.Api.Domain;
using BurhaniGuards.Api.Persistence.Mongo;
using BurhaniGuards.Api.Repositories.Interfaces;
using MongoDB.Driver;

namespace BurhaniGuards.Api.Repositories.Mongo;

public sealed class MemberRepository : IMemberRepository
{
    private readonly MongoContext _context;

    public MemberRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task<MemberDocument?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<MemberDocument>.Filter.Eq(x => x.Email, email.ToLowerInvariant());
        return await _context.Members.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Member?> GetByItsIdAsync(string itsId, CancellationToken cancellationToken = default)
    {
        // MongoDB implementation not supported - use MySQL repository instead
        throw new NotSupportedException("GetByItsIdAsync is not supported in MongoDB repository. Use MySQL repository instead.");
    }

    public Task<bool> UpdateNewPasswordAsync(string itsId, string newPasswordHash, CancellationToken cancellationToken = default)
    {
        // MongoDB implementation not supported - use MySQL repository instead
        throw new NotSupportedException("UpdateNewPasswordAsync is not supported in MongoDB repository. Use MySQL repository instead.");
    }
}

