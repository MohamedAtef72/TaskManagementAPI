using Task_Management_API.Domain.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;
}