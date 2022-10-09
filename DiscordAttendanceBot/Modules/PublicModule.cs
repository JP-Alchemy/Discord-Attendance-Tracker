using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordAttBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [Command("version")]
        public Task GetVersion() => ReplyAsync($"Version: {Program.Version}");

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [Command("guild_only")]
        [RequireContext(ContextType.Guild,
            ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand() => ReplyAsync("Nothing to see here!");
    }
}