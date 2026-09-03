namespace TeamsIntegration.Api.Models.Responses;

public class AccountResponse
{
    public required string Id { get; init; }

    // Identity
    public string? DisplayName { get; init; }
    public string? GivenName { get; init; }
    public string? Surname { get; init; }
    public string? PreferredLanguage { get; init; }

    // Contact
    public string? Mail { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? MobilePhone { get; init; }
    public IReadOnlyCollection<string> BusinessPhones { get; init; } = [];

    // Organization
    public string? JobTitle { get; init; }
    public string? Department { get; init; }
    public string? CompanyName { get; init; }
    public string? EmployeeId { get; init; }

    // Location
    public string? OfficeLocation { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }

    // Account
    public string? UserType { get; init; }
    public bool? AccountEnabled { get; init; }
}
