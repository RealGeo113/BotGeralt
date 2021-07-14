using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeraltBot.Models
{
    public class Server
    {
        [Key]
        public long Id { get; set; }
        public long ServerId { get; set; }
        public long ChannelId { get; set; }
    }
}
