using System.Text.Json.Serialization;

namespace Micros.HHParser;

public record HhVacanciesResponse(
    [property: JsonPropertyName("vacancySearchResult")]
    HhVacancySearchResult HhVacancySearchResult
);

public record HhPaging(
    [property: JsonPropertyName("pages")] HhPagingItem[] Pages,
    [property: JsonPropertyName("next")] HhPagingNext? Next
);

public record HhPagingNext(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("disabled")]
    bool Disabled
);

public record HhPagingItem(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("selected")]
    bool IsSelected
);

public record HhVacancySearchResult(
    [property: JsonPropertyName("vacancies")]
    HhVacancyItem[] Vacancies,
    [property: JsonPropertyName("paging")] HhPaging? Paging,
    [property: JsonPropertyName("totalResults")]
    int Total);

public record HhVacancyItem(
    [property: JsonPropertyName("vacancyId")]
    long VacancyId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("creationTime")]
    DateTimeOffset CreationTime);
