using System.ComponentModel.DataAnnotations;
using App.Application.Contacts.Commands;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Contacts;

/// <summary>
/// Page model for creating a new contact.
/// </summary>
public class Create : BaseStaffPageModel
{
    [BindProperty]
    public CreateContactViewModel Form { get; set; } = new();

    public IActionResult OnGet()
    {
        ViewData["Title"] = "Create Contact";
        ViewData["ActiveMenu"] = "Contacts";
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var command = new CreateContact.Command
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
            OrganizationAccount = Form.OrganizationAccount,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"Contact #{response.Result} created successfully.");
            return RedirectToPage(RouteNames.Contacts.Details, new { id = response.Result });
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record CreateContactViewModel
    {
        public long? Id { get; set; }

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
