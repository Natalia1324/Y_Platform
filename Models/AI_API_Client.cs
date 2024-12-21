using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Y_Platform.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using static Y_Platform.Models.AI_API_Client;

namespace Y_Platform.Models
{
    public class AI_API_Client
    {
        /// <summary>
        /// Klasa umożliwiająca korzystanie z funkcji API 
        /// </summary>
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiUrl;
        private ApplicationDbContext _context;

        public AI_API_Client(string apiUrl, ApplicationDbContext _context)
        {
            this.apiUrl = apiUrl;
            this._context = _context;
        }

        public class InputUserAggression
        {
            /// <summary>
            /// Klasa reprezentująca dane wejściowe funkcji /usersbyvalue
            /// </summary>
            public string apiKey { get; set; }
        }
        public class InputDataToPrediction
        {
            /// <summary>
            /// Klasa reprezentująca dane wejściowe funkcji /predict
            /// </summary>
            public int User_ID { get; set; }
            public string User_Name { get; set; }
            public string Content { get; set; }
            public DateTime CreatedDate { get; set; }
            public string apiKey { get; set; }
        }
        public class PostDataToLearning
        {
            /// <summary>
            /// Klasa reprezentująca dane wejściowe funkcji /learn
            /// </summary>
            public int User_ID { get; set; }
            public string User_Name { get; set; }
            public string Content { get; set; }
            public DateTime CreatedDate { get; set; }
            public int NotOffensive { get; set; }
            public int Offensive { get; set; }
            public string apiKey { get; set; }

        }
        public class Prediction
        {
            /// <summary>
            /// Klasa reprezentująca encje PredictionResponse
            /// </summary>
            public string text { get; set; }        
            public int classification { get; set; }  
            public float predictionValue { get; set; } 
        }
        public class PredictionResponse
        {
            /// <summary>
            /// Klasa reprezentująca odpowiedź funkcji /predict
            /// </summary>
            public List<List<object>> predictions { get; set; }
            public string message { get; set; }  // Informacja zwrotna od API (czy zapisano post)
        }
        public class ErrorResponse
        {
            /// <summary>
            /// Klasa reprezentująca odpowiedź błędu
            /// </summary>
            public string Detail { get; set; }
        }
        public class UserData
        {
            /// <summary>
            /// Klasa reprezentująca pojedyńczą encje danych użytkownika
            /// </summary>
            public int Id { get; set; }
            public string Nick { get; set; }
        }
        public class UserResponse
        {
            /// <summary>
            /// Klasa reprezentująca odpowiedź funkcji /usersbyvalue
            /// </summary>
            public List<List<object>> userData { get; set; }
            public string message { get; set; }
        }

        public async Task<List<Prediction>> GetPrediction(int userId, string userName, string content, DateTime createdDate, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty", nameof(content));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty", nameof(apiKey));

            var input = new InputDataToPrediction
            {
                User_ID = userId,
                User_Name = userName,
                Content = content,
                CreatedDate = createdDate,
                apiKey = apiKey
            };

            var jsonContent = JsonConvert.SerializeObject(input);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // Wysłanie żądania POST do API
                var response = await client.PostAsync($"{apiUrl}/predict", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var predictionResponse = JsonConvert.DeserializeObject<PredictionResponse>(responseContent);

                    var predictions = new List<Prediction>();
                    foreach (var pred in predictionResponse.predictions)
                    {
                        if (pred.Count == 3)
                        {
                            var prediction = new Prediction
                            {
                                text = pred[0].ToString(),
                                classification = Convert.ToInt32(pred[1]),
                                predictionValue = Convert.ToSingle(pred[2])
                            };
                            predictions.Add(prediction);
                        }
                    }

                    Console.WriteLine($"Message from API: {predictionResponse.message}");  // Wyświetlamy wiadomość zwrotną od API

                    return predictions;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw API error response: {errorMessage}");

                    try
                    {
                        var errorDetail = JsonConvert.DeserializeObject<ErrorResponse>(errorMessage);
                        throw new HttpRequestException($"API request failed with status {response.StatusCode}: {errorDetail.Detail}");
                    }
                    catch (JsonException)
                    {
                        throw new HttpRequestException($"API request failed with status {response.StatusCode}: {errorMessage}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }


        public async Task SendLearningData(string apiKey)
        {
            try
            {
                // Pobierz wszystkie posty z bazy danych
                var posts = await _context.Posts
                    .Include(p => p.Users)  // Include, aby mieć dostęp do UserId
                    .ToListAsync();

                foreach (var post in posts)
                {
                    // Obliczamy liczbę głosów na podstawie tabeli PostVotes
                    var offensiveVotes = _context.PostVotes.Count(v => v.Post.Id == post.Id && v.IsOffensive);
                    var notOffensiveVotes = _context.PostVotes.Count(v => v.Post.Id == post.Id && !v.IsOffensive);

                    var postData = new PostDataToLearning
                    {
                        User_ID = post.Users.Id, // Przesyłamy ID użytkownika
                        User_Name = post.Users.Nick,
                        Content = post.Content,
                        CreatedDate = post.CreatedDate,
                        NotOffensive = notOffensiveVotes, // Liczba głosów "Not Offensive"
                        Offensive = offensiveVotes,       // Liczba głosów "Offensive"
                        apiKey = apiKey,
                    };

                    var jsonContent = JsonConvert.SerializeObject(postData);

                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Wyślij dane do API
                    var response = await client.PostAsync($"{apiUrl}/learn", httpContent);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Post {post.Id} sent successfully.");
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Failed to send post {post.Id}. Error: {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserData>> GetMostAgressiveUsersByTimeAndValue(string apiKey)
        {
            try
            {
                var inputdata = new InputUserAggression
                {
                    apiKey = apiKey,
                };
                var jsonContent = JsonConvert.SerializeObject(inputdata);

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Wyślij dane do API
                var response = await client.PostAsync($"{apiUrl}/usersbyvalue", httpContent);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonConvert.DeserializeObject<UserResponse>(responseContent);

                    var users = new List<UserData>();
                    if (userResponse != null)
                    {
                        foreach (var user in userResponse.userData)
                        {
                            if (user.Count == 2)
                            {
                                var user1 = new UserData
                                {
                                    Id = Convert.ToInt32(user[0]),
                                    Nick = user[1].ToString(),
                                };

                                users.Add(user1);
                            }
                        }
                        foreach (var user in users)
                        {
                            Console.WriteLine(user.Id);
                            Console.WriteLine(user.Nick);
                        }
                        
                    }
                    return users;
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to retrieve. Error: {errorMessage}");
                }
                return [];

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

    
    }
 }

