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
using Microsoft.EntityFrameworkCore;

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
		public CommandModule(ApplicationDbContext db, DiscordSocketClient discord, Config config, serwerSOAPPortClient client)
		{
			_config = config;
			_client = client;
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

				if (result.x != 0 && result.y != 0)
				{
					User user = await _db.Users.Include(u => u.Server).AsAsyncEnumerable().Where(u => u.UserId == (long)Context.Message.Author.Id && u.Server.ServerId == (long)Context.Guild.Id).FirstOrDefaultAsync();

					if (user != null)
					{
						user.City = location;
						user.x = result.x;
						user.y = result.y;
						user.LastStorm = new DateTime();
						user.StormActive = false;
					}
					else
					{
						User newUser = new User()
						{
							UserId = (long)Context.Message.Author.Id,
							ApiKey = _config.ApiKey,
							City = location,
							x = result.x,
							y = result.y
						};

						Server server = await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).FirstOrDefaultAsync();
						if (server != null)
                        {
							newUser.Server = server;
                        }
                        else
                        {
							Server newServer = new Server()
							{
								ServerId = (long) Context.Guild.Id,
								ChannelId = (long) Context.Guild.DefaultChannel.Id
							};
							newUser.Server = newServer;
                        }

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
				List<User> users = await _db.Users.AsAsyncEnumerable().Where(u => u.UserId == (long)Context.Message.Author.Id).ToListAsync();
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
				if(await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).AnyAsync())
                {
					var server = _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).FirstOrDefaultAsync();
                }
                else
                {
					Server server = new Server()
					{
						ServerId = (long)Context.Guild.Id,
						ChannelId = (long)Context.Channel.Id
					};
					_db.Servers.Add(server);
                }
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
		public StormModule(ApplicationDbContext db, DiscordSocketClient discord, Config config, serwerSOAPPortClient client)
		{
			_db = db;
			_discord = discord;
			_client = client;
			_config = config;

			CheckStorm();
		}

		public Task CheckStorm()
		{
			var CheckStormTask = Task.Run(async () =>
			{
				while (true)
				{
					List<User> users = await _db.Users.Include(u => u.Server).AsAsyncEnumerable().ToListAsync();
					foreach (User item in users)
					{
						var result = await _client.szukaj_burzyAsync(item.y.ToString().Replace(',', '.'), item.x.ToString().Replace(',', '.'), 25, item.ApiKey);
						TimeSpan span = DateTime.Now - item.LastStorm;
						if (result.odleglosc != 0)
						{
							if (!item.StormActive)
							{
								SocketUser user = _discord.GetUser((ulong)item.UserId);
								var server = _discord.GetGuild((ulong)item.Server.ServerId);
								await server.GetTextChannel((ulong)item.Server.ChannelId).SendMessageAsync(string.Format("{0} Burza psiakrew...", user.Mention));
								item.StormActive = true;
							}
							item.LastStorm = DateTime.Now;
						}
						else
						{
							if (span.TotalMinutes > 30 && item.StormActive) item.StormActive = false;
						}
						await _db.SaveChangesAsync();
					}
					await Task.Delay(60000);
				}
			});
			return Task.CompletedTask;
		}
	}
	public class Events : ModuleBase<SocketCommandContext>
	{
		private readonly ApplicationDbContext _db;
		private readonly DiscordSocketClient _discord;
        public Events(DiscordSocketClient discord, ApplicationDbContext db)
        {
			_discord = discord;
			_db = db;

			_discord.LeftGuild += LeftGuild; 
        }

		public async Task LeftGuild(SocketGuild guild)
        {
			List<User> users = _db.Users.Include(u => u.Server).Where(u => u.Server.ServerId == (long)guild.Id).ToListAsync().Result;
			_db.Users.RemoveRange(users);
			var server = _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)guild.Id).FirstOrDefaultAsync();
			_db.Servers.Remove(server.Result);
			await _db.SaveChangesAsync();
        }
	}
}