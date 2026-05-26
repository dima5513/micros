namespace Micros.HHParser;

public class HhVacancyParserOptions
{
    public string HhHost { get; set; } = "";
    public string UserAgent { get; set; } = "";
    public int IntervalSeconds { get; set; } = 60;
    public int MaxConcurrency { get; set; } = 2;
}