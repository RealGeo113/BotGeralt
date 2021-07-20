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
			await ChangeChannel(context.Message.MentionedChannels.ElementAt(0));
        }

		public async Task ChangeChannel(SocketGuildChannel channel)
		{
			if (channel != null)
			{
				if (Context.Guild.GetUser(Context.Message.Author.Id).GuildPermissions.Administrator)
				{
					Server server = await _db.Servers.AsAsyncEnumerable().Where(s => s.ServerId == (long)Context.Guild.Id).FirstOrDefaultAsync();
					if (server != null)
					{
						SocketGuildChannel guildChannel = _discord.GetGuild((ulong)server.ServerId).Channels.Where(c => c.Id == (ulong)server.ChannelId).FirstOrDefault();
						if(guildChannel != null) {
							await _logger.LogAsync($"User {Context.Message.Author.Username}#{Context.Message.Author.Discriminator}" +
							$" ({Context.Message.Author.Id}) changed default channel from" +
							$" {guildChannel.Name} ({server.ChannelId})" + 
							$" to { channel.Name} ({channel.Id})");
							server.ChannelId = (long)channel.Id;
						}else return;
					}
					else
					{
						server = new Server()
						{
							ServerId = (long)Context.Guild.Id,
							ChannelId = (long)channel.Id
						};
						_db.Servers.Add(server);

						await _logger.LogAsync($"User {Context.Message.Author.Username}#{Context.Message.Author.Discriminator}" +
								$" ({Context.Message.Author.Id}) has set default channel to {channel.Name} ({channel.Id})");
					}
					await _db.SaveChangesAsync();
					await ReplyAsync("Zmieniono kanał");
				}
            }
		}

        private async Task ReplyAsync(string message)
        {
			await Context.Channel.SendMessageAsync(message);
        }
    }
}
