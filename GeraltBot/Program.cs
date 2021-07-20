using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using GeraltBot.Models;
using GeraltBot.Data;
using ServiceReference1;
using GeraltBot.Services;

namespace GeraltBot
{
    class Program
    {
        // Program entry point
        static void Main()
        {
            // Call the Program constructor, followed by the 
            // MainAsync method and wait until it finishes (which should be never).
            new Program().MainAsync().GetAwaiter().GetResult();
        }


        private readonly DiscordSocketClient _discord;
        // Keep the CommandService and DI container around for use with commands.
        // These two types require you install the Discord.Net.Commands package.
        private readonly CommandService _commands;
        private readonly CustomCommandService _customCommands;
        private readonly EventService _events;
        private readonly ApplicationDbContext _db;
        private readonly LoggingService _logger;
        private readonly IServiceProvider _services;
        private readonly Config _config;
        private readonly serwerSOAPPortClient _client;
        private Program()
        {
            _client = new serwerSOAPPortClient();
            _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            _logger = new LoggingService();
            _discord = new DiscordSocketClient(new DiscordSocketConfig
            {
                // How much logging do you want to see?
                LogLevel = LogSeverity.Info,

                // If you or another service needs to do anything with messages
                // (eg. checking Reactions, checking the content of edited/deleted messages),
                // you must set the MessageCacheSize. You may adjust the number as needed.
                //MessageCacheSize = 50,

                // If your platform doesn't have native WebSockets,
                // add Discord.Net.Providers.WS4Net from NuGet,
                // add the `using` at the top, and uncomment this line:
                //WebSocketProvider = WS4NetProvider.Instance
            });
            _db = new ApplicationDbContextFactory().CreateDbContext(new string[]{""});
            _events = new EventService(_db, _discord);
            _customCommands = new CustomCommandService(_db,_discord, _logger);
            _commands = new CommandService(new CommandServiceConfig
            {
                // Again, log level:
                LogLevel = LogSeverity.Info,

                // There's a few more properties you can set,
                // for example, case-insensitive commands.
                CaseSensitiveCommands = false,
            });
            // Subscribe the logging handler to both the client and the CommandService.
            _discord.Log += _logger.LogAsync;
            _commands.Log += _logger.LogAsync;
            // Setup your DI container.
            _services = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection()
                .AddDbContext<ApplicationDbContext>()
                .AddSingleton(_discord)
                .AddSingleton(_config)
                .AddSingleton(_client)
                .AddSingleton(_logger);

            // When all your required services are in the collection, build the container.
            // Tip: There's an overload taking in a 'validateScopes' bool to make sure
            // you haven't made any mistakes in your dependency graph.
            return map.BuildServiceProvider();
        }

        private async Task MainAsync()
        {
            // Centralize the logic for commands into a separate method.
            await InitCommands();
            await InitEvents();
            await _discord.LoginAsync(TokenType.Bot,
                _config.BotToken);
            await _discord.StartAsync();
            // Wait infinitely so your bot actually stays connected.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task InitCommands()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            // Or add Modules manually if you prefer to be a little more explicit:
            //      await _commands.AddModuleAsync<SomeModule>(_services);
            // Note that the first one is 'Modules' (plural) and the second is 'Module' (singular).

            // Subscribe a handler to see if a message invokes a command.
            _discord.MessageReceived += HandleCommandAsync;
        }

        private Task InitEvents()
        {
            _discord.LeftGuild += _events.LeftGuild;
            _discord.ChannelDestroyed += _events.ChannelDestroyed;

            return Task.CompletedTask;
        }


        private async Task HandleCommandAsync(SocketMessage arg)
        {
            // Bail out if it's a System Message.
            if (arg is not SocketUserMessage msg) return;

            // We don't want the bot to respond to itself or other bots.
            if (msg.Author.Id == _discord.CurrentUser.Id || msg.Author.IsBot) return;

            // Create a number to track where the prefix ends and the command begins
            int pos = 0;
            // Replace the '!' with whatever character
            // you want to prefix your commands with.
            // Uncomment the second half if you also want
            // commands to be invoked by mentioning the bot instead.
            if (msg.HasMentionPrefix(_discord.CurrentUser, ref pos))
            {
                // Create a Command Context.
                var context = new SocketCommandContext(_discord, msg);
                if (msg.MentionedChannels.Count > 0)
                {
                    await _customCommands.ExecuteAsync(context);
                }
                else
                {
                    var result = await _commands.ExecuteAsync(context, pos, _services);

                    if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                        await msg.Channel.SendMessageAsync(result.ErrorReason);
                }                
            }
        }
    }
}
