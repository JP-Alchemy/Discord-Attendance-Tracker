using System.Collections.Generic;
using MongoDB.Entities;

namespace DiscordAttBot.Models
{
    public class BotConfigEntity : Entity
    {
        public string Token { get; set; }
        public ulong OwnerId { get; set; }
        public ulong ChannelId { get; set; }
        public int TimeZone { get; set; }
    }
}