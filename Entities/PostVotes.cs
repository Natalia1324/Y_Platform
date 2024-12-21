namespace Y_Platform.Entities
{
    public class PostVotes
    {
        /// <summary>
        /// Klasa reprezentująca głosy oddane na posty
        /// </summary>
        public int Id { get; set; }
        public bool IsOffensive { get; set; }

        // References
        public required Posts Post { get; set; }
        public required Users User { get; set; }
    }
}
