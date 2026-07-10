using System.Text.Json;
using Micros.Api.Infrastructure.Database;
using Micros.Api.Infrastructure.Outbox;
using Micros.Core.logging;
using Micros.Core.messages;
using Micros.Core.rabbitmq;
using Microsoft.EntityFrameworkCore;

namespace Micros.Api.Domains.User;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserEntity> GetById(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(e => e.Id == id);

        if (user is null)
            throw new UserNotFoundException("");

        return user;
    }

    public async Task UpdateMeAsync(UpdateMeContract contract)
    {
        var user = await _db.Users.FirstOrDefaultAsync(e => e.Id == contract.UserId)
                   ?? throw new UserNotFoundException("");

        var oldTelegramId = user.TelegramId;

        if (contract.TelegramId.IsSet)
        {
            var newTelegramId = contract.TelegramId.Value;

            if (newTelegramId is not null && newTelegramId != user.TelegramId)
            {
                var taken = await _db.Users.AnyAsync(u => u.TelegramId == newTelegramId);
                if (taken)
                    throw new TelegramIdAlreadyExistsException(newTelegramId.Value);
            }

            user.TelegramId = newTelegramId;
        }


        if (oldTelegramId != user.TelegramId)
        {
            _db.OutboxMessages.Add(new OutboxMessageEntity
            {
                Exchange = HHUserTopology.Exchange,
                RoutingKey = HHUserTopology.UpdateUserKey,
                Payload = JsonSerializer.Serialize(new HHUserUpdatedMessage(user.Id, user.TelegramId)),
                CorrelationId = CorrelationIdContext.Current,
            });
        }

        await _db.SaveChangesAsync();
    }
}