namespace Y_Platform.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedDate { get; set; }
        public float? Prediction {  get; set; }

        //references
        public Users Users { get; set; }
    }
}
