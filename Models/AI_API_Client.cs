using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Y_Platform.Entities;
using Microsoft.EntityFrameworkCore;

namespace Y_Platform.Models
{
    public class AI_API_Client
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string apiUrl;
        private ApplicationDbContext _context;

        public AI_API_Client(string apiUrl, ApplicationDbContext _context)
        {
            this.apiUrl = apiUrl;
            this._context = _context;
        }

        // Klasa do danych wejściowych
        public class InputDataToPrediction
        {
            public int User_ID { get; set; }
            public string User_Name { get; set; }
            public string Content { get; set; }
            public DateTime CreatedDate { get; set; }
            public string apiKey { get; set; }  // Klucz autoryzacyjny
        }
        public class PostDataToLearning
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public DateTime CreatedDate { get; set; }
            public float? Prediction { get; set; }
            public int NotOffensive { get; set; }
            public int Offensive { get; set; }
            public int UserId { get; set; }  // Przesyłamy tylko ID użytkownika
        }
        public class Prediction
        {
            public string text { get; set; }         // Tekst
            public int classification { get; set; }  // Klasyfikacja
            public float predictionValue { get; set; } // Wartość predykcji
        }
        public class PredictionResponse
        {
            public List<List<object>> predictions { get; set; }
            public string message { get; set; }  // Informacja zwrotna od API (czy zapisano post)
        }
        public class ErrorResponse
        {
            public string Detail { get; set; }
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
                    // Wyświetlenie surowej odpowiedzi API w przypadku błędu
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw API error response: {errorMessage}");

                    // Deserializacja błędu
                    try
                    {
                        var errorDetail = JsonConvert.DeserializeObject<ErrorResponse>(errorMessage);
                        throw new HttpRequestException($"API request failed with status {response.StatusCode}: {errorDetail.Detail}");
                    }
                    catch (JsonException)
                    {
                        // Jeśli nie udało się zdeserializować, wyświetlamy surowy błąd
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
                    var postData = new PostDataToLearning
                    {
                        Id = post.Id,
                        Content = post.Content,
                        CreatedDate = post.CreatedDate,
                        Prediction = post.Prediction,
                        NotOffensive = post.NotOffensive,
                        Offensive = post.Offensive,
                        UserId = post.Users.Id  // Przesyłamy ID użytkownika
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
    }

}

