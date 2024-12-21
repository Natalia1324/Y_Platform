namespace Y_Platform.Entities
{
    public class Posts
    {
        /// <summary>
        /// Klasa reprezentująca tabele postów
        /// </summary>
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public float? Prediction {  get; set; }

        //references
        public required Users Users { get; set; }

        public List<PostVotes> PostVotes { get; set; }
    }
}
