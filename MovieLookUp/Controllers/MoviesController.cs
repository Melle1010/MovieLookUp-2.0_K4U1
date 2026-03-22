using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using MovieLookUp.Data;
using MovieLookUp.DTOs;
using MovieLookUp.Models;
using MovieLookUp.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace MovieLookUp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("Fixed")]
    public class MoviesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HybridCache _cache;

        public MoviesController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration, HybridCache cache)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<MovieReadDto>>> GetAllMovies(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            string cacheKey = $"movies_page_{page}_size_{pageSize}";

            var response = await _cache.GetOrCreateAsync(
                cacheKey,
                async cancelToken =>
                {
                    var query = _context.Movies.AsQueryable();
                    var totalItems = await query.CountAsync(cancelToken);
                    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                    var movies = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync(cancelToken);

                    var movieDtos = movies.Select(m => new MovieReadDto
                    {
                        Id = m.Id,
                        Title = m.Title,
                        Description = m.Description,
                        Rating = m.Rating,
                        ReleaseDate = m.ReleaseDate
                    }).ToList();

                    return new PagedResponse<MovieReadDto>
                    {
                        Items = movieDtos,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    };
                },
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(10),
                    LocalCacheExpiration = TimeSpan.FromMinutes(10)
                },
                tags: ["movies-list"]
            );

            return Ok(response);
        }

        [HttpGet("{id}", Name = "GetMovieById")]
        public async Task<ActionResult<MovieReadDto>> GetMovieById(int id)
        {
            //throw new KeyNotFoundException("Filmen du letade efter finns inte i databasen.");
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

            var omdbKey = _configuration["ApiKeys:OMDb"];
            var omdbUrl = $"http://www.omdbapi.com/?t={Uri.EscapeDataString(searchDto.Title)}&apikey={omdbKey}";
            var omdbData = await client.GetFromJsonAsync<OmdbMovieResponse>(omdbUrl);

            if (omdbData == null || omdbData.Response == "False")
            {
                return NotFound("Filmen hittades inte i OMDb.");
            }

            var tmdbUrl = $"https://api.themoviedb.org/3/search/movie?query={Uri.EscapeDataString(searchDto.Title)}";

            var tmdbToken = _configuration["ApiKeys:TMDB"];

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
            await _cache.RemoveByTagAsync("movies-list");

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