using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FixedIT.API.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub<INotificationsClient>;
