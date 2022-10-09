using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordAttBot.Roles
{
    public class NamedRoleAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        private readonly string[] _names;

        // Create a constructor so the name can be specified
        public NamedRoleAttribute(params string[] name) => _names = name;

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var channel = await context.Client.GetChannelAsync(Program.BotConfiguration.ChannelId);
            var user = await channel.GetUserAsync(context.User.Id);
            // Check if this user is a Guild User, which is the only context where roles exist
            if (!(user is SocketGuildUser gUser))
                return PreconditionResult.FromError("You must be in a guild to run this command.");

            // If this command was executed by a user with the appropriate role, return a success
            if (gUser.Roles.Any(r => _names.Contains(r.Name)))
                return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError($"You must have a role named {string.Join(',', _names)} to run this command. \n " +
                                                $"https://tenor.com/view/not-gonna-happen-mackelmore-nope-nada-no-way-gif-15549117");
        }
    }
}