using Discord.Commands;
using Discord.WebSocket;
using GeraltBot.Data;
using GeraltBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeraltBot.Services
{
    public class CustomCommandService
    {
        private readonly ApplicationDbContext _db;
        private readonly DiscordSocketClient _discord;
		private readonly LoggingService _logger;
		private SocketCommandContext Context;
		
        public CustomCommandService(ApplicationDbContext db, DiscordSocketClient discord, LoggingService logger)
        {
            _db = db;
            _discord = discord;
			_logger = logger;
        }
        public async Task ExecuteAsync(SocketCommandContext context)
        {
            Context = context;
			ChangeChannel(context.Message.MentionedChannels.ElementAt(0));
        }

		public async Task ChangeChannel(SocketGuildChannel channel)
		{
			if (channel != null)
			{
				if (Context.Guild.GetUser(Context.Message.Author.Id).GuildPermissions.Administrator)
				{
					if (await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).AnyAsync())
					{
						await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).ForEachAsync(async s => {
							await _logger.LogAsync($"User {Context.Message.Author.Username}#{Context.Message.Author.Discriminator}" +
								$" ({Context.Message.Author.Id}) changed default channel from" +
								$" {_discord.GetGuild((ulong)s.ServerId).Channels.Where(c => c.Id == (ulong)s.ChannelId).FirstOrDefault().Name} ({s.ChannelId})" +
								$" to { Context.Channel.Name} ({Context.Channel.Id})");
						});
					}
					else
					{
						Server server = new Server()
						{
							ServerId = (long)Context.Guild.Id,
							ChannelId = (long)Context.Channel.Id
						};
						_db.Servers.Add(server);

						await _logger.LogAsync($"User {Context.Message.Author.Username}#{Context.Message.Author.Discriminator}" +
								$" ({Context.Message.Author.Id}) has set default channel to {Context.Channel.Name} ({Context.Channel.Id})");

					}

					await _db.SaveChangesAsync();
					await ReplyAsync("Zmieniono kanał");
					Console.WriteLine();
				}
			}
		}

        private async Task ReplyAsync(string message)
        {
			await Context.Channel.SendMessageAsync(message);
        }
    }
}
