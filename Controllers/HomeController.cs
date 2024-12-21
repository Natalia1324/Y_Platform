using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Y_Platform.Entities;
using Y_Platform.Models;
using Microsoft.EntityFrameworkCore;
using static Y_Platform.Models.AI_API_Client;
using System.Security.Claims;

namespace Y_Platform.Controllers
{ 
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static ApplicationDbContext _context;
        private static List<Posts> posts = new List<Posts>();

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
            ///<summary>
            /// Ładuje posty z bazy danych do zmiennej posts.
            ///</summary>
            posts = _context.Posts
                .Include(p => p.Users)
                .ToList();
        }
        private Users getLoggedUser()
        {
            ///<summary>
            /// Zwraca zalogowanego użytkownika w sesji.
            /// </summary>
            return HttpContext.Session.GetObject<Users>("LoggedUser");
        }

        [HttpPost]
        public IActionResult Vote([FromBody] VoteRequest voteRequest)
        {
            ///<summary>
            /// Umożliwia zagłosowanie wobec obraźliwości posta.
            /// </summary>
            /// <param name="voteRequest"> Żądanie zawierające dane głosowania. </param>
            /// <returns> Informacja o statusie funkcji (OK, Unauthorized etc.) </returns>

            var loggedUser = getLoggedUser();
            if (loggedUser == null)
            {
                return Unauthorized("You must be logged in to vote.");
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Id == loggedUser.Id);

            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            var post = _context.Posts
                .FirstOrDefault(p => p.Id == voteRequest.PostId);

            if (post == null)
            {
                return NotFound("Post not found.");
            }

            var existingVote = _context.PostVotes
                .FirstOrDefault(v => v.Post.Id == voteRequest.PostId && v.User.Id == user.Id);

            if (existingVote != null)
            {
                if (existingVote.IsOffensive != voteRequest.IsOffensive)
                {
                    // Aktualizacja istniejącego głosu
                    existingVote.IsOffensive = voteRequest.IsOffensive;
                }
            }
            else
            {
                // Dodanie nowego głosu
                var vote = new PostVotes
                {
                    User = user,
                    Post = post,
                    IsOffensive = voteRequest.IsOffensive
                };

                _context.PostVotes.Add(vote);
            }

            _context.SaveChanges();

            LoadPostsFromDatabase();

            return Ok("Your vote has been submitted.");
        }
        public IActionResult Index()
        {
            ///<summary>
            /// Obsługa strony głównej, wyświetlającej i zapisującej posty
            /// </summary>
            /// <returns> Odpowiednie widoki </returns>
            var loggedUser = getLoggedUser();
            if (loggedUser == null)
            {
                return RedirectToAction("Login");
            }
            var currentUserId = loggedUser.Id;

            var posts = _context.Posts
                .Include(p => p.Users)
                .Include(p => p.PostVotes)
                .ToList();

            var postViewModels = posts.Select(post => new PostViewModel
            {
                Post = post,
                UserVote = post.PostVotes.FirstOrDefault(v => v.User.Id == currentUserId),
                OffensiveVotes = post.PostVotes.Count(v => v.IsOffensive),                 
                NotOffensiveVotes = post.PostVotes.Count(v => !v.IsOffensive)             
            }).ToList();

            return View(postViewModels);
        }

        [HttpPost]
        public async Task<IActionResult> AddPost(string content)
        {
            ///<summary>
            /// Funkcja umożliwiająca dodawanie postów
            /// </summary>
            /// <param name="content"> Treść posta </param>
            /// <returns> Odpowiedni widok </returns>
            
            var loggedUser = getLoggedUser();
            if (loggedUser == null)
            {
                return RedirectToAction("Login");
            }
            var u = _context.Users
                .FirstOrDefault(u => u.Nick == loggedUser.Nick && u.Password == loggedUser.Password);
            var creationDate = DateTime.Now;
            var newPost = new Posts
            {
                Content = content,
                CreatedDate = creationDate,
                Users = u,
                Prediction = await getPredict(u.Id, u.Nick, content, creationDate)
            };

            posts.Add(newPost);
            _context.Posts.Add(newPost);
            _context.SaveChanges();

            LoadPostsFromDatabase();

            return RedirectToAction("Index");
        }
        static async Task<float?> getPredict(int user_id, string user_name, string content, DateTime creationDate)
        {
            ///<summary>
            /// Funkcja pobierająca predykcje obraźliwości posta
            /// </summary>
            /// <param name="user_id"> ID użytkownika </param>
            /// <param name="user_name"> Nazwa użytkownika </param>
            /// <param name="content"> Treść posta </param>
            /// <param name="creationDate"> Data opublikowania posta </param>
            /// <returns> Wartość obraźliwości posta lub null w przypadku błędu </returns>
            
            var api = new AI_API_Client("http://127.0.0.1:8000", _context);
            string apiKey = "bf2dcaf1-8118-45ae-8466-327f52bed797";
            try
            {
                await api.SendLearningData(apiKey);
                var predictions = await api.GetPrediction(user_id, user_name, content, creationDate, apiKey);
                Console.WriteLine("Prediction:");
                if (predictions != null && predictions.Count > 0)
                {
                    foreach (var prediction in predictions)
                    {
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
            ///<summary>
            /// Widok Privacy
            /// </summary>
            return View(getLoggedUser());
        }
        public IActionResult Login()
        {
            ///<summary>
            /// Widok strony logowania
            /// </summary>
            return View();

        }
        [HttpPost]
        public IActionResult Login(Users newUser)
        {
            ///<summary>
            /// Obsługa zalogowania użytkownika
            /// </summary>
            /// <param name="newUser"> Zalogowany użytkownik </param>
            /// <returns> Odpowiedni widok </returns>
            var u = _context.Users
             .FirstOrDefault(u => u.Nick == newUser.Nick && u.Password == newUser.Password);

            if (u != null)
            {
                newUser.isLogged = true;
                var loggedUser = newUser;
                loggedUser.Id = u.Id;
                loggedUser.Email = u.Email;
                HttpContext.Session.SetObject("LoggedUser", loggedUser);
                return RedirectToAction("Index");

            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View();
            }
        }
        [HttpPost]
        [Route("/Home/Logout")]
        public IActionResult Logout()
        {
            ///<summary>
            /// Wylogowanie użytkownika.
            /// </summary>
            /// <returns> Status funkcji </returns>
            HttpContext.Session.Remove("LoggedUser");
            return Ok();
        }
        public IActionResult Register()
        {
            ///<summary>
            /// Widok rejestracji użytkownika.
            /// </summary>
            return View(getLoggedUser());
        }
        [HttpPost]
        public IActionResult Register(Users newUser)
        {
            ///<summary>
            /// Obsługa rejestracji użytkownika
            /// </summary>
            /// <param name="newUser"> Użytkownik </param>
            /// <returns> Odpowiedni widok </returns>
            try
            {
                var existingUser = _context.Users
                    .FirstOrDefault(u => u.Nick == newUser.Nick || u.Email == newUser.Email);

                if (existingUser != null)
                {
                    Console.WriteLine("Użytkownik jest już zarejestrowany.");

                    ModelState.AddModelError(string.Empty, "Użytkownik jest już zarejestrowany.");
                    return View();
                }
                else
                {
                    newUser.isLogged = true;

                    _context.Users.Add(newUser);
                    _context.SaveChanges();

                    Console.WriteLine($"Użytkownik {newUser.Email} został pomyślnie zarejestrowany.");

                    HttpContext.Session.SetObject("LoggedUser", newUser);

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas rejestracji użytkownika: " + ex.Message);

                ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas rejestracji użytkownika. Proszę spróbować ponownie później.");
                return View();
            }
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        
        public IActionResult Error()
        {
            ///<summary>
            /// Obsługa błędu na stronie
            /// </summary>
            /// <returns> Widok błędu </returns>
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
