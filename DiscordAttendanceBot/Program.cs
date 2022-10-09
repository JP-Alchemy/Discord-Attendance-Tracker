using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordAttBot.Models;
using DiscordAttBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Entities;

namespace DiscordAttBot
{
    static class Program
    {
        public static string Version = "001";
        public static ServerOptions ServerOptions = new ServerOptions();
        public static BotConfigEntity BotConfiguration = new BotConfigEntity();
        
        public static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();
            configuration.Bind(nameof(ServerOptions), ServerOptions);
            Version = configuration["Version"];
            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            await using var services = ConfigureServices();
            var client = services.GetRequiredService<DiscordSocketClient>();
            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

            await InitializeDatabase();
            var getConf = await GetConfig();

            if (getConf.Count >= 1)
            {
                BotConfiguration = getConf[0];
                Console.WriteLine($"Server for GMT + {BotConfiguration.TimeZone} Server time {TimeZoneInfo.Local.BaseUtcOffset}");
                await client.LoginAsync(TokenType.Bot, BotConfiguration.Token);
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                services.GetRequiredService<DiscordUserMonitorService>();
                // await new DiscordBot(BotConfiguration.Token).InitializeAsync();
                await Task.Delay(Timeout.Infinite);
            }
            else
            {
                Console.WriteLine("ERROR: Could not find any config!");
            }
        }
        
        private static Task LogAsync(LogMessage log)
        {
            Console.WriteLine($"Log: {log.ToString()}");

            return Task.CompletedTask;
        }

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<DiscordUserService>()
                .AddSingleton<ReportingService>()
                .AddSingleton<DiscordUserMonitorService>()
                .BuildServiceProvider();
        }

        private static async Task InitializeDatabase()
        {
            Console.WriteLine("...Initializing Database... --> " + ServerOptions.DatabaseName);
            var mClientSettings =
                MongoClientSettings.FromConnectionString(ServerOptions.MongoConnectionString);
            await DB.InitAsync(ServerOptions.DatabaseName, mClientSettings);
        }
        
        private static async Task<List<BotConfigEntity>> GetConfig()
        {
            return await DB.Find<BotConfigEntity>().ExecuteAsync();
        }
    }
}