using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Y_Platform.Entities;
using Y_Platform.Models;

namespace Y_Platform.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }
        private Users getLoggedUser()
        {
            return HttpContext.Session.GetObject<Users>("LoggedUser");
        }

        [HttpPost]
        [HttpPost]
        public IActionResult Vote(int postId, bool isOffensive)
        {
            var user = getLoggedUser();
            if (user == null) return Unauthorized();

            var post = _context.Posts.FirstOrDefault(p => p.Id == postId);
            if (post == null) return NotFound();

            // Sprawdzanie, czy użytkownik już głosował na ten post
            var existingVote = _context.PostVotes
                .FirstOrDefault(v => v.Post.Id == post.Id && v.User.Id == user.Id);

            if (existingVote != null)
            {
                // Aktualizacja głosu, jeśli użytkownik już głosował
                existingVote.IsOffensive = isOffensive;
                _context.SaveChanges();
                return Ok("Vote updated.");
            }

            // Nowe głosowanie
            var vote = new PostVotes
            {
                Post = post,  // Automatycznie ustawi PostId
                User = user,  // Automatycznie ustawi UserId
                IsOffensive = isOffensive
            };

            _context.PostVotes.Add(vote);
            _context.SaveChanges();

            return Ok("Vote registered.");
        }

    }
}
