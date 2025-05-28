namespace Task_Management_API.DTO
{
    public class UserWithRoles
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
