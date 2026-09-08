namespace Maintenance.Domain.Users;

public interface IUserTokenRepository
{
    Task<UserToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task AddAsync(UserToken token, CancellationToken cancellationToken);

    /// <summary>Marks every still-usable token of the user and purpose as used at <paramref name="now"/>.</summary>
    Task SupersedeAsync(Guid userId, TokenPurpose purpose, DateTime now, CancellationToken cancellationToken);
}
