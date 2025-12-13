using System.ComponentModel.DataAnnotations;
using App.Application.Contacts;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Contacts;

/// <summary>
/// Page model for changing a contact's ID number.
/// This creates a new contact with the specified ID, migrates all associated data,
/// and permanently deletes the original contact record.
/// </summary>
public class ChangeId : BaseStaffPageModel
{
    public ContactDto Contact { get; set; } = null!;

    [BindProperty]
    public ChangeIdViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = $"Change Contact ID #{id}";
        ViewData["ActiveMenu"] = "Contacts";

        var response = await Mediator.Send(new GetContactById.Query { Id = id }, cancellationToken);
        Contact = response.Result;

        Form = new ChangeIdViewModel
        {
            CurrentId = Contact.Id
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            // Reload contact for display
            var contactResponse = await Mediator.Send(
                new GetContactById.Query { Id = Form.CurrentId },
                cancellationToken
            );
            Contact = contactResponse.Result;
            return Page();
        }

        var command = new ChangeContactId.Command
        {
            CurrentId = Form.CurrentId,
            NewId = Form.NewId
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"Contact ID successfully changed from #{Form.CurrentId} to #{Form.NewId}. All associated tickets and records have been updated.");
            return RedirectToPage(RouteNames.Contacts.Details, new { id = Form.NewId });
        }

        // Reload contact for display
        try
        {
            var contactResponse = await Mediator.Send(
                new GetContactById.Query { Id = Form.CurrentId },
                cancellationToken
            );
            Contact = contactResponse.Result;
        }
        catch
        {
            // If contact not found, redirect to contacts list
            SetErrorMessage(response.GetErrors());
            return RedirectToPage(RouteNames.Contacts.Index);
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record ChangeIdViewModel
    {
        public long CurrentId { get; set; }

        [Required(ErrorMessage = "New contact ID is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "Contact ID must be a positive number.")]
        [Display(Name = "New Contact ID")]
        public long NewId { get; set; }
    }
}

