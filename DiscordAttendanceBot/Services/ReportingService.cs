using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordAttBot.Models;
using MongoDB.Driver;
using MongoDB.Entities;

namespace DiscordAttBot.Services
{
    public class ReportingService
    {
        public async Task<string> GetReport(DateTime reqData)
        {
            var userInfo = await DB.Find<UserTracker>()
                .Match(x => x.Date == reqData)
                .ExecuteAsync();

            if (userInfo.Count == 0) return string.Empty;

            var info = new StringBuilder();
            var statusValues = (UserStatus[]) Enum.GetValues(typeof(UserStatus));
            var header = "ID,Username,Date,";
            for (int i = 0; i < statusValues.Length; i++)
            {
                if (i + 1 != statusValues.Length) header += statusValues[i] + ",";
                else header += statusValues[i];
            }

            info.AppendLine(header);
            
            var logs = await Task.WhenAll(userInfo.Select(u => u.GetReport(statusValues)));
            info.AppendLine(string.Join("\n", logs));
            return info.ToString();
        }

        public async Task<string> GetReport(DateTime reqDataFrom, DateTime reqDataTo)
        {
            reqDataTo = reqDataTo.AddDays(1);
            var userInfo = await DB.Find<UserTracker>()
                .Match(x => x.Date >= reqDataFrom && x.Date <= reqDataTo)
                .ExecuteAsync();

            if (userInfo.Count == 0) return string.Empty;

            var info = new StringBuilder();
            var statusValues = (UserStatus[])Enum.GetValues(typeof(UserStatus));
            var header = "ID,Username,Date,";
            for (int i = 0; i < statusValues.Length; i++)
            {
                if (i + 1 != statusValues.Length) header += statusValues[i] + ",";
                else header += statusValues[i];
            }

            info.AppendLine(header);

            var logs = await Task.WhenAll(userInfo.Select(u => u.GetReport(statusValues)));
            info.AppendLine(string.Join("\n", logs));
            return info.ToString();
        }

        public async Task<string[]> GetSummary(DateTime reqData)
        {
            var userInfo = await DB.Find<UserTracker>()
                .Match(x => x.Date == reqData)
                .ExecuteAsync();

            if (userInfo.Count == 0) return null;
            var logs = await Task.WhenAll(userInfo.Select(u => u.GetSummary()));
            return logs;
        }
        
        public async Task<DeleteResult> DeleteReports(DateTime reqData)
        {
            var userInfo = await DB.Find<UserTracker>()
                .Match(x => x.Date == reqData)
                .ExecuteAsync();

            if (userInfo.Count == 0) return null;
            return await DB.DeleteAsync<UserTracker>(userInfo.Select(e => e.ID));
        }

        public async Task<DeleteResult> DeleteReports(DateTime reqDataFrom, DateTime reqDataTo)
        {
            reqDataTo = reqDataTo.AddDays(1);
            var userInfo = await DB.Find<UserTracker>()
                .Match(x => x.Date >= reqDataFrom && x.Date <= reqDataTo)
                .ExecuteAsync();

            if (userInfo.Count == 0) return null;
            return await DB.DeleteAsync<UserTracker>(userInfo.Select(e => e.ID));
        }
    }
}