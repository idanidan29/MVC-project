namespace MVC_project.ViewModels
{
    public class RatingSubmission
    {
        public int tripId { get; set; }
        public byte rating { get; set; }
        public string? comment { get; set; }
    }
}
