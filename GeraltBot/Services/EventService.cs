using Discord.WebSocket;
using GeraltBot.Data;
using GeraltBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeraltBot.Services
{
    class EventService
    {
		private readonly ApplicationDbContext _db;
		private readonly DiscordSocketClient _discord;
		public EventService(ApplicationDbContext db, DiscordSocketClient discord)
		{
			_discord = discord;
			_db = db;
		}

		public async Task ChannelDestroyed(SocketChannel channel)
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
