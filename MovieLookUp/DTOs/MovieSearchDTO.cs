using System.ComponentModel.DataAnnotations;

namespace MovieLookUp.DTOs
{
    public class MovieSearchDto
    {
        [Required(ErrorMessage = "Du måste ange en titel att söka på.")]
        [MinLength(2, ErrorMessage = "Titeln måste vara minst 2 tecken lång.")]
        public string Title { get; set; } = string.Empty;
    }
}