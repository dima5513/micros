using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Micros.HHParser;

public class HhVacancyHttpApiClient(HttpClient httpClient, ILogger<HhVacancyHttpApiClient> logger)
{
    private static readonly HtmlParser HtmlParser = new();

    public async IAsyncEnumerable<HhVacancyItem> SearchHtmlAsync(
        NameValueCollection query,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        foreach (string? key in query.AllKeys)
        foreach (var v in query.GetValues(key) ?? [])
            q.Add(key, v);

        var page = 0;
        var seen = 0;


        while (true)
        {
            q["page"] = page.ToString();


            var response = await GetHhVacancies(q, ct);

            if (response is null) yield break;

            var result = response.HhVacancySearchResult;

            if (result.Vacancies.Length == 0) yield break;

            foreach (var v in result.Vacancies)
            {
                yield return v;
                seen++;
            }

            if (seen >= result.Total) yield break;
            if (result.Paging?.Next is null) yield break;
            if (result.Paging.Next.Disabled) yield break;

            page = result.Paging.Next.Page;


            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    private async Task<HhVacanciesResponse?> GetHhVacancies(NameValueCollection query, CancellationToken ct)
    {
        var html = await httpClient.GetStringAsync($"search/vacancy?{query}", cancellationToken: ct);

        using var doc = await HtmlParser.ParseDocumentAsync(html);

        var template = doc.QuerySelector("template#HH-Lux-InitialState") as IHtmlTemplateElement;

        var json = template?.Content.TextContent ??
                   throw new InvalidOperationException("HH-Lux-InitialState not found");

        return JsonSerializer.Deserialize<HhVacanciesResponse>(json);
    }
}