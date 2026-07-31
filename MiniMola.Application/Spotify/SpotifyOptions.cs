using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Infrastructure.Spotify;

public sealed class SpotifyOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}