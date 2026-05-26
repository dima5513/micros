namespace Micros.Core.messages;

public record NewVacancyMessage(
    Guid TaskId,
    long VacancyId,
    string Name,
    long TelegramId,
    DateTimeOffset CreationTime,
    string Url
);