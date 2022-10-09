using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordAttBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;
            _discord.Ready += ReadyAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
        
        private async Task ReadyAsync()
        {
            Console.WriteLine($"{_discord.CurrentUser} is connected!");
            await _discord.SetStatusAsync(UserStatus.Invisible);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            if (!(message.Channel is SocketDMChannel)) return;
            
            // This value holds the offset where the prefix ends
            var argPos = 0;
            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.
            if (!message.HasCharPrefix('!', ref argPos))
            {
                await Usage(rawMessage);
                return;
            }

            var context = new SocketCommandContext(_discord, message);
            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            await _commands.ExecuteAsync(context, argPos, _services); 
            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }
        
        private static async Task Usage(SocketMessage message)
        {
            var text = new StringBuilder();
            text.AppendLine("__Usage:__");
            text.AppendLine("`!report`\t  - sends CSV report | optional: *enter a from date* | optional: *enter a to date*");
            text.AppendLine("`!delete`\t  - delete data in database for date specified | *enter a from date* | optional: *enter a to date*");
            text.AppendLine("`!summary`\t- sends easy to read summary | optional: *enter a from date*");
            text.AppendLine("`!set gmt`\t- sets the bots GMT offset | *enter a value offset eg: '2' for gmt+2 etc...*");
            text.AppendLine("`!version`\t- gets the current bot version");
            text.AppendLine("`!refresh`\t- refreshes the server cache");
            await message.Channel.SendMessageAsync(text.ToString());
        }
    }
}