using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viamatica.Application.Assistant;

namespace Viamatica.Api.Controllers;

/// <summary>Asistente con IA. Responde solo con datos del alcance del rol autenticado.</summary>
[ApiController]
[Route("api/assistant")]
[Authorize]
public class AssistantController : ControllerBase
{
    private readonly AssistantService _assistant;

    public AssistantController(AssistantService assistant) => _assistant = assistant;

    /// <summary>Indica si el asistente está configurado y sugiere preguntas según el rol.</summary>
    [HttpGet]
    public AssistantInfo Info() => _assistant.Info();

    [HttpPost("chat")]
    public Task<ChatResponse> Chat(ChatRequest request, CancellationToken ct) => _assistant.ChatAsync(request, ct);
}
