using Y_Platform.Entities;

namespace Y_Platform.Models
{
    public class PostViewModel
    {
        public Posts Post { get; set; }
        public PostVotes? UserVote { get; set; }
    }
}
