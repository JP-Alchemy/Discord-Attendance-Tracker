using Discord;

namespace DiscordAttBot.Models
{
    public class Utilities
    {
        public static string GetStatusEmoji(UserStatus status) => status switch {
            UserStatus.Online => ":green_circle:",
            UserStatus.Offline => ":black_circle:",
            UserStatus.Idle => ":orange_circle:",
            UserStatus.AFK => ":blue_circle:",
            UserStatus.DoNotDisturb => ":red_circle:",
            UserStatus.Invisible => ":white_circle:",
            _ => "-"
        };
    }
}