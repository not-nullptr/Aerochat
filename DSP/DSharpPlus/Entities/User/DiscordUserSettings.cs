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

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    public class DiscordUserSettings
    {
        [JsonProperty("timezone_offset")]
        public long TimezoneOffset { get; internal set; }

        [JsonProperty("theme")]
        public string Theme { get; internal set; }

        [JsonProperty("status")]
        public string Status { get; internal set; }

        [JsonProperty("show_current_game")]
        public bool ShowCurrentGame { get; internal set; }

        [JsonProperty("restricted_guilds")]
        public IList<ulong> RestrictedGuilds { get; internal set; }

        [JsonProperty("render_reactions")]
        public bool RenderReactions { get; internal set; }

        [JsonProperty("render_embeds")]
        public bool RenderEmbeds { get; internal set; }

        [JsonProperty("message_display_compact")]
        public bool MessageDisplayCompact { get; internal set; }

        [JsonProperty("locale")]
        public string Locale { get; internal set; }

        [JsonProperty("inline_embed_media")]
        public bool InlineEmbedMedia { get; internal set; }

        [JsonProperty("inline_attachment_media")]
        public bool InlineAttachmentMedia { get; internal set; }

        [JsonProperty("guild_positions")]
        public IList<ulong> GuildPositions { get; internal set; }

        [JsonProperty("guild_folders")]
        public IList<DiscordGuildFolder> GuildFolders { get; internal set; }

        [JsonProperty("gif_auto_play")]
        public bool GifAutoPlay { get; internal set; }

        // TODO: ????
        // [JsonProperty("friend_source_flags")]
        // public FriendSourceFlags FriendSourceFlags { get; internal set; }

        [JsonProperty("explicit_content_filter")]
        public int ExplicitContentFilter { get; internal set; }

        [JsonProperty("enable_tts_command")]
        public bool EnableTtsCommand { get; internal set; }

        [JsonProperty("developer_mode")]
        public bool DeveloperMode { get; internal set; }

        [JsonProperty("detect_platform_accounts")]
        public bool DetectPlatformAccounts { get; internal set; }

        [JsonProperty("default_guilds_restricted")]
        public bool DefaultGuildsRestricted { get; internal set; }

        [JsonProperty("convert_emoticons")]
        public bool ConvertEmoticons { get; internal set; }

        [JsonProperty("animate_emoji")]
        public bool AnimateEmoji { get; internal set; }

        [JsonProperty("afk_timeout")]
        public int AfkTimeout { get; internal set; }
    }
}
