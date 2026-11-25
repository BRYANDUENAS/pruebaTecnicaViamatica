namespace Viamatica.Application.Abstractions;

/// <summary>Envío de correos. Es opcional: si no está configurado, la aplicación lo informa.</summary>
public interface IEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public record AssistantTurn(bool FromUser, string Text);

/// <summary>Modelo de lenguaje detrás del asistente. Es opcional, igual que el correo.</summary>
public interface IAssistantModel
{
    bool IsConfigured { get; }

    /// <param name="systemPrompt">Instrucciones y datos autorizados para el rol del usuario.</param>
    /// <param name="conversation">Mensajes previos y, al final, la pregunta actual.</param>
    Task<string> AskAsync(string systemPrompt, IReadOnlyList<AssistantTurn> conversation, CancellationToken ct = default);
}
