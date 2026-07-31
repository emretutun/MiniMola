using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Abstractions;

public interface IUserProfileService
{
    Task<int> EnsureUserProfileAsync(
        string identityUserId,
        string displayName,
        CancellationToken cancellationToken = default);
}