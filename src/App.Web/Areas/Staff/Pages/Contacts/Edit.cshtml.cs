using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Contacts;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Web.Areas.Staff.Pages.Shared.Models;

namespace App.Web.Areas.Staff.Pages.Contacts;

/// <summary>
/// Page model for editing a contact.
/// </summary>
public class Edit : BaseStaffPageModel
{
    public ContactDto Contact { get; set; } = null!;

    [BindProperty]
    public EditContactViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(long id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Edit Contact";
        ViewData["ActiveMenu"] = "Contacts";

        var response = await Mediator.Send(new GetContactById.Query { Id = id }, cancellationToken);
        Contact = response.Result;

        Form = new EditContactViewModel
        {
            Id = Contact.Id,
            Name = Contact.Name,
            Email = Contact.Email,
            PhoneNumbers = string.Join("\n", Contact.PhoneNumbers),
            Address = Contact.Address,
            OrganizationAccount = Contact.OrganizationAccount
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new UpdateContact.Command
        {
            Id = Form.Id,
            Name = Form.Name,
            Email = Form.Email,
            PhoneNumbers = Form.PhoneNumbers?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList(),
            Address = Form.Address,
            OrganizationAccount = Form.OrganizationAccount
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Contact updated successfully.");
            return RedirectToPage("./Details", new { id = Form.Id });
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record EditContactViewModel
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumbers { get; set; }

        public string? Address { get; set; }

        public string? OrganizationAccount { get; set; }
    }
}

