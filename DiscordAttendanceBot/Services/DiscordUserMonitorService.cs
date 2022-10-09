using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Discord;
using Discord.WebSocket;
using DiscordAttBot.Models;

namespace DiscordAttBot.Services
{
    public class DiscordUserMonitorService
    {
        private readonly DiscordSocketClient _discord;
        private readonly DiscordUserService _userService;

        private CancellationTokenSource Wtoken;
        private Task _task;
        private SocketChannel _channel;
        private const int HeartBeatMilliSec = 5000;
        private static int GmtOffset => Program.BotConfiguration.TimeZone;
        public static bool ForceNewDay;
        public static DateTime StoredDate;

        public DiscordUserMonitorService(DiscordSocketClient discordSocketClient, DiscordUserService userService)
        {
            _discord = discordSocketClient;
            _userService = userService;

            _discord.Ready += ReadyAsync;
        }

        private async Task ReadyAsync()
        {
            if (_task != null)
            {
                Wtoken.Cancel();
            }

            _channel = _discord.GetChannel(Program.BotConfiguration.ChannelId);
            if (_channel != null)
            {
                Wtoken = new CancellationTokenSource();

                // When server startup add to the DB and local cache
                foreach (var user in _channel.Users)
                {
                    if (user.IsBot) continue;
                    await _userService.AddFreshUser(user);
                }

                StoredDate = DateTime.UtcNow.Date;
                _task = Task.Run(async () =>
                {
                    while (true)
                    {
                        await MonitorChannel();
                        await Task.Delay(HeartBeatMilliSec, Wtoken.Token); // <- await with cancellation
                    }
                }, Wtoken.Token);
            }
            else
            {
                Console.WriteLine("ERR: Could not find channel -> No monitor activated");
                await _discord.GetUser(Program.BotConfiguration.OwnerId)
                    .SendMessageAsync("Could not find a Channel to monitor");
            }
        }

        /// <summary>
        /// Ran every x seconds to monitor status changes for each user in the channel
        /// </summary>
        /// <returns></returns>
        private async Task MonitorChannel()
        {
            // If it is a new day or force refresh
            if (DateTime.UtcNow.Date > StoredDate || ForceNewDay)
            {
                if (ForceNewDay)
                {
                    Console.WriteLine($"Forced new day -- processing...");
                    ForceNewDay = false;
                }
                
                // Update the stored date and clear the cache for new cache
                StoredDate = DateTime.UtcNow.Date;
                DiscordUserService.UserStoredStatus.Clear();
            }
            
            foreach (var user in _channel.Users)
            {
                if (user.IsBot) continue;
                if (DiscordUserService.UserStoredStatus.TryGetValue(user.Id, out var info))
                {
                    // current status equals stored status
                    if (user.Status == info.Status) continue;
                    
                    var nInfo = new UserStatusInfo
                    {
                        Status = user.Status,
                        TimeRecorded = DateTime.UtcNow
                    };

                    Console.WriteLine();
                    TimeSpan ts = nInfo.TimeRecorded - info.TimeRecorded;
                    var text = new StringBuilder();
                    text.AppendLine($"{user.Username} was {info.Status} for {ts.TotalHours:F} hours");
                    text.AppendLine($"{user.Username} now {nInfo}");
                    Console.WriteLine(text.ToString());

                    await _userService.AddStatusChange(user.Id, DateTime.UtcNow.Date, nInfo);
                    DiscordUserService.UserStoredStatus[user.Id] = nInfo;
                }
                else
                {
                    Console.WriteLine($"New day / user: {user.Username} @ {StoredDate}");
                    await _userService.AddFreshUser(user);
                }
            }
            await Task.Delay(HeartBeatMilliSec);
        }
    }
}