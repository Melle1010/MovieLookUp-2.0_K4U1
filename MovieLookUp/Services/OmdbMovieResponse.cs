namespace MovieLookUp.Services
{
    public class OmdbMovieResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string Plot { get; set; } = string.Empty;
        public string imdbRating { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty; // "True" or "False"
    }
}
