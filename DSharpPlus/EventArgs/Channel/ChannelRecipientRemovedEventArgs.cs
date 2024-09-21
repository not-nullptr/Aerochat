using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;

namespace DSharpPlus.EventArgs
{
    public class ChannelRecipientRemovedEventArgs : DiscordEventArgs
    {
        public DiscordDmChannel Channel;
        public DiscordUser User;
    }
}
