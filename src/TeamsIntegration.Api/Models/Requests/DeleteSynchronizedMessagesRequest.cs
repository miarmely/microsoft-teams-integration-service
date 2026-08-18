using System.ComponentModel.DataAnnotations;

namespace TeamsIntegration.Api.Models.Requests;

/// <summary>Date range used to permanently delete synchronized messages.</summary>
public sealed class DeleteSynchronizedMessagesRequest
{
    /// <summary>Inclusive lower bound of the Teams message creation time.</summary>
    [Required]
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>Inclusive upper bound of the Teams message creation time.</summary>
    [Required]
    public DateTimeOffset? ToDate { get; init; }
}
