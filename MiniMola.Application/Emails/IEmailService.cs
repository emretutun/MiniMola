using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Emails;

public interface IEmailService
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage,
        CancellationToken cancellationToken = default);
}