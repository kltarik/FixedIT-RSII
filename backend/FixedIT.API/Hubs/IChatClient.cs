using FixedIT.API.DTOs.Chat;

namespace FixedIT.API.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(MessageResponse message);
}
