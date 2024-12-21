namespace Y_Platform.Models
{
    /// <summary>
    /// Model reprezentujący żądanie zawierające dane głosowanias 
    /// </summary>
    public class VoteRequest
    {
        public int PostId { get; set; }
        public bool IsOffensive { get; set; }
    }
}
