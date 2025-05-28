namespace Task_Management_API.DTO
{
    public class ErrorResponse
    {
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }
}
