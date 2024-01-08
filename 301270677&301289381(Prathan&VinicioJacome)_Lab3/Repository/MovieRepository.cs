using _301270677_301289381_Prathan_VinicioJacome__Lab3.Connector;
using _301270677_301289381_Prathan_VinicioJacome__Lab3.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using System.Xml.Linq;

namespace _301270677_301289381_Prathan_VinicioJacome__Lab3.Repository
{
    public class MovieRepository: IMovieRepository
    {
        private readonly AWSConnector _awsConnector;
        private readonly string _tableName = "Movie";
        private readonly Table _table;

        public MovieRepository(AWSConnector awsConnector)
        {
            _awsConnector = awsConnector;
            _table = _awsConnector.LoadContentTable(_tableName);
        }
        public async Task<List<Movie>> GetMoviesAsync()
        {
            DynamoDBContext context = _awsConnector.Context;
            var movies = await context.ScanAsync<Movie>(new List<ScanCondition>()).GetRemainingAsync();
            return movies;
        }
        public async Task AddMovieAsync(Movie movie)
        {
            DateTime currentDate = DateTime.Now;
            string formattedDate = currentDate.ToString("yyyy-MM-dd");

            var document = new Document();
            document["MovieID"] = Guid.NewGuid().ToString("N");
            document["ReleaseDate"] = formattedDate;
            document["MovieS3Url"] = movie.MovieS3Url;
            document["ImageS3Url"] = movie.ImageS3Url;
            document["Title"] = movie.Title;
            document["Genre"] = movie.Genre;
            // Convert and Save Directors as DynamoDBList
            var directorsList = new DynamoDBList();
            foreach (var director in movie.Directors)
            {
                directorsList.Add(director);
            }
            document["Directors"] = directorsList;

            // Convert and Save Actors as DynamoDBList
            var actorsList = new DynamoDBList();
            foreach (var actor in movie.Actors)
            {
                actorsList.Add(actor);
            }
            document["Actors"] = actorsList;
            document["Description"] = movie.Description;
            document["UserID"] = movie.UserID;
            document["AverageRating"] = 0;

            await _table.PutItemAsync(document);
        }

        public async Task DeleteMovieAsync(string movieId)
        {
            var deleteItemSpec = new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
        {
            {"MovieID", new AttributeValue { S = movieId }}
        }
            };

            try
            {
                await _awsConnector.DynamoClient.DeleteItemAsync(deleteItemSpec);
            }
            catch (AmazonDynamoDBException ex) 
            {
                throw new Exception($"DynamoDB error: {ex.ErrorCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"General error: {ex.Message}", ex);
            }
        }

        public async Task<Movie> GetMovieByIdAsync(string movieId)
        {
            var getItemSpec = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
        {
            {"MovieID", new AttributeValue { S = movieId }}
        }
            };

            var result = await _awsConnector.DynamoClient.GetItemAsync(getItemSpec);

            if (result.Item == null || !result.IsItemSet)
            {
                return null; // or throw an exception if you prefer
            }

            return DocumentToMovie(Document.FromAttributeMap(result.Item));

        }

        private Movie DocumentToMovie(Document document)
        {
            // Convert the Document to a Movie object
            var movie = new Movie
            {
                MovieID = document["MovieID"].AsString(),
                Title = document["Title"].AsString(),
                Genre = document["Genre"].AsString(),
                MovieS3Url = document["MovieS3Url"].AsString(),
                ImageS3Url = document["ImageS3Url"].AsString(),
                Description = document["Description"].AsString(),
                UserID = document["UserID"].AsString(),

            };
            if (document.ContainsKey("AverageRating"))
            {
                movie.AverageRating = document["AverageRating"].AsInt();
            }

            if (document.ContainsKey("Ratings") && document["Ratings"].AsListOfDocument().Any())
            {
                movie.Ratings = document["Ratings"]
                    .AsListOfDocument()
                    .Select(doc => new Rating
                    {
                        UserID = doc["UserID"].AsString(),
                        RateValue = doc["RateValue"].AsInt(),
                        RatingDateTime = DateTime.Parse(doc["RatingDateTime"].AsString())
                    })
                    .ToList();
            }

            if (document.ContainsKey("Comments") && document["Comments"].AsListOfDocument().Any())
            {
                movie.Comments = document["Comments"]
                    .AsListOfDocument()
                    .Select(doc => new Comment
                    {
                        UserID = doc["UserID"].AsString(),
                        Message = doc["Message"].AsString(),
                        CommentDateTime = DateTime.Parse(doc["CommentDateTime"].AsString())
                    })
                    .ToList();
            }


            // Convert the Directors from Document list to List<string>
            // Check if the Directors key exists in the document
            if (document.ContainsKey("Directors"))
            {
                try
                {
                    // Cast the Directors value to DynamoDBList
                    var directorsDynamoList = document["Directors"] as DynamoDBList;
                    if (directorsDynamoList != null)
                    {
                        // Ensure movie.Directors is initialized
                        if (movie.Directors == null)
                        {
                            movie.Directors = new List<string>();
                        }

                        // Clear any existing data in movie.Directors if necessary
                        movie.Directors.Clear();

                        foreach (var directorEntry in directorsDynamoList.Entries)
                        {
                            var directorString = directorEntry as Primitive;
                            if (directorString != null)
                            {
                                movie.Directors.Add(directorString.AsString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    // Handle the error if it's not a list or any other issues
                }
            }



            if (document.ContainsKey("Actors"))
            {
                try
                {
                    // Cast the Actors value to DynamoDBList
                    var actorsDynamoList = document["Actors"] as DynamoDBList;
                    if (actorsDynamoList != null)
                    {
                        // Ensure movie.Actors is initialized
                        if (movie.Actors == null)
                        {
                            movie.Actors = new List<string>();
                        }

                        // Clear any existing data in movie.Actors if necessary
                        movie.Actors.Clear();

                        foreach (var actorEntry in actorsDynamoList.Entries)
                        {
                            var actorString = actorEntry as Primitive;
                            if (actorString != null)
                            {
                                movie.Actors.Add(actorString.AsString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    // Handle the error if it's not a list or any other issues
                }
            }


            return movie;
        }


        public async Task UpdateMovieAsync(Movie movie)
        {            
            var document = new Document();
            document["MovieID"] = movie.MovieID;
            document["MovieS3Url"] = movie.MovieS3Url;
            document["ImageS3Url"] = movie.ImageS3Url;
            document["Title"] = movie.Title;
            document["Genre"] = movie.Genre;

            // Update Directors
            if (movie.Directors != null)  
            {
                var directorsList = new DynamoDBList();
                foreach (var director in movie.Directors)
                {
                    directorsList.Add(director);
                }
                document["Directors"] = directorsList;
            }

            // Update Actors
            if (movie.Actors != null)  
            {
                var actorsList = new DynamoDBList();
                foreach (var actor in movie.Actors)
                {
                    actorsList.Add(actor);
                }
                document["Actors"] = actorsList;
            }


            document["Description"] = movie.Description;
            document["UserID"] = movie.UserID;

            await _table.PutItemAsync(document);
        }

   
        public async Task<List<Movie>> GetMoviesByMinRatingAsync(double minRating)
        {
            var context = _awsConnector.Context;

            var conditions = new List<ScanCondition>
    {
        new ScanCondition("AverageRating", ScanOperator.GreaterThanOrEqual, minRating)
    };

            var searchResults = await context.QueryAsync<Movie>(conditions, new DynamoDBOperationConfig
            {
                IndexName = "MovieRating-index", // Specify the GSI name here            
            }).GetRemainingAsync();

            return searchResults.ToList();
        }

        public async Task<List<Movie>> GetMoviesByGenreAsync(string genre)
        {
            var context = _awsConnector.Context;

            var conditions = new List<ScanCondition>
    {
        new ScanCondition("Genre", ScanOperator.Equal, genre)
    };

            var searchResults = await context.QueryAsync<Movie>(conditions, new DynamoDBOperationConfig
            {
                IndexName = "MovieGenre-index", // Specify the GSI name here
                OverrideTableName = "Movie" 
            }).GetRemainingAsync();

            return searchResults.ToList();
        }


        public async Task SaveMovieAsync(Movie movie)
        {
            var request = new UpdateItemRequest
            {
                TableName = _tableName, 
                Key = new Dictionary<string, AttributeValue>
        {
            { "MovieID", new AttributeValue { S = movie.MovieID } }
            // Add sort key here if you have one
        },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>()
            };

            if (movie.Ratings != null)
            {
                var ratingsList = movie.Ratings.Select(rating => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
            {
                { "UserID", new AttributeValue { S = rating.UserID } },
                { "RateValue", new AttributeValue { N = rating.RateValue.ToString() } },
                { "RatingDateTime", new AttributeValue { S = rating.RatingDateTime.ToString() } }
            }
                }).ToList();
                request.AttributeUpdates["Ratings"] = new AttributeValueUpdate { Value = new AttributeValue { L = ratingsList }, Action = "PUT" };
            }

            // Update comments
            if (movie.Comments != null)
            {
                var commentsList = movie.Comments.Select(comment => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
            {
                { "UserID", new AttributeValue { S = comment.UserID } },
                { "Message", new AttributeValue { S = comment.Message } },
                { "CommentDateTime", new AttributeValue { S = comment.CommentDateTime.ToString() } }
            }
                }).ToList();
                request.AttributeUpdates["Comments"] = new AttributeValueUpdate { Value = new AttributeValue { L = commentsList }, Action = "PUT" };
            }

            // Updating the AverageRating attribute
            request.AttributeUpdates["AverageRating"] = new AttributeValueUpdate { Value = new AttributeValue { N = movie.AverageRating.ToString() }, Action = "PUT" };

            await _awsConnector.DynamoClient.UpdateItemAsync(request);
        }


        public Task<List<string>> GetGenresAsync()
        {
            return Task.FromResult(new List<string>
            {
                "Action",
                "Adventure",
                "Animation",
                "Comedy",
                "Crime",
                "Documentary",
                "Drama",
                "Fantasy",
                "History",
                "Horror",
                "Sci-Fi",
                "Sport",
                "Thriller"
            });
        }

    }
}
