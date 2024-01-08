using _301270677_301289381_Prathan_VinicioJacome__Lab3.Connector;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Models;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Mvc;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Service;
using Amazon.S3.Model;
using Amazon.S3;

namespace _301270677_301289381_Prathan_VinicioJacome__Lab3.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        public MovieController(IMovieService movieService)
        {
            _movieService = movieService;
        }
        public async Task<IActionResult> Index()
        {
            //var s3Client = _aWSConnector.S3Client;
            if (HttpContext.Session.GetString("UserId") != null)
            {
                List<Movie> movies = await _movieService.GetMoviesAsync();
                return View(movies);
            }
            else
            {
                return RedirectToAction("Login","User");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddMovie()
        {

            if (HttpContext.Session.GetString("UserId") != null)
            {
                ViewBag.Genres = await _movieService.GetGenresAsync();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddMovie(MovieViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    Console.WriteLine("Model is invalid!!!!");
            //    var errors = ModelState.SelectMany(x => x.Value.Errors.Select(p => p.ErrorMessage)).ToList();
            //    return View(model);
            //}

            model.UserID = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(model.UserID))
            {
                ModelState.AddModelError("UserID", "User ID is required.");
               
                return View(model);
            }

            try
            {
                await _movieService.AddMovieAsync(model);
                return RedirectToAction("Index");
            }
            catch (Exception ex)  // General exception catch block should be last
            {
               
                ModelState.AddModelError(string.Empty, "An unknown error occurred while adding the movie.");
                return View(model);
            }

           
        }

        [HttpDelete]

        public async Task<IActionResult> Delete(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                await _movieService.DeleteMovieAsync(id);
                return Json(new { success = true, message = "Movie deleted successfully" });
            }
            catch (AmazonDynamoDBException ex)
            {
                // AWS DynamoDB specific error handling
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // General error handling
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "An error occurred while deleting the movie." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditMovie(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login"); 
            }

            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                Console.WriteLine($"MovieID: {movie.MovieID}, Directors count: {movie.Directors?.Count ?? 0}");

                var model = new MovieViewModel 
                {
                    MovieID = movie.MovieID,
                    Title = movie.Title,
                    Genre = movie.Genre,
                    Description = movie.Description,
                    Directors = movie.Directors ?? new List<string>(),
                    Actors = movie.Actors ?? new List<string>()

                };
                    //get genre for dropdown
                List<string> genres = await _movieService.GetGenresAsync();
                ViewBag.Genres = genres;


                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error retrieving movie details.");
                return View(new MovieViewModel()); // Return an empty model to the view if there's an error
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditMovie(MovieViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    return View(model);
            //}

            model.UserID = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(model.UserID))
            {
                ModelState.AddModelError("UserID", "User ID is required.");
                return View(model);
            }

            try
            {
                await _movieService.UpdateMovieAsync(model); 
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unknown error occurred while updating the movie.");
                return View(model);
            }
        }

        //Show details of the movie
        [HttpGet]
        public async Task<IActionResult> ShowDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Movie ID is required.");
            }

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                Movie movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                {
                    return NotFound("Movie not found.");
                }
                return View(movie);
            }
            catch (Exception ex)
            {
                // You might want to log this exception for debugging.
                ModelState.AddModelError(string.Empty, "Error retrieving movie details.");
                return View("Error"); // Assuming you have an error view, otherwise handle it accordingly.
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListMovieRating(double? minRating = null)
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                List<Movie> movies = await _movieService.GetMoviesByRatingAsync(minRating);
                return View(movies);
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListMovieGenre(string genre)
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                // Filter movies by genre if a genre parameter is provided
                List<Movie> movies = await _movieService.GetMoviesByGenreAsync(genre);
                //get genre for dropdown
                List<string> genres = await _movieService.GetGenresAsync();
                ViewBag.Genres = genres;

                return View(movies);
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(string movieId, string message)
        {
            // Create the new comment
            var comment = new Comment
            {
                UserID = HttpContext.Session.GetString("UserId"),
                Message = message,
                CommentDateTime = DateTime.Now
            };

            try
            {
                await _movieService.AddCommentToMovieAsync(movieId, comment);
                return Ok();
            }
            catch (Exception ex)
            {
                // Handle error appropriately, for simplicity I'm just returning a BadRequest here
                return BadRequest(ex.Message);
            }
        }




        [HttpPost]
        public async Task<IActionResult> EditComment(string movieId, string userId, string originalMessage, string editedMessage)
        {
            try
            {
                // Fetch the movie from DynamoDB and locate the specific comment
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                    return NotFound();

                var comment = movie.Comments.FirstOrDefault(c => c.UserID == userId && c.Message == originalMessage);

                if (comment != null)
                {
                    // Update the comment's message
                    comment.Message = editedMessage;
                    comment.CommentDateTime = DateTime.Now;

                    // Save the movie with the edited comment back to DynamoDB
                    await _movieService.EditCommentMovieAsync(movie);

                    // Return a success response
                    return Ok();
                }
                else
                {
                    // Comment not found
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                return BadRequest("Failed to edit the comment: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAverageRating(string movieId, string userId, int userRating)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                    return NotFound("Movie not found.");

   
                // If Ratings was null, initialize it
                if (movie.Ratings == null)
                {
                    movie.Ratings = new List<Rating>();
                    var ratings = new Rating
                    {
                        UserID = userId,
                        RateValue = userRating,
                        RatingDateTime = DateTime.Now
                    };
                    movie.Ratings.Add(ratings);
                }

                var existingRating = movie.Ratings.FirstOrDefault(r => r.UserID == userId);

                if (existingRating != null)
                {
                    existingRating.RateValue = userRating;
                    existingRating.RatingDateTime = DateTime.Now;
                }
                else
                {
                    movie.Ratings.Add(new Rating { UserID = userId, RateValue = userRating, RatingDateTime = DateTime.Now });
                }
                var totalRatingValue = movie.Ratings.Sum(r => r.RateValue);
                var totalRatingsCount = movie.Ratings.Count();
                movie.AverageRating = totalRatingValue / totalRatingsCount;

                await _movieService.EditCommentMovieAsync(movie);

    

                return Ok(); // Return success
            }
            catch (Exception ex)
            {
                // Handle any errors
                return BadRequest($"Failed to update the rating: {ex.Message}");
            }
        }



    }
}
