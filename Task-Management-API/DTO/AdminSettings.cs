namespace Task_Management_API.DTO
{
    public class AdminSettings
    {
        public List<string> AdminEmails { get; set; } = new();
        public string DefaultAdminPassword { get; set; } = "Admin@123456";

    }
}
