namespace App.Application.Dashboard;

public record DashboardDto
{
    public int TotalUsers { get; init; }
    public decimal DbSize { get; init; }
}
