using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.News;

public interface INewsService
{
    Task<NewsFeedDto> GetHeadlinesAsync(
        CancellationToken cancellationToken = default);
}