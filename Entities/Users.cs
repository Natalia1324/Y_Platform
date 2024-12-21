using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace Y_Platform.Entities
{
    public class Users
    {
        /// <summary>
        /// Klasa reprezentująca tabele użytkowników
        /// </summary>
        public int Id { get; set; }
        public required string Nick { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }

        // References
        public List<Posts> Posts { get; set; } = new List<Posts>();
        public List<PostVotes> PostVotes { get; set; } = new List<PostVotes>();

        [NotMapped]
        public bool isLogged { get; set; } = false;
    }
}

