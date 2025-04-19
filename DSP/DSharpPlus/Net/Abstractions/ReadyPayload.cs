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
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DSharpPlus.Net.Abstractions
{
    internal class MergedPresences
    {
        [JsonProperty("friends", NullValueHandling = NullValueHandling.Ignore)]
        public JToken[] Friends { get; private set; }

        [JsonProperty("guilds", NullValueHandling = NullValueHandling.Ignore)]
        public JArray[] Guilds { get; private set; }
    }

    /// <summary>
    /// Represents data for websocket ready event payload.
    /// </summary>
    internal class ReadyPayload
    {
        /// <summary>
        /// Gets the gateway version the client is connected to.
        /// </summary>
        [JsonProperty("v")]
        public int GatewayVersion { get; private set; }

        /// <summary>
        /// Gets the current user.
        /// </summary>
        [JsonProperty("user")]
        public TransportUser CurrentUser { get; private set; }

        /// <summary>
        /// Gets the private channels available for this shard.
        /// </summary>
        [JsonProperty("private_channels")]
        public IReadOnlyList<DiscordDmChannel> DmChannels { get; private set; }

        /// <summary>
        /// Gets the guilds available for this shard.
        /// </summary>
        [JsonProperty("guilds")]
        public IReadOnlyList<DiscordGuild> Guilds { get; private set; }

        /// <summary>
        /// Gets the relationships available for this shard.
        /// </summary>
        [JsonProperty("relationships", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<DiscordRelationship> Relationships { get; private set; }

        /// <summary>
        /// Gets the relationships available for this shard.
        /// </summary>
        [JsonProperty("read_state", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<DiscordReadState> ReadStates { get; private set; }

        /// <summary>
        /// Gets the user settings for this shard
        /// </summary>
        [JsonProperty("user_settings", NullValueHandling = NullValueHandling.Ignore)]
        public DiscordUserSettings UserSettings { get; set; }

        /// <summary>
        /// Protobuf user settings encoded in base64.
        /// </summary>
        [JsonProperty("user_settings_proto", NullValueHandling = NullValueHandling.Ignore)]
        public string UserSettingsProto { get; set; }

        /// <summary>
        /// List of settings for guilds, includes data like muted channels 
        /// </summary>
        [JsonProperty("user_guild_settings", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<DiscordUserGuildSettings> UserGuildSettings { get; set; }

        /// <summary>
        /// List of users extracted from elsewhere in the READY payload
        /// </summary>
        [JsonProperty("users", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<TransportUser> Users { get; set; }

        /// <summary>
        /// List of members extracted from elsewhere in the READY payload, in the same order as <see cref="Guilds"/>
        /// </summary>
        [JsonProperty("merged_members", NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<IReadOnlyList<TransportMember>> MergedMembers { get; set; }

        [JsonProperty("merged_presences", NullValueHandling = NullValueHandling.Ignore)]
        public MergedPresences MergedPresences { get; set; }

        /// <summary>
        /// Gets the current session's ID.
        /// </summary>
        [JsonProperty("session_id")]
        public string SessionId { get; private set; }

        /// <summary>
        /// Refreshed auth token
        /// </summary>
        [JsonProperty("auth_token")]
        public string AuthToken { get; private set; }

        /// <summary>
        /// Gets debug data sent by Discord. This contains a list of servers to which the client is connected.
        /// </summary>
        [JsonProperty("_trace")]
        public IReadOnlyList<string> Trace { get; private set; }
    }
}
