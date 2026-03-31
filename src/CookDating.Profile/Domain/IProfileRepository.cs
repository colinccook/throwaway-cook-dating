using CookDating.SharedKernel.Domain;

namespace CookDating.Profile.Domain;

public interface IProfileRepository : IRepository<UserProfile, string>
{
}
