using System.ComponentModel.DataAnnotations;

namespace MovieLookUp.DTOs
{
    public class MovieCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Rating { get; set; } = string.Empty;

        [Required]
        public string ReleaseDate { get; set; } = string.Empty;
    }
}