using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.News;

public sealed record NewsFeedDto(
    string SourceName,
    DateTimeOffset RetrievedAtUtc,
    IReadOnlyList<NewsItemDto> Items);

public sealed record NewsItemDto(
    string Id,
    string Title,
    string Summary,
    string Url,
    DateTimeOffset? PublishedAt);