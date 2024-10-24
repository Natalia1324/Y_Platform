namespace Y_Platform.Entities
{
    public class PostVotes
    {
        public int Id { get; set; }
        public bool IsOffensive { get; set; } // True if the user voted that the post is offensive

        // References
        public required Posts Post { get; set; }
        public required Users User { get; set; }
    }
}
