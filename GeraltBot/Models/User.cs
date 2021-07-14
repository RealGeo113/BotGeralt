using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeraltBot.Models
{
    public class User
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        [ForeignKey("Server")]
        public long ServerId { get; set; }
        public Server Server { get; set; }
        public string ApiKey { get; set; }
        public string City { get; set; }
        public float? x { get; set; }
        public float? y { get; set; }
        public DateTime LastMessage { get; set; }
    }
}
