using SavedByTheMaid.Application.DTOs.Contact;
using SavedByTheMaid.Domain.Common;

namespace SavedByTheMaid.Application.Interfaces;

/// <summary>
/// Application service for handling contact/inquiry requests.
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Sends a contact request (inquiry, feedback, or support message).
    /// </summary>
    Task<Result> SendContactRequestAsync(ContactRequest request);
}
