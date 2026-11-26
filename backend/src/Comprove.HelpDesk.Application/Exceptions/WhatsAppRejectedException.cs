namespace Comprove.HelpDesk.Application.Exceptions;

// Repetir não resolve (ou poderia duplicar a mensagem): vai para a DLQ para conferência.
public sealed class WhatsAppRejectedException(string message) : Exception(message);
