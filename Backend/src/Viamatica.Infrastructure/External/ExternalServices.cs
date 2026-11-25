using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viamatica.Application.Abstractions;
using Viamatica.Domain.Common;

namespace Viamatica.Infrastructure.External;

public class EmailOptions
{
    public const string Section = "Email";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string FromName { get; set; } = "NetCaja";
}

/// <summary>Envío por SMTP con STARTTLS (compatible con Gmail y contraseñas de aplicación).</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.User) && !string.IsNullOrWhiteSpace(_options.Password);

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.User!, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(to);

            // EnableSsl en el puerto 587 negocia STARTTLS antes de autenticar.
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_options.User, _options.Password),
            };
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "No se pudo enviar el correo con asunto {Subject}", subject);
            throw new DomainException("No se pudo enviar el correo. Intente nuevamente en unos minutos.", 502);
        }
    }
}

public class AssistantOptions
{
    public const string Section = "Assistant";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.5-flash";
}

/// <summary>Cliente de la API de Gemini (generateContent) por HTTP.</summary>
public class GeminiAssistantModel : IAssistantModel
{
    private readonly HttpClient _http;
    private readonly AssistantOptions _options;
    private readonly ILogger<GeminiAssistantModel> _logger;

    public GeminiAssistantModel(HttpClient http, IOptions<AssistantOptions> options, ILogger<GeminiAssistantModel> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> AskAsync(string systemPrompt, IReadOnlyList<AssistantTurn> conversation, CancellationToken ct = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent";
        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = conversation.Select(turn => new
            {
                role = turn.FromUser ? "user" : "model",
                parts = new[] { new { text = turn.Text } },
            }),
            generationConfig = new { temperature = 0.2, maxOutputTokens = 2048 },
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            request.Headers.Add("x-goog-api-key", _options.ApiKey);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("El modelo del asistente respondió {Status}: {Detail}",
                    (int)response.StatusCode, detail.Length > 300 ? detail[..300] : detail);
                // 429: se agotó la cuota de consultas por minuto del modelo.
                throw (int)response.StatusCode == 429
                    ? new DomainException("El asistente alcanzó su límite de consultas. Espere un minuto e intente de nuevo.", 429)
                    : Unavailable();
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var parts = json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
            var text = string.Concat(parts.EnumerateArray()
                .Where(p => p.TryGetProperty("text", out _))
                .Select(p => p.GetProperty("text").GetString()));
            return string.IsNullOrWhiteSpace(text) ? throw Unavailable() : text.Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            _logger.LogError(ex, "Error al consultar el modelo del asistente");
            throw Unavailable();
        }
    }

    private static DomainException Unavailable() =>
        new("El asistente no está disponible en este momento. Intente nuevamente.", 502);
}
