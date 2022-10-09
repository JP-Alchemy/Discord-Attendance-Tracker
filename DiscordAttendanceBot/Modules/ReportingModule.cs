using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordAttBot.Roles;
using DiscordAttBot.Services;
using MongoDB.Entities;

namespace DiscordAttBot.Modules
{
    [RequireContext(ContextType.DM)]
    [NamedRole("Admin", "Admin 2")]
    public class ReportingModule : ModuleBase<SocketCommandContext>
    {
        private readonly ReportingService _reportingService;

        public ReportingModule(ReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        // Gets the report of the date given
        [Command("report")]
        [Summary("Get's the user list report.")]
        public async Task GetReport(
            [Summary("The date from.")] string date1 = null,
            [Summary("The date to.")] string date2 = null)
        {
            var (valid1, dt1) = await ValidateDate(date1);
            if (!valid1) return;

            if (date2 == null)
            {
                // Start letting the user know we are processing
                await ReplyAsync($":hourglass_flowing_sand: ...Retrieving Data For **{dt1.Date:d}**... :hourglass_flowing_sand:");
                var reportResult = await _reportingService.GetReport(dt1);

                if (reportResult == string.Empty)
                {
                    await ReplyAsync($":scream: Could not find data for **{dt1.Date:d}**!");
                }
                else
                {
                    var byteArray = Encoding.ASCII.GetBytes(reportResult);
                    var stream = new MemoryStream(byteArray);
                    await Context.Channel.SendFileAsync(stream, $"{dt1.Date:d}-Attendance.csv",
                        "Here is your file!");
                }
            }
            else
            {
                var (valid2, dt2) = await ValidateDate(date2);
                if (!valid2) return;

                await ReplyAsync($":hourglass_flowing_sand: ...Retrieving Data From **{dt1.Date:d}** to **{dt2.Date:d}**... :hourglass_flowing_sand:");
                var reportResult = await _reportingService.GetReport(dt1, dt2);
                if (reportResult == string.Empty)
                {
                    await ReplyAsync($":scream: Could not find data From **{dt1.Date:d} to {dt2.Date:d}**!");
                }
                else
                {
                    var byteArray = Encoding.ASCII.GetBytes(reportResult);
                    var stream = new MemoryStream(byteArray);
                    await Context.Channel.SendFileAsync(stream, $"{dt1.Date:d}-{dt2.Date:d}-Attendance.csv",
                        "Here is your file!");
                }
            }
        }

        // Gets the report of the date given
        [Command("summary")]
        [Summary("Get's the user list summary as a message.")]
        public async Task GetSummary(
            [Summary("The date to get a summary for.")]
            string date = null)
        {
            var (valid, dt) = await ValidateDate(date);
            if (!valid) return;

            // Start letting the user know we are processing
            await ReplyAsync(
                $":hourglass_flowing_sand: ...Retrieving Data For {dt.Date:d}... :hourglass_flowing_sand:");
            var summaryResult = await _reportingService.GetSummary(dt);

            if (summaryResult == null)
            {
                await ReplyAsync($":scream: Could not find data for **{dt.Date:d}**!");
            }
            else
            {
                foreach (var summary in summaryResult)
                {
                    await ReplyAsync(summary);
                }
            }
        }

        [Command("delete")]
        public async Task Delete([Summary("The date from.")] string date1 = null,
            [Summary("The date to.")] string date2 = null)
        {
            if (date1 == null)
            {
                await ReplyAsync(":warning: Please specify a deletion date! Structure is **MM/DD/YYYY**");
                return;
            }
            var (valid1, dt1) = await ValidateDate(date1);
            if (!valid1) return;

            if (dt1 == DateTime.UtcNow.Date)
            {
                await ReplyAsync($"Can not delete todays data, wait for end of day.");
                return;
            }

            if (date2 == null)
            {
                await ReplyAsync($":hourglass_flowing_sand: ...Deleting Data On **{dt1.Date:d}**... :hourglass_flowing_sand:");
                var res = await _reportingService.DeleteReports(dt1);
                if (res == null)
                {
                    await ReplyAsync($":scream: Could not delete data for **{dt1.Date:d}**!");
                }
                else
                {
                    await ReplyAsync($"Success! Deleted **{res.DeletedCount} records**!");
                }
            }
            else
            {
                var (valid2, dt2) = await ValidateDate(date2);
                if (!valid2) return;

                if (dt2 == DateTime.UtcNow.Date)
                {
                    await ReplyAsync($"Can not delete todays data, wait for end of day.");
                    return;
                }

                await ReplyAsync($":hourglass_flowing_sand: ...Deleting Data From **{dt1.Date:d}** to **{dt2.Date:d}**... :hourglass_flowing_sand:");

                var res = await _reportingService.DeleteReports(dt1, dt2);
                if (res == null)
                {
                    await ReplyAsync($":scream: Could not delete data From **{dt1.Date:d}** to **{dt2.Date:d}**!");
                }
                else
                {
                    await ReplyAsync($"Success! Deleted **{res.DeletedCount} records**!");
                }
            }
        }

        [Command("set gmt")]
        public async Task SetTimeZone([Summary("The time to set the gmt to.")] int time)
        {
            Program.BotConfiguration.TimeZone = time;
            await Program.BotConfiguration.SaveAsync();
            await ReplyAsync($"GMT set to: **{time}**");
        }

        [Command("refresh")]
        public async Task Refresh()
        {
            DiscordUserMonitorService.ForceNewDay = true;
            await ReplyAsync($"Refreshing cache - this will take 5 seconds");
        }

        private async Task<(bool, DateTime)> ValidateDate(string date)
        {
            DateTime dt;
            if (date == null) dt = DateTime.UtcNow.Date;
            else
            {
                var validated = DateTime.TryParse(date, out dt);
                if (!validated)
                {
                    await ReplyAsync(":warning: Invalid date! Structure is **MM/DD/YYYY**");
                    return (false, dt);
                }
            }

            if (dt > DateTime.Now)
            {
                await ReplyAsync(
                    "https://tenor.com/view/back-to-the-future-doc-brown-smile-hey-christopher-lloyd-gif-4584331");
                return (false, dt);
            }


            return (true, dt);
        }
    }
}