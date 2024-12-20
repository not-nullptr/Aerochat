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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace DSharpPlus.Net.Abstractions
{
    // Aerochat-specific.
    internal class TransportProfile
    {
        [JsonProperty("badges", NullValueHandling=NullValueHandling.Ignore)]
        public IList<JObject> Badges { get; internal set; }

        [JsonProperty("connected_accounts", NullValueHandling = NullValueHandling.Ignore)]
        public IList<JObject> ConnectedAccounts { get; internal set; }

        [JsonProperty("guild_badges", NullValueHandling = NullValueHandling.Ignore)]
        public IList<JObject> GuildBadges { get; internal set; }

        [JsonProperty("mutual_guilds", NullValueHandling = NullValueHandling.Ignore)]
        public IList<JObject> MutualGuilds { get; internal set; }

        [JsonProperty("premium_guild_since", NullValueHandling = NullValueHandling.Ignore)]
        public object? PremiumGuildSince { get; internal set; }

        [JsonProperty("premium_since", NullValueHandling = NullValueHandling.Ignore)]
        public object? PremiumSince { get; internal set; }

        [JsonProperty("premium_type", NullValueHandling = NullValueHandling.Ignore)]
        public object? PremiumType { get; internal set; }

        [JsonProperty("user")]
        public TransportUser User { get; internal set; }

        [JsonProperty("user_profile", NullValueHandling = NullValueHandling.Ignore)]
        public TransportUserProfile UserProfile { get; internal set; }

        internal TransportProfile() { }

        internal TransportProfile(TransportProfile other)
        {
            this.Badges = other.Badges;
            this.MutualGuilds = other.MutualGuilds;
            this.PremiumGuildSince = other.PremiumGuildSince;
            this.PremiumSince = other.PremiumSince;
            this.PremiumType = other.PremiumType;
            this.User = other.User;
            this.UserProfile = other.UserProfile;
        }
    }
}
