using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Contacts.Commands;
using App.Web.Areas.Staff.Pages.Shared.Models;

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
            SetSuccessMessage($"Contact #{response.Result} created successfully.");
            return RedirectToPage("./Details", new { id = response.Result });
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record CreateContactViewModel
    {
        [Required]
        [MaxLength(500)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Phone numbers, one per line.
        /// </summary>
        public string? PhoneNumbers { get; set; }

        public string? Address { get; set; }

        public string? OrganizationAccount { get; set; }
    }
}

