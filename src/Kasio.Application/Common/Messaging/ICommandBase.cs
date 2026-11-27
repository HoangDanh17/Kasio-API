namespace Kasio.Application.Common.Messaging;

/// <summary>
/// Marker cho command (ghi dữ liệu). Chỉ request có marker này mới được bọc trong
/// transaction + SaveChanges/Commit bởi <c>UnitOfWorkBehavior</c>.
/// </summary>
public interface ICommandBase { }
