using Discord;
using Discord.Commands;
using Discord.WebSocket;
using GeraltBot.Data;
using GeraltBot.Models;
using Newtonsoft.Json;
using ServiceReference1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GeraltBot.Modules
{
	// Keep in mind your module **must** be public and inherit ModuleBase.
	// If it isn't, it will not be discovered by AddModulesAsync!
	public class CommandModule : ModuleBase<SocketCommandContext>
	{
		private Config _config { get; set; }
		private ApplicationDbContext _db { get; set; }
		private serwerSOAPPortClient _client { get; set; }
		private DiscordSocketClient _discord { get; set; }
		public CommandModule(ApplicationDbContext db, DiscordSocketClient discord)
		{
			_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
			_client = new serwerSOAPPortClient();
			_db = db;
			_discord = discord;
		}

		[Command("burza")]
		[Summary("Psiakrew")]
		public async Task Psiakrew()
		{
			await ReplyAsync(string.Format("{0} Burza psiakrew.", Context.Message.Author.Mention));
		}

		[Command("burza")]
		[Summary("Checks for storm in 25km from specified location")]
		public async Task Test([Remainder][Summary("Location to test")] string location)
		{
			var result = await _client.miejscowoscAsync(location, _config.ApiKey);
			var id = Context.Message.Author.Id;
			if (result.x != 0 && result.y != 0)
			{
				var thunderstorm = await _client.szukaj_burzyAsync(result.y.ToString().Replace(',', '.'), result.x.ToString().Replace(',', '.'), 25, _config.ApiKey);

				if (thunderstorm.odleglosc != 0)
				{
					await ReplyAsync(string.Format("{0} Burza Psiakrew...", Context.Message.Author.Mention));
				}
				else
				{
					await ReplyAsync(string.Format("{0} Ciemno wszędzie, głucho wszędzie...", Context.Message.Author.Mention));
				}
			}
			else
			{
				await ReplyAsync(string.Format("{0} Co to za zadupie?", Context.Message.Author.Mention));
			}
		}

		[Command("zapisz")]
		[Summary("Saves user location in database for automated storm checks")]
		public async Task Save([Remainder][Summary("Location to save")] string location)
		{
			if (!Context.IsPrivate)
			{
				var result = await _client.miejscowoscAsync(location, _config.ApiKey);

				User user = new User();
				if (result.x != 0 && result.y != 0)
				{
					user = await _db.Users.AsAsyncEnumerable().Where(u => u.UserId == Context.Message.Author.Id && u.ChannelId == Context.Channel.Id).FirstOrDefaultAsync();

					if (user != null)
					{
						user.City = location;
						user.x = result.x;
						user.y = result.y;
					}
					else
					{
						User newUser = new User()
						{
							UserId = Context.Message.Author.Id,
							ServerId = Context.Guild.Id,
							ChannelId = Context.Guild.DefaultChannel.Id,
							ApiKey = _config.ApiKey,
							City = location,
							x = result.x,
							y = result.y
						};

						_db.Users.Add(newUser);
					}
					await _db.SaveChangesAsync();
					await ReplyAsync(string.Format("{0} Dodano do bazy", Context.Message.Author.Mention));
				}
				else
				{
					await ReplyAsync(string.Format("{0} Co to za zadupie?", Context.Message.Author.Mention));
				}
			}
		}

		[Command("klucz")]
		[Summary("Changes ApiKey to burze.dzis.net for the one provided by user")]
		public async Task ChangeKey([Remainder][Summary("Api Key burze.dzis.net")] string key)
        {
            if (Context.IsPrivate)
            {
				List<User> users = await _db.Users.AsAsyncEnumerable().Where(u => u.UserId == Context.Message.Author.Id).ToListAsync();
				foreach (User user in users)
				{
					user.ApiKey = key;
				}
				await _db.SaveChangesAsync();
				await ReplyAsync(String.Format("{0} Klucz API został zmieniony", Context.Message.Author.Mention));
			}
            else
            {
				await ReplyAsync(String.Format("{0} Wyślij mi klucz w wiadomości prywatnej!", Context.Message.Author.Mention));
            }
        }

		[Command("tutaj")]
		[Summary("Changes channel Geralt displies warning messages on")]
		public async Task ChangeChannel()
        {
            if (Context.Guild.GetUser(Context.Message.Author.Id).GuildPermissions.Administrator)
            {
				await _db.Users.AsAsyncEnumerable().Where(u => u.ServerId == Context.Guild.Id).ForEachAsync(u => u.ChannelId = Context.Channel.Id);
				await _db.SaveChangesAsync();
				await ReplyAsync("Zmieniono kanał");
            }
        }
	}

	public class StormModule : ModuleBase<SocketCommandContext>
	{
		private ApplicationDbContext _db { get; set; }
		private DiscordSocketClient _discord { get; set; }
		private serwerSOAPPortClient _client { get; set; }
		private Config _config { get; set; }
		public StormModule(ApplicationDbContext db, DiscordSocketClient discord)
		{
			_db = db;
			_discord = discord;
			_client = new serwerSOAPPortClient();
			_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

			CheckStorm();
		}

		public Task CheckStorm()
		{
			var CheckStormTask = Task.Run(async () =>
			{
				while (true)
				{
					List<User> users = await _db.Users.ToListAsync();
					foreach (User item in users)
					{
						TimeSpan span = DateTime.Now - item.LastMessage;
						if(span.TotalHours > 12)
                        {
							var result = await _client.szukaj_burzyAsync(item.y.ToString().Replace(',', '.'), item.x.ToString().Replace(',', '.'), 25, _config.ApiKey);
							if (result.odleglosc != 0)
							{
								SocketUser user = _discord.GetUser(item.UserId);
								var server = _discord.GetGuild(item.ServerId);
								await server.GetTextChannel(item.ChannelId).SendMessageAsync(string.Format("{0} Burza psiakrew...", user.Mention));
								
								item.LastMessage = DateTime.Now;
							}
						}
					}
					await _db.SaveChangesAsync();
					await Task.Delay(60000);
				}
			});
			return Task.CompletedTask;
		}
	}
}
        // TODO:
        // 1. Take user location
        // 2. If exists, save to database
        // 3. Check if any warnings for specified location are present
        //	3a. I have only 10 calls per minute, can I schedule checks for different users
        // 4. If warning is present, return message about weather
        //	3b. Save user Api key if he provides one
        // TODO:
        // 1. Take user location
        // 2. Call burze.dzis.net API and check if location exists, if not reply with 
        //	  "Wrong location" message
        // 3. If exist, show thunderstorm warnings for location in 25km range