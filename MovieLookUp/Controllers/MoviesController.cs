using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieLookUp.Data;
using MovieLookUp.DTOs;
using MovieLookUp.Models;
using MovieLookUp.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace MovieLookUp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey = "887d56c3";

        public MoviesController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MovieReadDto>>> GetAllMovies()
        {
            var movies = await _context.Movies.ToListAsync();

            var movieDtos = movies.Select(m => new MovieReadDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Rating = m.Rating,
                ReleaseDate = m.ReleaseDate
            });

            return Ok(movieDtos);
        }

        [HttpGet("{id}", Name = "GetMovieById")]
        public async Task<ActionResult<MovieReadDto>> GetMovieById(int id)
        {
            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
            {
                return NotFound();
            }

            return Ok(new MovieReadDto
            {
                Id = movie.Id,
                Title = movie.Title,
                Description = movie.Description,
                Rating = movie.Rating,
                ReleaseDate = movie.ReleaseDate
            });
        }

        [HttpPost]
        public async Task<ActionResult<MovieReadDto>> CreateMovie([FromBody] MovieCreateDto movieDto)
        {
            var movieModel = new Movie
            {
                Title = movieDto.Title,
                Description = movieDto.Description,
                Rating = movieDto.Rating,
                ReleaseDate = movieDto.ReleaseDate
            };

            _context.Movies.Add(movieModel);
            await _context.SaveChangesAsync();

            var movieReadDto = new MovieReadDto
            {
                Id = movieModel.Id,
                Title = movieModel.Title,
                Description = movieModel.Description,
                Rating = movieModel.Rating,
                ReleaseDate = movieModel.ReleaseDate
            };

            return CreatedAtRoute(nameof(GetMovieById), new { id = movieReadDto.Id }, movieReadDto);
        }

        [HttpPost("import")]
        public async Task<ActionResult<MovieReadDto>> ImportMovieFromExternalApi([FromBody] MovieSearchDto searchDto)
        {
            var client = _httpClientFactory.CreateClient();

            var omdbUrl = $"http://www.omdbapi.com/?t={Uri.EscapeDataString(searchDto.Title)}&apikey={_apiKey}";
            var omdbData = await client.GetFromJsonAsync<OmdbMovieResponse>(omdbUrl);

            if (omdbData == null || omdbData.Response == "False")
            {
                return NotFound("Filmen hittades inte i OMDb.");
            }

            var tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(searchDto.Title)}";

            var tmdbToken = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiIyNzIzZmQyNzhkNmQ1Y2RjZTE3NWM0MTgzYWE3OTE3ZSIsIm5iZiI6MTc3MzYwMzQ1My43MzksInN1YiI6IjY5YjcwYTdkNTY1NGI0NzM0MzVhNzMwNyIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.rkW40Be2-LVBz2XzQxax42xn3tUSlZK44cwBgPtZ8z0";

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tmdbToken);

            var tmdbResponse = await client.GetAsync(tmdbUrl);
            string tmdbRating = "N/A";

            if (tmdbResponse.IsSuccessStatusCode)
            {
                var tmdbJson = await tmdbResponse.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(tmdbJson);
                var results = document.RootElement.GetProperty("results");

                if (results.GetArrayLength() > 0)
                {
                    var firstResult = results[0];
                    if (firstResult.TryGetProperty("vote_average", out var voteAverage))
                    {
                        tmdbRating = voteAverage.GetDouble().ToString();
                    }
                }
            }

            var movieModel = new Movie
            {
                Title = omdbData.Title,
                Description = omdbData.Plot,
                ReleaseDate = omdbData.Year,
                Rating = $"IMDB: {omdbData.imdbRating} | TMDB: {tmdbRating}"
            };

            _context.Movies.Add(movieModel);
            await _context.SaveChangesAsync();

            var movieReadDto = new MovieReadDto
            {
                Id = movieModel.Id,
                Title = movieModel.Title,
                Description = movieModel.Description,
                Rating = movieModel.Rating,
                ReleaseDate = movieModel.ReleaseDate
            };

            return CreatedAtRoute(nameof(GetMovieById), new { id = movieReadDto.Id }, movieReadDto);
        }
    }
}