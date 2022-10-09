using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MongoDB.Entities;

namespace DiscordAttBot.Models
{
    public class UserTracker : Entity
    {
        public ulong DiscordId { get; set; }
        public string Username { get; set; }
        public DateTime Date { get; set; }
        public List<UserStatusInfo> StatusTrack { get; set; } = new List<UserStatusInfo>();

        public async Task<Dictionary<UserStatus, double>> GetStatusSummary()
        {
            return await Task.Run(() =>
            {
                var statusValues = (UserStatus[]) Enum.GetValues(typeof(UserStatus));
                var d = new Dictionary<UserStatus, double>();
                foreach (var status in statusValues)
                {
                    d.TryAdd(status, 0);
                }

                if (StatusTrack.Count >= 2)
                {
                    for (int i = 0; i < StatusTrack.Count - 1; i++)
                    {
                        var ts = StatusTrack[i + 1].TimeRecorded - StatusTrack[i].TimeRecorded;
                        d[StatusTrack[i].Status] += ts.TotalHours;
                    }
                }

                var lastDate = DateTime.UtcNow;
                if (lastDate.Date > StatusTrack[^1].TimeRecorded.Date)
                {
                    lastDate = StatusTrack[^1].TimeRecorded.Date.AddTicks(-1).AddDays(1);
                }

                var lastTs = lastDate - StatusTrack[^1].TimeRecorded;
                if (d.TryGetValue(StatusTrack[^1].Status, out _))
                {
                    d[StatusTrack[^1].Status] += lastTs.TotalHours;
                }
                else
                {
                    d.TryAdd(StatusTrack[^1].Status, lastTs.TotalHours);
                }

                return d;
            });
        }

        public async Task<string> GetSummary()
        {
            return await Task.Run(async () =>
            {
                var info = new StringBuilder();
                info.AppendLine($"**__{Username} has {StatusTrack.Count - 1} Status Changes today__**");
                var log = await GetStatusSummary();
                foreach (var (status, time) in log)
                {
                    if (time <= 0) continue;
                    info.AppendLine($"\t{Utilities.GetStatusEmoji(status)} **{status}** for **{time:F} hours**.");
                }

                info.AppendLine();

                return info.ToString();
            });
        }

        public async Task<string> GetReport(UserStatus[] statusValues)
        {
            return await Task.Run(async () =>
            {
                var log = await GetStatusSummary();
                string logInfo = $"{DiscordId},{Username},{Date:d},";
                for (int j = 0; j < statusValues.Length; j++)
                {
                    if (j + 1 != statusValues.Length) logInfo += $"{log[statusValues[j]]:F},";
                    else logInfo += $"{log[statusValues[j]]:F}";
                }

                return logInfo;
            });
        }
    }
}