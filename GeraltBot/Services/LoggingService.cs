using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace GeraltBot.Services
{
	public class LoggingService
	{
		public Task LogAsync(LogMessage message)
		{
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"[{DateTime.Now,-19}] [{message.Severity,8}] {message.Source, 8}: {message.Message} {message.Exception}");
            Console.ResetColor();

			return Task.CompletedTask;
		}
        public Task LogAsync(string message)
        {
            Console.WriteLine($"[{DateTime.Now,-19}] [{"Info",8}] {"Client", 8}: {message}");
            return Task.CompletedTask;
        }
    }
}
