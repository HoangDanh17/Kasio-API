using Kasio.Contracts.User;
using Kasio.Domain.Users;

namespace Kasio.Application.Common.Interfaces;

public interface IUserPermissionReader
{
    /// <summary>Trạng thái từng quyền của user, hoặc null nếu user không tồn tại.</summary>
    Task<IReadOnlyList<UserPermissionStateResponse>?> GetStatesAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Mã các quyền user thực sự có (dùng cho authorization).</summary>
    Task<IReadOnlySet<string>> GetEffectivePermissionCodesAsync(UserId userId, CancellationToken cancellationToken = default);
}
