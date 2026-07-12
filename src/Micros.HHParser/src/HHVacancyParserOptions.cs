namespace Micros.HHParser;

public class HhVacancyParserOptions
{
    public string HhHost { get; set; } = "";
    public string UserAgent { get; set; } = "";

    public int SearchPeriodDays { get; set; } = 1;
}