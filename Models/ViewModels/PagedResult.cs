namespace WorkerBookingSystem.Models.ViewModels
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalItems { get; set; }
        public string? Search { get; set; }
        public string? Skill { get; set; }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class WorkerSearchItemViewModel
    {
        public int WorkerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Skill { get; set; }
        public bool IsActive { get; set; }
        public decimal? DisplayRate { get; set; }
        public int CompletedJobs { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
