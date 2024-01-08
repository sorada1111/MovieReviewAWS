using _301270677_301289381_Prathan_VinicioJacome__Lab3.Models;

namespace _301270677_301289381_Prathan_VinicioJacome__Lab3.Service
{
    public interface IMovieService
    {
        Task<List<Movie>> GetMoviesAsync();
        Task AddMovieAsync(MovieViewModel model);
        Task<string> UploadFileAsync(IFormFile file);

        Task DeleteMovieAsync(string movieId);
        Task<Movie> GetMovieByIdAsync(string movieId);
        Task UpdateMovieAsync(MovieViewModel model);

        Task<List<Movie>> GetMoviesByRatingAsync(double? minRating = null);

        Task<List<Movie>> GetMoviesByGenreAsync(string genre);
        Task AddCommentToMovieAsync(string movieId, Comment comment);
        Task EditCommentMovieAsync(Movie movie);
        Task<List<string>> GetGenresAsync();


    }
}
