namespace Task_Management_API.Paggination
{
    public class PaginationParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public const int MaxPageSize = 50;

        public int ValidatedPageSize => (PageSize > MaxPageSize) ? MaxPageSize : PageSize;

    }
}
