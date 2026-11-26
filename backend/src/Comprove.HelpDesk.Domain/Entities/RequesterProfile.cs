namespace Comprove.HelpDesk.Domain.Entities;

// Retrato de quem abriu o chamado, tirado do token no momento da abertura.
public sealed record RequesterProfile(string? Name, string? Phone, IReadOnlyList<string> Roles);
