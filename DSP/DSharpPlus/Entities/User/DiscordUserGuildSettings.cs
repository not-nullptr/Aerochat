// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2023 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Generic;
using System.Text;
using DSharpPlus.Enums;
using DSharpPlus.Net.Serialization;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{

    public class DiscordUserGuildSettings 
    {
        [JsonProperty("version")]
        public int Version { get; internal set; }

        [JsonProperty("guild_id")]
        public ulong? GuildId { get; internal set; }

        [JsonProperty("suppress_roles")]
        public bool SuppressRoles { get; internal set; }

        [JsonProperty("suppress_everyone")]
        public bool SuppressEveryone { get; internal set; }

        [JsonProperty("muted")]
        public bool Muted { get; internal set; }

        [JsonProperty("mute_config")]
        public DiscordMuteConfig MuteConfig { get; internal set; }

        [JsonProperty("message_notifications")]
        public DiscordMessageNotifications MessageNotifications { get; internal set; }

        [JsonProperty("mobile_push")]
        public bool MobilePushNotifications { get; internal set; }

        [JsonProperty("channel_overrides")]
        public List<DiscordUserChannelSettings> ChannelOverrides { get; internal set; }
            = new List<DiscordUserChannelSettings>();
    }
}
