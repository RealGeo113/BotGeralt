using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeraltBot.Models
{
    public class User
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public string City { get; set; }
        public double x { get; set; }
        public double y { get; set; }
    }
}
