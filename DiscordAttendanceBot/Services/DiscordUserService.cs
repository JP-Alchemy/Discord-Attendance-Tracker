using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using DiscordAttBot.Models;
using MongoDB.Driver;
using MongoDB.Entities;

namespace DiscordAttBot.Services
{
    public class DiscordUserService : IDiscordUserService
    {
        public static readonly Dictionary<ulong, UserStatusInfo> UserStoredStatus = new Dictionary<ulong, UserStatusInfo>();
        public async Task AddFreshUser(IUser user)
        {
            var uInfo = new UserStatusInfo
            {
                Status = user.Status,
                TimeRecorded = DateTime.UtcNow
            };

            var userTrack = CreateDefaultUserTracker(user);
            userTrack.StatusTrack.Add(uInfo);

            if (UserStoredStatus.TryAdd(user.Id, uInfo))
            {
                await UpsertUser(userTrack);
                await AddStatusChange(user.Id, DateTime.UtcNow.Date, uInfo);
                Console.WriteLine($"Info Start For: {user.Username} -> {uInfo}");
            }
        }

        public UserTracker CreateDefaultUserTracker(IUser user)
        {
            return new UserTracker
            {
                DiscordId = user.Id,
                Username = user.Username,
                Date = DateTime.UtcNow.Date,
                StatusTrack = new List<UserStatusInfo>()
            };
        }

        public async Task UpsertUser(UserTracker userTrack)
        {
            await DB.Update<UserTracker>()
                .Option(x => x.IsUpsert = true)
                .Match(u => u.DiscordId == userTrack.DiscordId && u.Date == userTrack.Date)
                .Modify(u => u.Username, userTrack.Username)
                .Modify(u => u.Date, userTrack.Date)
                .ExecuteAsync();
        }

        public async Task AddStatusChange(ulong discordId, DateTime date, UserStatusInfo statusChanged)
        {
            var filter = Builders<UserTracker>.Filter.Where(x => x.DiscordId == discordId && x.Date == TimeZoneInfo.ConvertTimeToUtc(date));
            var update = Builders<UserTracker>.Update.Push(x => x.StatusTrack, statusChanged);
            await DB.Collection<UserTracker>().FindOneAndUpdateAsync(filter, update);
        }
    }
}