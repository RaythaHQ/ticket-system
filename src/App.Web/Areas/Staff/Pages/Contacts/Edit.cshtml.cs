using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Contacts;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Web.Areas.Staff.Pages.Shared;
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
            FirstName = Contact.FirstName,
            LastName = Contact.LastName,
            Email = Contact.Email,
            PhoneNumbersList = Contact.PhoneNumbers.ToList(),
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
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            Email = Form.Email,
            PhoneNumbers = Form.PhoneNumbersList?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList(),
            Address = Form.Address,
            OrganizationAccount = Form.OrganizationAccount
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Contact updated successfully.");
            return RedirectToPage(RouteNames.Contacts.Details, new { id = Form.Id });
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record EditContactViewModel
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(250)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Phone numbers as a list for the interactive editor.
        /// </summary>
        public List<string>? PhoneNumbersList { get; set; }

        public string? Address { get; set; }

        public string? OrganizationAccount { get; set; }
    }
}

