﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Y_Platform.Entities;
using Y_Platform.Models;
using Microsoft.EntityFrameworkCore;
using static Y_Platform.Models.AI_API_Client;

namespace Y_Platform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private static List<Post> posts = new List<Post>();

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            if (!posts.Any())
            {
                LoadPostsFromDatabase();
            }
        }
        private void LoadPostsFromDatabase()
        {
            posts = _context.Posts
                .Include(p => p.Users) // Jeżeli chcesz dołączyć powiązanych użytkowników
                .ToList(); // Pobierz wszystkie posty z bazy
        }
        private Users getLoggedUser()
        {
            return HttpContext.Session.GetObject<Users>("LoggedUser");
        }
        public IActionResult Index()
        {
            var loggedUser = getLoggedUser();
            if (loggedUser == null)
            {
                // Przekierowanie do strony logowania, jeśli użytkownik nie jest zalogowany
                return RedirectToAction("Login");
            }

            // Pobierz wszystkie posty i przekaż do widoku
            return View(posts);
        }
        [HttpPost]
        public async Task<IActionResult> AddPost(string content)
        {
            var loggedUser = getLoggedUser();
            if (loggedUser == null)
            {
                // Przekierowanie do strony logowania, jeśli użytkownik nie jest zalogowany
                return RedirectToAction("Login");
            }
            var u = _context.Users
                .FirstOrDefault(u => u.Nick == loggedUser.Nick && u.Password == loggedUser.Password);
            var creationDate = DateTime.Now;
            var newPost = new Post
            {
                Content = content,
                CreatedDate = creationDate,
                Users = u,
                Prediction = await getPredict(u.Id, u.Nick, content, creationDate)
            };

            posts.Add(newPost);
            _context.Posts.Add(newPost);
            _context.SaveChanges();

            // Przekieruj na stronę główną po dodaniu posta
            return RedirectToAction("Index");
        }

        static async Task<float?> getPredict(int user_id, string user_name, string content, DateTime creationDate)
        {
            // Tworzymy nowego klienta dla API
            var api = new AI_API_Client("http://127.0.0.1:8000");
            string apiKey = "b390facc-6191-41a9-ada7-bb4f022e9153";
            try
            {
                // Wywołanie API i otrzymanie predykcji
                var predictions = await api.GetPrediction(user_id, user_name, content, creationDate, apiKey);

                // Wyświetlenie wyniku
                Console.WriteLine("Prediction:");
                if (predictions != null && predictions.Count > 0)
                {
                    foreach (var prediction in predictions)
                    {
                        // Wyświetlanie każdej predykcji z odpowiednimi polami
                        Console.WriteLine($"Text: {prediction.text}, Classification: {prediction.classification}, Predicted Value: {prediction.predictionValue}");
                        return prediction.predictionValue;

                    }
                    
                }
                else
                {
                    Console.WriteLine("No predictions found.");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }




        public IActionResult Privacy()
        {
            return View(getLoggedUser());
        }
        public IActionResult Login()
        {

            return View(); //zmiana

        }
        [HttpPost]
        public IActionResult Login(Users newUser)
        {
            var u = _context.Users
             .FirstOrDefault(u => u.Nick == newUser.Nick && u.Password == newUser.Password);

            if (u != null)
            {
                newUser.isLogged = true;
                var loggedUser = newUser;
                loggedUser.Id = u.Id;
                loggedUser.Login = u.Login;
                HttpContext.Session.SetObject("LoggedUser", loggedUser);
                return View("Index", posts);

            }
            else
            {
                //return View("Index", getLoggedUser());
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View();
            }
        }
        [HttpPost]
        [Route("/Home/Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("LoggedUser");
            return Ok();
        }
        public IActionResult Register()
        {
            return View(getLoggedUser());
        }
        [HttpPost]
        public IActionResult Register(Users newUser)
        {
            try
            {
                var existingUser = _context.Users
                    .FirstOrDefault(u => u.Nick == newUser.Nick || u.Login == newUser.Login);

                if (existingUser != null)
                {
                    // Logowanie komunikatu o błędzie do konsoli
                    Console.WriteLine("Użytkownik jest już zarejestrowany.");

                    // Dodanie komunikatu o błędzie do ModelState
                    ModelState.AddModelError(string.Empty, "Użytkownik jest już zarejestrowany.");
                    return View();
                }
                else
                {
                    // Ustawienie domyślnej rangi dla nowego użytkownika
                    newUser.isLogged = true;

                    // Dodanie nowego użytkownika do bazy danych
                    _context.Users.Add(newUser);
                    _context.SaveChanges();

                    // Logowanie komunikatu o sukcesie do konsoli
                    Console.WriteLine($"Użytkownik {newUser.Login} został pomyślnie zarejestrowany.");

                    // Ustawienie sesji dla zalogowanego użytkownika
                    HttpContext.Session.SetObject("LoggedUser", newUser);

                    // Przekierowanie do widoku "Index" z zalogowanym użytkownikiem
                    return View("Index", posts);
                }
            }
            catch (Exception ex)
            {
                // Logowanie błędów do konsoli
                Console.WriteLine("Wystąpił błąd podczas rejestracji użytkownika: " + ex.Message);

                // Dodanie komunikatu o błędzie do ModelState
                ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas rejestracji użytkownika. Proszę spróbować ponownie później.");
                return View();
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
