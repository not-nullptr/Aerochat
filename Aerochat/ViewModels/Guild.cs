using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    public class GuildViewModel : ViewModelBase
    {
        private string _name;
        private ulong _id;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public ulong Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public static GuildViewModel FromGuild(DiscordGuild guild)
        {
            return new GuildViewModel
            {
                Name = guild.Name,
                Id = guild.Id
            };
        }
    }
}
