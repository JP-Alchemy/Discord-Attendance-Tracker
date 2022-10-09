using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using DiscordAttBot.Models;

namespace DiscordAttBot.Services
{
    public interface IDiscordUserService
    {
        Task AddFreshUser(IUser user);
        UserTracker CreateDefaultUserTracker(IUser user);
        Task UpsertUser(UserTracker userTrack);
        Task AddStatusChange(ulong discordId, DateTime today, UserStatusInfo statusChanged);
    }
}