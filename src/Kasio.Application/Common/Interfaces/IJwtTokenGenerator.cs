using Kasio.Domain.Users;

namespace Kasio.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}
