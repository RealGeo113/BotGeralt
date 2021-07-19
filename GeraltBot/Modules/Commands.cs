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


		public async Task ChangeChannel(SocketGuildChannel channel)
        {
			if(channel != null)
            {
				if (Context.Guild.GetUser(Context.Message.Author.Id).GuildPermissions.Administrator)
				{
					if (await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).AnyAsync())
					{
						await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).ForEachAsync(s => s.ChannelId = (long)channel.Id);
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
					Console.WriteLine($"User {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} ({Context.Message.Author.Id}) changed channel from {Context.Channel.Name} ({Context.Channel.Id}) to {Context.Channel.Name}");
				}
			}
		}

		[Command("burza")]
		[Summary("Psiakrew")]
		public async Task Psiakrew()
		{
			await ReplyAsync(string.Format("{0} Burza psiakrew.", Context.Message.Author.Mention));
		}

		[Command("burza")]
		[Summary("Checks for storm in 25km from specified location")]
		public async Task CheckStorm([Remainder][Summary("Location to test")] string location)
		{
			var result = await _client.miejscowoscAsync(location, _config.ApiKey);
			var id = Context.Message.Author.Id;
			if (result.x != 0 && result.y != 0)
			{
				var thunderstorm = await _client.szukaj_burzyAsync(result.y.ToString().Replace(',', '.'), result.x.ToString().Replace(',', '.'), 25, _config.ApiKey);

				if (thunderstorm.odleglosc != 0)
				{
					Embed embed = new EmbedBuilder()
						.WithColor(new Color(0,0,0))
						.WithTitle("Szczegóły burzy")
						.AddField("Lokalizacja", location, true)
						.AddField("Odleglosc", thunderstorm.odleglosc, true)
						.AddField("Ilosc", thunderstorm.liczba, true)
						.AddField("Kierunek", thunderstorm.kierunek, true)
						.Build();

					await ReplyAsync($"{Context.Message.Author.Mention} Burza psiakrew...");
					await ReplyAsync(embed: embed);
				}
				else
				{
					await ReplyAsync($"{Context.Message.Author.Mention} Ciemno wszędzie, głucho wszędzie...");
				}
			}
			else
			{
				await ReplyAsync($"{Context.Message.Author.Mention} Co to za zadupie?");
			}
		}

		[Command("zapisz")]
		[Alias("dodaj", "add", "save")]
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
					await ReplyAsync($"{Context.Message.Author.Mention} Dodano do bazy");
				}
				else
				{
					await ReplyAsync($"{Context.Message.Author.Mention} Co to za zadupie?");
				}
			}
		}

		[Command("klucz")]
		[Summary("Changes ApiKey to burze.dzis.net for the one provided by user")]
		public async Task ChangeKey([Remainder][Summary("Api Key burze.dzis.net")] string key)
        {
            if (Context.IsPrivate)
            {
                if (_client.KeyAPIAsync(key).Result)
                {
					List<User> users = await _db.Users.AsAsyncEnumerable().Where(u => u.UserId == (long)Context.Message.Author.Id).ToListAsync();
					foreach (User user in users)
					{
						user.ApiKey = key;
					}
					await _db.SaveChangesAsync();
					await ReplyAsync($"{Context.Message.Author.Mention} Klucz API został zmieniony.");
					Console.WriteLine($"User {Context.Message.Author.Username}#{Context.Message.Author.Discriminator} ({Context.Message.Author.Id}) changed API Key to: {key}");
                }
                else
                {
					await ReplyAsync($"Klucz `{key}` jest nieprawidłowy.");
                }
			}
            else
            {
				await ReplyAsync($"{Context.Message.Author.Mention} Wyślij mi klucz w wiadomości prywatnej!");
            }
        }

		[Command("tutaj")]
		[Summary("Changes channel Geralt displies warning messages on")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task ChangeChannelCommand()
        {
			await ChangeChannel(Context.Guild.Channels.Where(c => c.Id == Context.Channel.Id).FirstOrDefault());
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
						var thunderstorm = await _client.szukaj_burzyAsync(item.y.ToString().Replace(',', '.'), item.x.ToString().Replace(',', '.'), 10, item.ApiKey);
						TimeSpan span = DateTime.Now - item.LastStorm;
						if (thunderstorm.odleglosc != 0 && thunderstorm.liczba > 10)
						{
							if (!item.StormActive)
							{
								SocketUser user = _discord.GetUser((ulong)item.UserId);
								var server = _discord.GetGuild((ulong)item.Server.ServerId);

								Embed embed = new EmbedBuilder()
									.WithColor(new Color(0, 0, 0))
									.WithTitle("Szczegóły burzy")
									.AddField("Lokalizacja", item.City, true)
									.AddField("Odleglosc", thunderstorm.odleglosc, true)
									.AddField("Ilosc", thunderstorm.liczba, true)
									.AddField("Kierunek", thunderstorm.kierunek, true)
									.Build();

								await ReplyAsync($"{Context.Message.Author.Mention} Burza psiakrew...");
								await ReplyAsync(embed: embed);
								var textChannel = server.GetTextChannel((ulong)item.Server.ChannelId);
								await textChannel.SendMessageAsync(string.Format("{0} Burza psiakrew...", user.Mention));
								await textChannel.SendMessageAsync(embed: embed);
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
			_discord.ChannelDestroyed += ChannelDestroyed;
        }

        private async Task ChannelDestroyed(SocketChannel channel)
        {
			await _db.Servers
				.AsAsyncEnumerable()
				.Where(s => s.ChannelId == (long)channel.Id)
				.ForEachAsync(s => s.ChannelId = (long)_discord.GetGuild((ulong)s.ServerId).DefaultChannel.Id);

			await _db.SaveChangesAsync();
		}

        public async Task LeftGuild(SocketGuild guild)
        {
			List<User> users = _db.Users
				.Include(u => u.Server)
				.Where(u => u.Server.ServerId == (long)guild.Id)
				.ToListAsync().Result;

			_db.Users.RemoveRange(users);
			var server = _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)guild.Id).FirstOrDefaultAsync();
			_db.Servers.Remove(server.Result);
			await _db.SaveChangesAsync();
        }
	}
}