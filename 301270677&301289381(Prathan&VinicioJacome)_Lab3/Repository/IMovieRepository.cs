using _301270677_301289381_Prathan_VinicioJacome__Lab3.Models;

namespace _301270677_301289381_Prathan_VinicioJacome__Lab3.Repository
{
    public interface IMovieRepository
    {
        Task<List<Movie>> GetMoviesAsync();
        Task AddMovieAsync(Movie movie);
        Task DeleteMovieAsync(string movieId);

        Task<Movie> GetMovieByIdAsync(string movieId);
        Task UpdateMovieAsync(Movie movie);

        Task<List<Movie>> GetMoviesByMinRatingAsync(double minRating);

        Task<List<Movie>> GetMoviesByGenreAsync(string genre);

        Task SaveMovieAsync(Movie movie);

        Task<List<string>> GetGenresAsync();

    }
}
