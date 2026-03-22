namespace MovieLookUp.DTOs
{
    public class MovieReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
    }
}