using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace Y_Platform.Entities
{
    public class Users
    {
        public int Id { get; set; }
        public required string Nick { get; set; }
        public required string Login { get; set; }
        public required string Password { get; set; }

        // References
        public List<Posts> Posts { get; set; } = new List<Posts>();
        public List<PostVotes> PostVotes { get; set; } = new List<PostVotes>();

        [NotMapped]
        public bool isLogged { get; set; } = false;
    }
}

