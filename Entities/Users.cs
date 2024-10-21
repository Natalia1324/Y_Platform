using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace Y_Platform.Entities
{
    public class Users
    {
        public int Id { get; set; }
        public string Nick { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        // References
        public List<Post> Posts { get; set; } = new List<Post>();
     
        [NotMapped]
        public bool isLogged { get; set; } = false;
    }
}

