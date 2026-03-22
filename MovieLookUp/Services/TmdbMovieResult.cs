using System.Text.Json.Serialization;

namespace MovieLookUp.Services
{
    public class TmdbMovieResult
    {
        // TMDB's JSON key is "vote_average", so we map it to our C# property
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
    }
}
