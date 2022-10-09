using System;
using Discord;

namespace DiscordAttBot.Models
{
    public class UserStatusInfo
    {
        public UserStatus Status;
        public DateTime TimeRecorded;
        public override string ToString()
        {
            return $"{Status} @ {TimeRecorded}";
        }
    }
}