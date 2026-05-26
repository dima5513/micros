namespace Micros.Api.Domains.User;

public interface IUserService
{
    Task<UserEntity> GetById(Guid id);
    Task UpdateMeAsync(UpdateMeContract contract);
}
