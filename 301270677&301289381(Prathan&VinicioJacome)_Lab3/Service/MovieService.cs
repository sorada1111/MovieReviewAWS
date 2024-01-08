using _301270677_301289381_Prathan_VinicioJacome__Lab3.Connector;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Models;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Repository;
using Amazon.S3.Transfer;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;

namespace _301270677_301289381_Prathan_VinicioJacome__Lab3.Service
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _movieRepository;
        private readonly AWSConnector _awsConnector;
        private readonly String bucketName = "assignment2movie";

        public MovieService(IMovieRepository movieRepository, AWSConnector awsConnector)
        {
            _movieRepository = movieRepository;
            _awsConnector = awsConnector;
        }

        public async Task<List<Movie>> GetMoviesAsync()
        {
            return await _movieRepository.GetMoviesAsync();
        }
        public async Task AddMovieAsync(MovieViewModel model)
        {
            try
            {


                if (model.MovieFile != null & model.ImageFile != null)
                {
                    var movieS3Url = await UploadFileAsync(model.MovieFile);
                    var imageS3Url = await UploadFileAsync(model.ImageFile);
                    model.MovieFileUrl = movieS3Url;
                    model.ImageFileUrl = imageS3Url;

                }

                // Map the ViewModelt to the Domain Model
                var movie = new Movie
                {
                    MovieID = model.MovieID,
                    Title = model.Title,
                    Genre = model.Genre,
                    Description = model.Description,
                    Directors = model.Directors,
                    Actors = model.Actors,
                    MovieS3Url = model.MovieFileUrl,
                    ImageS3Url = model.ImageFileUrl,
                    UserID = model.UserID,
                    ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd")
                };


                await _movieRepository.AddMovieAsync(movie);

            }catch (Exception ex) 
            {
                throw;
            }
        }

      
        public async Task<string> UploadFileAsync(IFormFile file)
        {

            var fileName = Path.GetFileName(file.FileName);
            var key = fileName;
                //+ Path.GetExtension(file.FileName);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = file.OpenReadStream(),
                Key = key,
                BucketName = bucketName,
                CannedACL = S3CannedACL.PublicRead
            };

            var fileTransferUtility = new TransferUtility(_awsConnector.S3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);
            return $"https://{bucketName}.s3.amazonaws.com/{key}";   
        }
        public async Task DeleteMovieAsync(string movieId)
        {
            try
            {
                // Retrieve the movie by ID
                var movie = await _movieRepository.GetMovieByIdAsync(movieId);

                if (movie == null)
                {
                    throw new Exception("Movie not found!");
                }

                // Delete from S3
                await DeleteFileFromS3Async(movie.MovieS3Url);
                await DeleteFileFromS3Async(movie.ImageS3Url);

                // Delete from DynamoDB
                await _movieRepository.DeleteMovieAsync(movieId);
            }
            catch (AmazonS3Exception s3Ex)
            {
                // Handle S3 exceptions, check if it's a permission issue
                if (s3Ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Exception($"S3 Exception. {s3Ex.Message}", s3Ex);
                }
                throw; // Re-throw the original exception if it's not a Forbidden status
            }
            catch (AmazonDynamoDBException dbEx)
            {
                // Handle DynamoDB exceptions, check if it's a permission issue
                if (dbEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Exception($"DynamoDB Exception. {dbEx.Message}", dbEx);
                }
                throw; // Re-throw the original exception if it's not a Forbidden status
            }
            catch (Exception ex)
            {
                // General exception handling
                throw new Exception($"An error occurred: {ex.Message}", ex);
            }
        }


        public async Task DeleteFileFromS3Async(string fileUrl)
        {
            var key = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            await _awsConnector.S3Client.DeleteObjectAsync(deleteObjectRequest);
        }

        public async Task<Movie> GetMovieByIdAsync(string movieId)
        {
            return await _movieRepository.GetMovieByIdAsync(movieId);
        }
        public async Task UpdateMovieAsync(MovieViewModel model)
        {
            try
            {
                var existingMovie = await _movieRepository.GetMovieByIdAsync(model.MovieID);
                if (existingMovie == null)
                {
                    throw new Exception("Movie not found!");
                }

                if (model.MovieFile != null)
                {
                    // Delete the old movie file from S3
                    await DeleteFileFromS3Async(existingMovie.MovieS3Url);
            
                    // Upload the new movie file to S3
                    var movieS3Url = await UploadFileAsync(model.MovieFile);
                    model.MovieFileUrl = movieS3Url;
                }

                if (model.ImageFile != null)
                {
                    // Delete the old image file from S3
                    await DeleteFileFromS3Async(existingMovie.ImageS3Url);

                    // Upload the new image file to S3
                    var imageS3Url = await UploadFileAsync(model.ImageFile);
                    model.ImageFileUrl = imageS3Url;
                }

                // Map the ViewModel to the Domain Model
                var movieToUpdate = new Movie
                {
                    MovieID = model.MovieID,
                    Title = model.Title,
                    Genre = model.Genre,
                    Description = model.Description,
                    Directors = model.Directors,
                    Actors = model.Actors,
                    MovieS3Url = model.MovieFileUrl ?? existingMovie.MovieS3Url, 
                    ImageS3Url = model.ImageFileUrl ?? existingMovie.ImageS3Url, 
                    UserID = model.UserID,
                    ReleaseDate = existingMovie.ReleaseDate 
                };

                await _movieRepository.UpdateMovieAsync(movieToUpdate); 
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
            public async Task<List<Movie>> GetMoviesByRatingAsync(double? minRating = null)
        {
            if (minRating.HasValue)
            {
                return await _movieRepository.GetMoviesByMinRatingAsync(minRating.Value);
            }
            else
            {
                return await _movieRepository.GetMoviesAsync();
            }
        }


        public async Task<List<Movie>> GetMoviesByGenreAsync(string genre)
        {
            if (!string.IsNullOrWhiteSpace(genre))
            {
                return await _movieRepository.GetMoviesByGenreAsync(genre);
            }
            else
            {
                return await _movieRepository.GetMoviesAsync();
            }
        }


        public async Task AddCommentToMovieAsync(string movieId, Comment comment)
        {
            var movie = await _movieRepository.GetMovieByIdAsync(movieId);
            if (movie == null)
                throw new Exception("Movie not found");

            movie.Comments = movie.Comments ?? new List<Comment>();
            movie.Comments.Add(comment);

            await _movieRepository.SaveMovieAsync(movie);
        }

        public async Task EditCommentMovieAsync(Movie movie) 
        {
            
            await _movieRepository.SaveMovieAsync(movie);
        }

        public async Task<List<string>> GetGenresAsync()
        {
            return await _movieRepository.GetGenresAsync();
        }


    }
}
