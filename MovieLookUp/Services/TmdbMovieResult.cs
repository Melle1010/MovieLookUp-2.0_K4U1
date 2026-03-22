using System.Text.Json.Serialization;

namespace MovieLookUp.Services
{
    public class TmdbMovieResult
    {
        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
    }
}
