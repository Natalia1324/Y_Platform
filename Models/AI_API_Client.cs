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
            public string message { get; set; }
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

            /// <summary>
            /// Pobiera predykcję dotyczące treści wpisu na podstawie danych użytkownika i zawartości wpisu.
            /// </summary>
            /// <param name="userId">ID użytkownika</param>
            /// <param name="userName">Nazwa użytkownika</param>
            /// <param name="content">Treść wpisu</param>
            /// <param name="createdDate">Data utworzenia wpisu</param>
            /// <param name="apiKey">Klucz API</param>
            /// <returns>Lista predykcji dotyczących wpisu</returns>
            public async Task<List<Prediction>> GetPrediction(int userId, string userName, string content, DateTime createdDate, string apiKey)
            {
                if (string.IsNullOrWhiteSpace(content))
                    throw new ArgumentException("Treść nie może być pusta", nameof(content));

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new ArgumentException("Klucz API nie może być pusty", nameof(apiKey));

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

                        Console.WriteLine($"Komunikat API: {predictionResponse.message}");
                        return predictions;
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Niepowodzenie API: {errorMessage}");

                        try
                        {
                            var errorDetail = JsonConvert.DeserializeObject<ErrorResponse>(errorMessage);
                            throw new HttpRequestException($"Żądanie API nie powiodło się z kodem statusu {response.StatusCode}: {errorDetail.Detail}");
                        }
                        catch (JsonException)
                        {
                            throw new HttpRequestException($"Żądanie API nie powiodło się z kodem statusu {response.StatusCode}: {errorMessage}");
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Błąd żądania HTTP: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Nieoczekiwany błąd: {ex.Message}");
                    throw;
                }
            }

            /// <summary>
            /// Wysyła dane uczące do zewnętrznego API na podstawie zgromadzonych danych o postach i głosach użytkowników.
            /// </summary>
            /// <param name="apiKey">Klucz API</param>
            public async Task SendLearningData(string apiKey)
            {
                try
                {
                    var posts = await _context.Posts
                        .Include(p => p.Users)
                        .ToListAsync();

                    foreach (var post in posts)
                    {
                        var offensiveVotes = _context.PostVotes.Count(v => v.Post.Id == post.Id && v.IsOffensive);
                        var notOffensiveVotes = _context.PostVotes.Count(v => v.Post.Id == post.Id && !v.IsOffensive);

                        var postData = new PostDataToLearning
                        {
                            User_ID = post.Users.Id,
                            User_Name = post.Users.Nick,
                            Content = post.Content,
                            CreatedDate = post.CreatedDate,
                            NotOffensive = notOffensiveVotes,
                            Offensive = offensiveVotes,
                            apiKey = apiKey,
                        };

                        var jsonContent = JsonConvert.SerializeObject(postData);
                        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync($"{apiUrl}/learn", httpContent);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Post {post.Id} został pomyślnie wysłany.");
                        }
                        else
                        {
                            var errorMessage = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Nie udało się wysłać postu {post.Id}. Błąd: {errorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wystąpił błąd: {ex.Message}");
                    throw;
                }
            }

            /// <summary>
            /// Pobiera listę najbardziej agresywnych użytkowników na podstawie danych z API.
            /// </summary>
            /// <param name="apiKey">Klucz API</param>
            /// <returns>Lista danych użytkowników</returns>
            public async Task<List<UserData>> GetMostAggressiveUsers(string apiKey)
            {
                try
                {
                    var inputdata = new InputUserAggression
                    {
                        apiKey = apiKey,
                    };
                    var jsonContent = JsonConvert.SerializeObject(inputdata);

                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

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
                                Console.WriteLine($"ID: {user.Id}, Nick: {user.Nick}");
                            }

                        }
                        return users;
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Nie udało się pobrać danych użytkowników. Błąd: {errorMessage}");
                    }
                    return new List<UserData>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wystąpił błąd: {ex.Message}");
                    throw;
                }
            }
        }
    }


