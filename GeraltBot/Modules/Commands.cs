using Discord.Commands;
using GeraltBot.Models;
using Newtonsoft.Json;
using ServiceReference1;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GeraltBot.Modules
{

    // Keep in mind your module **must** be public and inherit ModuleBase.
    // If it isn't, it will not be discovered by AddModulesAsync!
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private Config _config { get; set; }

        public InfoModule()
        {
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
        }

        [Command("burza")]
        [Summary("Responds with 'Psiakrew'")]
        public async Task Psiakrew()
        {
            await ReplyAsync("Psiakrew");
        }

        // TODO:
        // 1. Take user location
        // 2. Call burze.dzis.net API and check if location exists, if not reply with 
        //	  "Wrong location" message
        // 3. If exist, show thunderstorm warnings for location in 25km range

        [Command("test")]
        [Summary("Test command for dev purpose")]
        public async Task Test([Remainder][Summary("Location to check")] string location)
        {

            serwerSOAPPortClient client = new();
            var result = await client.miejscowoscAsync(location, _config.ApiKey);
            if (result.x != 0 && result.y != 0)
            {
                var thunderstorm = await client.szukaj_burzyAsync(result.y.ToString().Replace(',', '.'), result.x.ToString().Replace(',', '.'), 25, _config.ApiKey);
                if (thunderstorm.odleglosc != 0)
                {
                    await ReplyAsync("Burza Psiakrew...");
                }
                else
                {
                    await ReplyAsync("Śpij spokojnie.");
                }
            }
            else
            {
                await ReplyAsync("Co to za zadupie?");
            }
        }


        // TODO:
        // 1. Take user location
        // 2. If exists, save to database
        // 3. Check if any warnings for specified location are present
        //	3a. I have only 10 calls per minute, can I schedule checks for different users
        //	3b. Save user Api key if he provides one
        // 4. If warning is present, return message about weather
    }
}