using Microsoft.AspNetCore.Identity.UI.Services;
using MiniMola.Application.Emails;

namespace MiniMola.Web.Services;

public sealed class IdentityEmailSender(
    IEmailService emailService)
    : IEmailSender
{
    public Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        return emailService.SendAsync(
            email,
            subject,
            htmlMessage);
    }
}