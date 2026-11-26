using Comprove.HelpDesk.Api.Auth;
using Comprove.HelpDesk.Api.Mapping;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Queries;
using Comprove.HelpDesk.Application.UseCases;
using Comprove.HelpDesk.Contracts.Http;
using Comprove.HelpDesk.Domain.Entities;
using Comprove.HelpDesk.Domain.Enums;
using Comprove.HelpDesk.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Comprove.HelpDesk.Api.Controllers;

// Central de FAQ: qualquer usuário autenticado do tenant lê, registra dúvida e vota.
[ApiController]
[Authorize]
[Route("api/v1/faqs")]
public sealed class FaqsController(
    CreateFaqUseCase create,
    FaqReadUseCase reads,
    IValidator<CreateFaqRequest> createValidator) : ControllerBase
{
    private RequestContext Context => RequestContextFactory.Create(HttpContext);

    [HttpGet]
    public async Task<ActionResult<PagedResponse<FaqResponse>>> List(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? cursor,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var parsedStatus = string.IsNullOrWhiteSpace(status) ? (FaqResolutionStatus?)null : FrontendFaqStatusMapper.Parse(status);
        var page = await reads.ListAsync(Context, parsedStatus, search, cursor, pageSize, ct);
        return Ok(new PagedResponse<FaqResponse>(page.Items.Select(ToResponse).ToList(), page.NextCursor));
    }

    [HttpGet("{faqId}", Name = nameof(GetFaq))]
    public async Task<ActionResult<FaqResponse>> GetFaq(string faqId, CancellationToken ct) =>
        Ok(ToResponse(await reads.GetAsync(Context, faqId, ct)));

    [HttpPost]
    public async Task<ActionResult<FaqResponse>> Create([FromBody] CreateFaqRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var faq = await create.ExecuteAsync(Context, ToCommand(request), ct);
        return CreatedAtAction(nameof(GetFaq), new { faqId = faq.Id }, FaqResponseMapper.ToResponse(faq, Context.IsAgent));
    }

    [HttpPost("{faqId}/upvote")]
    public async Task<ActionResult<UpvoteResponse>> ToggleUpvote(string faqId, CancellationToken ct)
    {
        var result = await reads.ToggleUpvoteAsync(Context, faqId, ct);
        return Ok(new UpvoteResponse(result.AskCount, result.Upvoted));
    }

    private FaqResponse ToResponse(FaqView view) => FaqResponseMapper.ToResponse(view, Context.IsAgent);

    private static CreateFaqCommand ToCommand(CreateFaqRequest request) => new(
        request.Title,
        request.ProblemDescription,
        new FaqDraft(request.ProblemField, request.SpecificScreen, request.BugType, request.BugSubType),
        ParseEnum<ImpactLevel>(request.Impact),
        ParseEnum<ScopeLevel>(request.Scope),
        ParseEnum<FrequencyLevel>(request.Frequency),
        ParseEnum<UrgencyLevel>(request.Urgency),
        request.IdempotencyKey);

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainRuleViolationException($"Valor inválido para {typeof(T).Name}: {value}.");
}
