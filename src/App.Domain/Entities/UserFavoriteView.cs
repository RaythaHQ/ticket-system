namespace App.Domain.Entities;

/// <summary>
/// Represents a user's favorited ticket view for quick access in the sidebar.
/// </summary>
public class UserFavoriteView : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid TicketViewId { get; set; }
    public virtual TicketView TicketView { get; set; } = null!;

    /// <summary>
    /// Display order of the favorite (lower numbers appear first).
    /// </summary>
    public int DisplayOrder { get; set; }
}

