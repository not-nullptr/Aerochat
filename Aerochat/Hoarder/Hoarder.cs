using Aerochat.ViewModels;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.Hoarder
{
    public static class Discord
    {   
        public static DiscordClient Client;

        public static bool Ready = false;

        public static string GetName(DiscordUser user)
        {
            return user.DisplayName;
        }

        // StringComp function which uses String.IsNullOrEmpty to pick the first non-empty string from an unlimited amount of strings
        public static string? StringComp(params string[] strings)
        {
            foreach (string str in strings)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    return str;
                }
            }
            return null;
        }
    }
}
