namespace Micros.Scheduler;

public class SchedulerOptions
{
    // TickerQ ждёт 6-польный cron (sec min hour day month day-of-week), а не 5-польный.
    // "0 */2 * * * *" = каждые 2 минуты. 5-польное "*/2 * * * *" роняет валидацию AddAsync.
    public string ParseCronExpression { get; set; } = "0 */2 * * * *";
};