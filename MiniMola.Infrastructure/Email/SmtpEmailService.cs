using System;
using System.Collections.Generic;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MiniMola.Application.Emails;

namespace MiniMola.Infrastructure.Email;

public sealed class SmtpEmailService(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<SmtpEmailService> logger)
    : IEmailService
{
    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            recipientEmail);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            subject);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            htmlMessage);

        var options = smtpOptions.Value;

        ValidateOptions(options);

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                options.FromName,
                options.FromEmail));

        message.To.Add(
            MailboxAddress.Parse(
                recipientEmail));

        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = htmlMessage
        }.ToMessageBody();

        using var smtpClient =
            new SmtpClient();

        smtpClient.Timeout = 30_000;

        try
        {
            var socketOptions =
                options.UseStartTls
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            await smtpClient.ConnectAsync(
                options.Host,
                options.Port,
                socketOptions,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                options.Username,
                options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            logger.LogInformation(
                "E-posta SMTP üzerinden gönderildi. "
                + "Subject: {Subject}",
                subject);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(
                    true,
                    CancellationToken.None);
            }
        }
    }

    private static void ValidateOptions(
        SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(
                options.Host))
        {
            throw new InvalidOperationException(
                "Smtp:Host ayarı bulunamadı.");
        }

        if (options.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Smtp:Port ayarı geçerli değil.");
        }

        if (string.IsNullOrWhiteSpace(
                options.Username))
        {
            throw new InvalidOperationException(
                "Smtp:Username ayarı bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(
                options.Password))
        {
            throw new InvalidOperationException(
                "Smtp:Password ayarı bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(
                options.FromEmail))
        {
            throw new InvalidOperationException(
                "Smtp:FromEmail ayarı bulunamadı.");
        }
    }
}