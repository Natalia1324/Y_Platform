using Y_Platform.Entities;

namespace Y_Platform.Models
{
    /// <summary>
    /// Klasa przechowywująca informacje o postach, dla ułatwienia wyświetlania w Index.cshtml
    /// </summary>
    public class PostViewModel
    {
        public Posts Post { get; set; }
        public PostVotes? UserVote { get; set; }
        public int OffensiveVotes { get; set; } 
        public int NotOffensiveVotes { get; set; }
    }
}

