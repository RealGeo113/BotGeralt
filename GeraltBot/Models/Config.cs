using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeraltBot.Models
{
    public class Config
    {
        public string BotToken { get; set; }
        public string ApiKey { get; set; }
        public Database Database { get; set; }
    }

    public class Database
    {
        public string Host { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
