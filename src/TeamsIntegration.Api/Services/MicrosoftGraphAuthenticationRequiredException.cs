namespace TeamsIntegration.Api.Services;

public sealed class MicrosoftGraphAuthenticationRequiredException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
