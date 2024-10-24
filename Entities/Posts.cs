namespace Y_Platform.Entities
{
    public class Posts
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public float? Prediction {  get; set; }
        public int NotOffensive { get; set; } = 0;
        public int Offensive { get; set; } = 0;

        //references
        public required Users Users { get; set; }

        public List<PostVotes> PostVotes { get; set; }
    }
}
