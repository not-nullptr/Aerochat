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
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DSharpPlus
{
    public sealed partial class DiscordClient
    {
        #region Private Fields

        private string _sessionId;
        private bool _guildDownloadCompleted = false;

        #endregion

        #region Dispatch Handler

        internal async Task HandleDispatchAsync(GatewayPayload payload)
        {
            DiscordChannel chn;
            DiscordThreadChannel thread;
            ulong gid;
            ulong cid;
            TransportUser usr = default;
            TransportMember mbr = default;
            TransportUser refUsr = default;
            TransportMember refMbr = default;
            JToken rawMbr = default;
            JObject dat = payload.Data as JObject;
            JArray rawMembers = default;
            JArray rawPresences = default;

            long startTime = Stopwatch.GetTimestamp();

            try
            {
                switch (payload.EventName.ToLowerInvariant())
                {
                    #region Gateway Status

                    case "ready":
                        if (payload.Data is not ReadyPayload ready)
                            throw new InvalidOperationException();

                        await this.OnReadyEventAsync(ready).ConfigureAwait(false);
                        break;

                    case "resumed":
                        await this.OnResumedAsync().ConfigureAwait(false);
                        break;

                    #endregion

                    #region Channel

                    case "channel_create":
                        chn = dat.ToDiscordObject<DiscordChannel>();
                        await this.OnChannelCreateEventAsync(chn).ConfigureAwait(false);
                        break;

                    case "channel_update":
                        await this.OnChannelUpdateEventAsync(dat.ToDiscordObject<DiscordChannel>()).ConfigureAwait(false);
                        break;

                    case "channel_delete":
                        var isPrivate = dat["is_private"]?.ToObject<bool>() ?? false;

                        chn = isPrivate ? dat.ToDiscordObject<DiscordDmChannel>() : dat.ToDiscordObject<DiscordChannel>();
                        await this.OnChannelDeleteEventAsync(chn).ConfigureAwait(false);
                        break;

                    case "channel_pins_update":
                        cid = (ulong)dat["channel_id"];
                        var ts = (string)dat["last_pin_timestamp"];
                        await this.OnChannelPinsUpdateAsync((ulong?)dat["guild_id"], cid, ts != null ? DateTimeOffset.Parse(ts, CultureInfo.InvariantCulture) : default(DateTimeOffset?)).ConfigureAwait(false);
                        break;


                    case "channel_unread_update":
                        await this.OnChannelUnreadUpdate(dat).ConfigureAwait(false);
                        break;

                    case "channel_recipient_add":
                        await this.OnChannelRecipientAddAsync(dat).ConfigureAwait(false);
                        break;

                    case "channel_recipient_remove":
                        await this.OnChannelRecipientRemoveAsync(dat).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Scheduled Guild Events

                    case "guild_scheduled_event_create":
                        var cevt = dat.ToDiscordObject<DiscordScheduledGuildEvent>();
                        await this.OnScheduledGuildEventCreateEventAsync(cevt).ConfigureAwait(false);
                        break;
                    case "guild_scheduled_event_delete":
                        var devt = dat.ToDiscordObject<DiscordScheduledGuildEvent>();
                        await this.OnScheduledGuildEventDeleteEventAsync(devt).ConfigureAwait(false);
                        break;
                    case "guild_scheduled_event_update":
                        var uevt = dat.ToDiscordObject<DiscordScheduledGuildEvent>();
                        await this.OnScheduledGuildEventUpdateEventAsync(uevt).ConfigureAwait(false);
                        break;
                    case "guild_scheduled_event_user_add":
                        gid = (ulong)dat["guild_id"];
                        var uid = (ulong)dat["user_id"];
                        var eid = (ulong)dat["guild_scheduled_event_id"];
                        await this.OnScheduledGuildEventUserAddEventAsync(gid, eid, uid).ConfigureAwait(false);
                        break;
                    case "guild_scheduled_event_user_remove":
                        gid = (ulong)dat["guild_id"];
                        uid = (ulong)dat["user_id"];
                        eid = (ulong)dat["guild_scheduled_event_id"];
                        await this.OnScheduledGuildEventUserRemoveEventAsync(gid, eid, uid).ConfigureAwait(false);
                        break;
                    #endregion

                    #region Guild

                    case "guild_create":

                        rawMembers = (JArray)dat["members"];
                        rawPresences = (JArray)dat["presences"];
                        dat.Remove("members");
                        dat.Remove("presences");

                        await this.OnGuildCreateEventAsync(dat.ToDiscordObject<DiscordGuild>(), rawMembers, rawPresences.ToDiscordObject<IEnumerable<DiscordPresence>>()).ConfigureAwait(false);
                        break;

                    case "guild_update":

                        rawMembers = (JArray)dat["members"];
                        dat.Remove("members");

                        await this.OnGuildUpdateEventAsync(dat.ToDiscordObject<DiscordGuild>(), rawMembers).ConfigureAwait(false);
                        break;

                    case "guild_delete":

                        rawMembers = (JArray)dat["members"];
                        dat.Remove("members");

                        await this.OnGuildDeleteEventAsync(dat.ToDiscordObject<DiscordGuild>(), rawMembers).ConfigureAwait(false);
                        break;

                    case "guild_sync":
                        gid = (ulong)dat["id"];

                        rawMembers = (JArray)dat["members"];
                        rawPresences = (JArray)dat["presences"];
                        dat.Remove("members");
                        dat.Remove("presences");

                        await this.OnGuildSyncEventAsync(this._guilds[gid], (bool)dat["large"], rawMembers, rawPresences.ToDiscordObject<IEnumerable<DiscordPresence>>()).ConfigureAwait(false);
                        break;

                    case "guild_emojis_update":
                        gid = (ulong)dat["guild_id"];
                        var ems = dat["emojis"].ToDiscordObject<IEnumerable<DiscordEmoji>>();
                        await this.OnGuildEmojisUpdateEventAsync(this._guilds[gid], ems).ConfigureAwait(false);
                        break;

                    case "guild_integrations_update":
                        gid = (ulong)dat["guild_id"];

                        // discord fires this event inconsistently if the current user leaves a guild.
                        if (!this._guilds.ContainsKey(gid))
                            return;

                        await this.OnGuildIntegrationsUpdateEventAsync(this._guilds[gid]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Guild Ban

                    case "guild_ban_add":
                        usr = dat["user"].ToDiscordObject<TransportUser>();
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildBanAddEventAsync(usr, this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_ban_remove":
                        usr = dat["user"].ToDiscordObject<TransportUser>();
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildBanRemoveEventAsync(usr, this._guilds[gid]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Guild Member

                    case "guild_member_add":
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildMemberAddEventAsync(dat.ToDiscordObject<TransportMember>(), this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_member_remove":
                        gid = (ulong)dat["guild_id"];
                        usr = dat["user"].ToDiscordObject<TransportUser>();

                        if (!this._guilds.ContainsKey(gid))
                        {
                            // discord fires this event inconsistently if the current user leaves a guild.
                            if (usr.Id != this.CurrentUser.Id)
                                this.Logger.LogError(LoggerEvents.WebSocketReceive, "Could not find {Guild} in guild cache", gid);
                            return;
                        }

                        await this.OnGuildMemberRemoveEventAsync(usr, this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_member_update":
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildMemberUpdateEventAsync(dat.ToDiscordObject<TransportMember>(), this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_members_chunk":
                        await this.OnGuildMembersChunkEventAsync(dat).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Guild Role

                    case "guild_role_create":
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildRoleCreateEventAsync(dat["role"].ToDiscordObject<DiscordRole>(), this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_role_update":
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildRoleUpdateEventAsync(dat["role"].ToDiscordObject<DiscordRole>(), this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_role_delete":
                        gid = (ulong)dat["guild_id"];
                        await this.OnGuildRoleDeleteEventAsync((ulong)dat["role_id"], this._guilds[gid]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Invite

                    case "invite_create":
                        gid = (ulong)dat["guild_id"];
                        cid = (ulong)dat["channel_id"];
                        await this.OnInviteCreateEventAsync(cid, gid, dat.ToDiscordObject<DiscordInvite>()).ConfigureAwait(false);
                        break;

                    case "invite_delete":
                        gid = (ulong)dat["guild_id"];
                        cid = (ulong)dat["channel_id"];
                        await this.OnInviteDeleteEventAsync(cid, gid, dat).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Message

                    case "message_ack":
                        cid = (ulong)dat["channel_id"];
                        var mid = (ulong)dat["message_id"];
                        await this.OnMessageAckEventAsync(this.InternalGetCachedChannel(cid), mid).ConfigureAwait(false);
                        break;

                    case "message_create":
                        rawMbr = dat["member"];

                        if (rawMbr != null)
                            mbr = rawMbr.ToDiscordObject<TransportMember>();

                        var rawRefMsg = dat["referenced_message"];
                        if (rawRefMsg != null && rawRefMsg.HasValues)
                        {
                            if (rawRefMsg.SelectToken("author") != null)
                            {
                                refUsr = rawRefMsg.SelectToken("author").ToDiscordObject<TransportUser>();
                            }

                            if (rawRefMsg.SelectToken("member") != null)
                            {
                                refMbr = rawRefMsg.SelectToken("member").ToDiscordObject<TransportMember>();
                            }
                        }

                        var author = dat["author"].ToDiscordObject<TransportUser>();
                        dat.Remove("author");
                        dat.Remove("member");

                        await this.OnMessageCreateEventAsync(dat.ToDiscordObject<DiscordMessage>(), author, mbr, refUsr, refMbr).ConfigureAwait(false);
                        break;

                    case "message_update":
                        rawMbr = dat["member"];

                        if (rawMbr != null)
                            mbr = rawMbr.ToDiscordObject<TransportMember>();

                        rawRefMsg = dat["referenced_message"];
                        if (rawRefMsg != null && rawRefMsg.HasValues)
                        {
                            if (rawRefMsg.SelectToken("author") != null)
                            {
                                refUsr = rawRefMsg.SelectToken("author").ToDiscordObject<TransportUser>();
                            }

                            if (rawRefMsg.SelectToken("member") != null)
                            {
                                refMbr = rawRefMsg.SelectToken("member").ToDiscordObject<TransportMember>();
                            }
                        }

                        await this.OnMessageUpdateEventAsync(dat.ToDiscordObject<DiscordMessage>(), dat["author"]?.ToDiscordObject<TransportUser>(), mbr, refUsr, refMbr).ConfigureAwait(false);
                        break;

                    // delete event does *not* include message object
                    case "message_delete":
                        await this.OnMessageDeleteEventAsync((ulong)dat["id"], (ulong)dat["channel_id"], (ulong?)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "message_delete_bulk":
                        await this.OnMessageBulkDeleteEventAsync(dat["ids"].ToDiscordObject<ulong[]>(), (ulong)dat["channel_id"], (ulong?)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Message Reaction

                    case "message_reaction_add":
                        rawMbr = dat["member"];

                        if (rawMbr != null)
                            mbr = rawMbr.ToDiscordObject<TransportMember>();

                        await this.OnMessageReactionAddAsync((ulong)dat["user_id"], (ulong)dat["message_id"], (ulong)dat["channel_id"], (ulong?)dat["guild_id"], mbr, dat["emoji"].ToDiscordObject<DiscordEmoji>()).ConfigureAwait(false);
                        break;

                    case "message_reaction_remove":
                        await this.OnMessageReactionRemoveAsync((ulong)dat["user_id"], (ulong)dat["message_id"], (ulong)dat["channel_id"], (ulong?)dat["guild_id"], dat["emoji"].ToDiscordObject<DiscordEmoji>()).ConfigureAwait(false);
                        break;

                    case "message_reaction_remove_all":
                        await this.OnMessageReactionRemoveAllAsync((ulong)dat["message_id"], (ulong)dat["channel_id"], (ulong?)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "message_reaction_remove_emoji":
                        await this.OnMessageReactionRemoveEmojiAsync((ulong)dat["message_id"], (ulong)dat["channel_id"], (ulong)dat["guild_id"], dat["emoji"]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region User/Presence Update

                    case "presence_update":
                        // Presences are a mess. I'm not touching this. ~Velvet
                        await this.OnPresenceUpdateEventAsync(dat, (JObject)dat["user"]).ConfigureAwait(false);
                        break;

                    case "user_settings_update":
                        await this.OnUserSettingsUpdateEventAsync(dat).ConfigureAwait(false);
                        break;

                    case "user_settings_proto_update":
                        await this.OnUserSettingsProtoUpdateEventAsync(dat).ConfigureAwait(false);
                        break;

                    case "user_guild_settings_update":
                        await this.OnUserGuildSettingsUpdated(dat).ConfigureAwait(false);
                        break;

                    case "user_update":
                        await this.OnUserUpdateEventAsync(dat.ToDiscordObject<TransportUser>()).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Relationships

                    case "relationship_add":
                        await this.OnRelationshipAddAsync(dat).ConfigureAwait(false);
                        break;

                    case "relationship_remove":
                        await this.OnRelationshipRemoveAsync(dat).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Voice

                    case "voice_state_update":
                        await this.OnVoiceStateUpdateEventAsync(dat).ConfigureAwait(false);
                        break;

                    case "voice_server_update":
                        gid = (ulong)dat["guild_id"];
                        await this.OnVoiceServerUpdateEventAsync((string)dat["endpoint"], (string)dat["token"], this._guilds[gid]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Thread

                    case "thread_create":
                        thread = dat.ToDiscordObject<DiscordThreadChannel>();
                        await this.OnThreadCreateEventAsync(thread, thread.IsNew).ConfigureAwait(false);
                        break;

                    case "thread_update":
                        thread = dat.ToDiscordObject<DiscordThreadChannel>();
                        await this.OnThreadUpdateEventAsync(thread).ConfigureAwait(false);
                        break;

                    case "thread_delete":
                        thread = dat.ToDiscordObject<DiscordThreadChannel>();
                        await this.OnThreadDeleteEventAsync(thread).ConfigureAwait(false);
                        break;

                    case "thread_list_sync":
                        gid = (ulong)dat["guild_id"]; //get guild
                        await this.OnThreadListSyncEventAsync(this._guilds[gid], dat).ConfigureAwait(false);
                        break;

                    case "thread_member_update":
                        await this.OnThreadMemberUpdateEventAsync(dat.ToDiscordObject<DiscordThreadChannelMember>()).ConfigureAwait(false);
                        break;

                    case "thread_members_update":
                        gid = (ulong)dat["guild_id"];
                        await this.OnThreadMembersUpdateEventAsync(this._guilds[gid], (ulong)dat["id"], dat["added_members"]?.ToDiscordObject<IReadOnlyList<DiscordThreadChannelMember>>(), dat["removed_member_ids"]?.ToDiscordObject<IReadOnlyList<ulong?>>(), (int)dat["member_count"]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Interaction/Integration/Application

                    case "interaction_create":

                        rawMbr = dat["member"];

                        if (rawMbr != null)
                        {
                            mbr = dat["member"].ToDiscordObject<TransportMember>();
                            usr = mbr.User;
                        }
                        else
                        {
                            usr = dat["user"]?.ToDiscordObject<TransportUser>();
                        }

                        // Re: Removing re-serialized data: This one is probably fine?
                        // The user on the object is marked with [JsonIgnore].

                        cid = dat["channel_id"]?.ToObject<ulong>() ?? 0;
                        await this.OnInteractionCreateAsync((ulong?)dat["guild_id"], cid, usr, mbr, dat.ToDiscordObject<DiscordInteraction>()).ConfigureAwait(false);
                        break;

                    case "application_command_create":
                        await this.OnApplicationCommandCreateAsync(dat.ToDiscordObject<DiscordApplicationCommand>(), (ulong?)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "application_command_update":
                        await this.OnApplicationCommandUpdateAsync(dat.ToDiscordObject<DiscordApplicationCommand>(), (ulong?)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "application_command_permissions_update":
                        await this.OnApplicationCommandPermissionsUpdateAsync(dat).ConfigureAwait(false);
                        break;

                    case "application_command_delete":
                        await this.OnApplicationCommandDeleteAsync(dat.ToDiscordObject<DiscordApplicationCommand>(), (ulong?)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "integration_create":
                        await this.OnIntegrationCreateAsync(dat.ToDiscordObject<DiscordIntegration>(), (ulong)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "integration_update":
                        await this.OnIntegrationUpdateAsync(dat.ToDiscordObject<DiscordIntegration>(), (ulong)dat["guild_id"]).ConfigureAwait(false);
                        break;

                    case "integration_delete":
                        await this.OnIntegrationDeleteAsync((ulong)dat["id"], (ulong)dat["guild_id"], (ulong?)dat["application_id"]).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Stage Instance

                    case "stage_instance_create":
                        await this.OnStageInstanceCreateAsync(dat.ToDiscordObject<DiscordStageInstance>()).ConfigureAwait(false);
                        break;

                    case "stage_instance_update":
                        await this.OnStageInstanceUpdateAsync(dat.ToDiscordObject<DiscordStageInstance>()).ConfigureAwait(false);
                        break;

                    case "stage_instance_delete":
                        await this.OnStageInstanceDeleteAsync(dat.ToDiscordObject<DiscordStageInstance>()).ConfigureAwait(false);
                        break;

                    #endregion

                    #region Misc

                    case "gift_code_update": //Not supposed to be dispatched to bots
                        break;

                    case "embedded_activity_update": //Not supposed to be dispatched to bots
                        break;

                    case "typing_start":
                        cid = (ulong)dat["channel_id"];
                        rawMbr = dat["member"];

                        if (rawMbr != null)
                            mbr = rawMbr.ToDiscordObject<TransportMember>();

                        await this.OnTypingStartEventAsync((ulong)dat["user_id"], cid, this.InternalGetCachedChannel(cid), (ulong?)dat["guild_id"], Utilities.GetDateTimeOffset((long)dat["timestamp"]), mbr).ConfigureAwait(false);
                        break;

                    case "webhooks_update":
                        gid = (ulong)dat["guild_id"];
                        cid = (ulong)dat["channel_id"];
                        await this.OnWebhooksUpdateAsync(this._guilds[gid].GetChannel(cid), this._guilds[gid]).ConfigureAwait(false);
                        break;

                    case "guild_stickers_update":
                        var strs = dat["stickers"].ToDiscordObject<IEnumerable<DiscordMessageSticker>>();
                        await this.OnStickersUpdatedAsync(strs, dat).ConfigureAwait(false);
                        break;

                    default:
                        await this.OnUnknownEventAsync(payload).ConfigureAwait(false);
                        if (this.Configuration.LogUnknownEvents)
                            this.Logger.LogWarning(LoggerEvents.WebSocketReceive, "Unknown event: {EventName}\npayload: {@Payload}", payload.EventName, payload.Data);
                        break;

                        #endregion
                }
            }
            finally
            {
                var endTime = Stopwatch.GetTimestamp();
                var deltaTime = TimeSpan.FromSeconds((double)(endTime - startTime) / (double)Stopwatch.Frequency);

                if (deltaTime > TimeSpan.FromMilliseconds(50))
                {
                    this.Logger.LogError(LoggerEvents.SlowDispatch, "Dispatch of event \'{EventName}\' took {EventMs}ms! < 50ms target!", payload.EventName, deltaTime.TotalMilliseconds);
                }
            }
        }


        #endregion

        #region Events

        #region Gateway

        internal async Task OnReadyEventAsync(ReadyPayload ready)
        {
            //ready.CurrentUser.Discord = this;

            var rusr = ready.CurrentUser;
            this.CurrentUser.Username = rusr.Username;
            this.CurrentUser.Discriminator = rusr.Discriminator;
            this.CurrentUser.AvatarHash = rusr.AvatarHash;
            this.CurrentUser.MfaEnabled = rusr.MfaEnabled;
            this.CurrentUser.Verified = rusr.Verified;
            this.CurrentUser.IsBot = rusr.IsBot;

            this.GatewayVersion = ready.GatewayVersion;

            this.UserSettings = ready.UserSettings;
            this.UserSettingsProto = ready.UserSettingsProto;

            if (!string.IsNullOrEmpty(ready.AuthToken))
            {
                this.Configuration.Token = ready.AuthToken;
                this.ApiClient.UpdateConfiguration(this.Configuration);

                this.Logger.LogWarning("Discord provided an updated auth token! Please make sure you're saving this!");

                await this._authTokenUpdate.InvokeAsync(this, new AuthTokenUpdatedEventArgs() { Token = ready.AuthToken });
            }

            this._sessionId = ready.SessionId;
            var users = ready.Users.Select(u => this.UpdateUserCache(new DiscordUser(u) { Discord = this }))
                .ToFrozenDictionary(k => k.Id);

            this._privateChannels.Clear();
            foreach (var channel in ready.DmChannels)
            {
                channel.Discord = this;

                //xdc._recipients =
                //    .Select(xtu => this.InternalGetCachedUser(xtu.Id) ?? new DiscordUser(xtu) { Discord = this })
                //    .ToList();

                var recipients = new List<DiscordUser>();
                if (channel.RecipientIds != null)
                {
                    foreach (var idToken in channel.RecipientIds)
                        recipients.Add(users[(ulong)idToken]);
                }
                else
                {
                    var recips_raw = channel.InternalRecipients;
                    foreach (var xr in recips_raw)
                    {
                        var xu = new DiscordUser(xr) { Discord = this };
                        xu = this.UpdateUserCache(xu);

                        recipients.Add(xu);
                    }
                }
                channel.Recipients = recipients;


                this._privateChannels[channel.Id] = channel;
            }

            this._guilds.Clear();

            var guilds = ready.Guilds;
            for (var i = 0; i < guilds.Count; i++)
            {
                var guild = guilds[i];
                var merged_members = ready.MergedMembers[i];

                guild.Discord = this;
                guild._channels ??= new ConcurrentDictionary<ulong, DiscordChannel>();
                guild._threads ??= new ConcurrentDictionary<ulong, DiscordThreadChannel>();

                foreach (var (_, xc) in guild.Channels)
                {
                    xc.GuildId = guild.Id;
                    xc.Discord = this;
                    foreach (var xo in xc._permissionOverwrites)
                    {
                        xo.Discord = this;
                        xo._channel_id = xc.Id;
                    }
                }

                foreach (var (_, xt) in guild.Threads)
                {
                    xt.GuildId = guild.Id;
                    xt.Discord = this;
                }

                guild._roles ??= new ConcurrentDictionary<ulong, DiscordRole>();

                foreach (var (_, xr) in guild.Roles)
                {
                    xr.Discord = this;
                    xr._guild_id = guild.Id;
                }

                guild._members?.Clear();
                guild._members ??= new ConcurrentDictionary<ulong, DiscordMember>();

                if (merged_members != null)
                {
                    foreach (var xtm in merged_members)
                    {
                        guild._members[xtm.UserId] = new DiscordMember(xtm) { Discord = this, _guild_id = guild.Id };
                    }
                }

                guild._emojis ??= new ConcurrentDictionary<ulong, DiscordEmoji>();

                foreach (var (_, xe) in guild.Emojis)
                    xe.Discord = this;

                guild._voiceStates ??= new ConcurrentDictionary<ulong, DiscordVoiceState>();

                foreach (var (_, xvs) in guild.VoiceStates)
                    xvs.Discord = this;

                this._guilds[guild.Id] = guild;
            }


            foreach (var item in ready.UserGuildSettings)
            {
                _userGuildSettings[item.GuildId ?? default] = item;
            }

            foreach (var relationship in ready.Relationships ?? Array.Empty<DiscordRelationship>())
            {
                relationship.Discord = this;

                //var user = this.UpdateUserCache(new DiscordUser(relationship.InternalUser));
                if (_relationships.TryGetValue(relationship.Id, out var oldRel))
                {
                    oldRel.RelationshipType = relationship.RelationshipType;
                }
                else
                {
                    _relationships.TryAdd(relationship.Id, relationship);
                }
            }

            foreach (var dat in ready.ReadStates ?? Array.Empty<DiscordReadState>())
            {
                if (this._readStates.TryGetValue(dat.Id, out var state))
                {
                    state.LastMessageId = dat.LastMessageId;
                    state.LastPinTimestamp = dat.LastPinTimestamp;
                    state.MentionCount = dat.MentionCount;
                }
                else
                {
                    dat.Discord = this;
                    this._readStates[dat.Id] = dat;
                }
            }

            if (ready.MergedPresences != null)
            {
                var friends = ready.MergedPresences.Friends;
                foreach (var presence in friends)
                {
                    await this.OnPresenceUpdateEventAsync((JObject)presence, (JObject)presence["user"], true).ConfigureAwait(false);
                }

                // presences are technically per guild but we dont really handle that properly :sob:
                var guildPresences = ready.MergedPresences.Guilds;
                if (guildPresences != null)
                {
                    for (var i = 0; i < guildPresences.Length; i++)
                    {
                        var guildPresence = guildPresences[i];
                        var guild = guilds[i];
                        foreach (var presence in guildPresence)
                        {
                            await this.OnPresenceUpdateEventAsync((JObject)presence, (JObject)presence["user"], true).ConfigureAwait(false);
                        }
                    }
                }
            }

            await this._ready.InvokeAsync(this, new ReadyEventArgs()).ConfigureAwait(false);
        }

        internal Task OnResumedAsync()
        {
            this.Logger.LogInformation(LoggerEvents.SessionUpdate, "Session resumed");
            return this._resumed.InvokeAsync(this, new ResumedEventArgs());
        }

        #endregion

        #region Channel

        internal async Task OnChannelCreateEventAsync(DiscordChannel channel)
        {
            channel.Discord = this;
            foreach (var xo in channel._permissionOverwrites)
            {
                xo.Discord = this;
                xo._channel_id = channel.Id;
            }

            if (channel.GuildId != null)
                this._guilds[channel.GuildId.Value]._channels[channel.Id] = channel;
            else 
                this._privateChannels[channel.Id] = (DiscordDmChannel)channel;

            await this._channelCreated.InvokeAsync(this, new ChannelCreateEventArgs { Channel = channel, Guild = channel.Guild }).ConfigureAwait(false);
        }

        internal async Task OnChannelUpdateEventAsync(DiscordChannel channel)
        {
            if (channel == null)
                return;

            channel.Discord = this;

            var gld = channel.Guild;

            var channel_new = this.InternalGetCachedChannel(channel.Id);
            DiscordChannel channel_old = null;

            if (channel_new != null)
            {
                channel_old = new DiscordChannel
                {
                    Bitrate = channel_new.Bitrate,
                    Discord = this,
                    GuildId = channel_new.GuildId,
                    Id = channel_new.Id,
                    //IsPrivate = channel_new.IsPrivate,
                    LastMessageId = channel_new.LastMessageId,
                    Name = channel_new.Name,
                    _permissionOverwrites = new List<DiscordOverwrite>(channel_new._permissionOverwrites),
                    Position = channel_new.Position,
                    Topic = channel_new.Topic,
                    Type = channel_new.Type,
                    UserLimit = channel_new.UserLimit,
                    ParentId = channel_new.ParentId,
                    IsNSFW = channel_new.IsNSFW,
                    PerUserRateLimit = channel_new.PerUserRateLimit,
                    RtcRegionId = channel_new.RtcRegionId,
                    QualityMode = channel_new.QualityMode
                };

                channel_new.Bitrate = channel.Bitrate;
                channel_new.Name = channel.Name;
                channel_new.Position = channel.Position;
                channel_new.Topic = channel.Topic;
                channel_new.UserLimit = channel.UserLimit;
                channel_new.ParentId = channel.ParentId;
                channel_new.IsNSFW = channel.IsNSFW;
                channel_new.PerUserRateLimit = channel.PerUserRateLimit;
                channel_new.Type = channel.Type;
                channel_new.RtcRegionId = channel.RtcRegionId;
                channel_new.QualityMode = channel.QualityMode;

                channel_new._permissionOverwrites.Clear();

                foreach (var po in channel._permissionOverwrites)
                {
                    po.Discord = this;
                    po._channel_id = channel.Id;
                }

                channel_new._permissionOverwrites.AddRange(channel._permissionOverwrites);
            }
            else if (gld != null)
            {
                gld._channels[channel.Id] = channel;
            }

            await this._channelUpdated.InvokeAsync(this, new ChannelUpdateEventArgs { ChannelAfter = channel_new, Guild = gld, ChannelBefore = channel_old }).ConfigureAwait(false);
        }

        internal async Task OnChannelDeleteEventAsync(DiscordChannel channel)
        {
            if (channel == null)
                return;

            channel.Discord = this;

            //if (channel.IsPrivate)
            if (channel.Type == ChannelType.Group || channel.Type == ChannelType.Private)
            {
                var dmChannel = channel as DiscordDmChannel;

                _ = this._privateChannels.TryRemove(dmChannel.Id, out _);

                await this._dmChannelDeleted.InvokeAsync(this, new DmChannelDeleteEventArgs { Channel = dmChannel }).ConfigureAwait(false);
            }
            else
            {
                var gld = channel.Guild;

                if (gld._channels.TryRemove(channel.Id, out var cachedChannel)) channel = cachedChannel;

                await this._channelDeleted.InvokeAsync(this, new ChannelDeleteEventArgs { Channel = channel, Guild = gld }).ConfigureAwait(false);
            }
        }

        internal async Task OnChannelPinsUpdateAsync(ulong? guildId, ulong channelId, DateTimeOffset? lastPinTimestamp)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);

            if (channel == null)
            {
                channel = new DiscordDmChannel
                {
                    Id = channelId,
                    Discord = this,
                    Type = ChannelType.Private,
                    Recipients = Array.Empty<DiscordUser>()
                };

                var chn = (DiscordDmChannel)channel;

                this._privateChannels[channelId] = chn;
            }

            var ea = new ChannelPinsUpdateEventArgs
            {
                Guild = guild,
                Channel = channel,
                LastPinTimestamp = lastPinTimestamp
            };
            await this._channelPinsUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        private async Task OnChannelUnreadUpdate(JObject dat)
        {
            var readStateDict = new Dictionary<ulong, DiscordReadState>();
            var guildId = dat["guild_id"]?.ToObject<ulong?>();
            var readStates = dat["channel_unread_updates"].ToDiscordObject<IEnumerable<DiscordReadState>>();
            foreach (var state in readStates)
            {
                state.Discord = this;
                var newReadState = this._readStates.AddOrUpdate(state.Id, state, (id, old) =>
                {
                    old.LastMessageId = state.LastMessageId;
                    old.LastPinTimestamp = state.LastPinTimestamp;
                    old.MentionCount = state.MentionCount;
                    return old;
                });

                readStateDict.Add(state.Id, state);
            }

            var ev = new ChannelUnreadUpdateEventArgs() { GuildId = guildId, ReadStates = readStateDict.ToFrozenDictionary() };
            await this._channelUnreadUpdate.InvokeAsync(this, ev);
        }

        private async Task OnChannelRecipientAddAsync(JObject dat)
        {
            var channelId = dat["channel_id"]?.ToObject<ulong?>();
            if (channelId == null) return;
            var tUser = dat["user"].ToDiscordObject<TransportUser>();
            var user = new DiscordUser(tUser)
            {
                Discord = this
            };
            user = this.UpdateUserCache(user);
            this._privateChannels.TryGetValue(channelId ?? 0, out DiscordDmChannel channel);
            var recipients = new List<DiscordUser>();
            foreach (var u in channel.Recipients.Where(x => x.Id != user.Id))
            {
                recipients.Add(u);
            }
            recipients.Add(user);
            channel.RecipientIds = recipients.Select(x => x.Id).ToList().AsReadOnly();
            channel.Recipients = recipients;
            if (channel.InternalRecipients != null)
            {
                var internalRecipients = channel.InternalRecipients.Where(x => x.Id != tUser.Id).ToList();
                internalRecipients.Add(tUser);
                channel.InternalRecipients = internalRecipients.AsReadOnly();
            }
            var ev = new ChannelRecipientAddedEventArgs { Channel = channel, User = user };
            await this._channelRecipientAdded.InvokeAsync(this, ev);
        }

        private async Task OnChannelRecipientRemoveAsync(JObject dat)
        {
            var channelId = dat["channel_id"]?.ToObject<ulong?>();
            if (channelId == null) return;
            var tUser = dat["user"].ToDiscordObject<TransportUser>();
            var user = new DiscordUser(tUser)
            {
                Discord = this
            };
            user = this.UpdateUserCache(user);
            this._privateChannels.TryGetValue(channelId ?? 0, out DiscordDmChannel channel);
            var recipients = new List<DiscordUser>();
            foreach (var u in channel.Recipients.Where(x => x.Id != user.Id))
            {
                recipients.Add(u);
            }
            channel.RecipientIds = recipients.Select(x => x.Id).ToList().AsReadOnly();
            channel.Recipients = recipients;
            if (channel.InternalRecipients != null)
            {
                var internalRecipients = channel.InternalRecipients.Where(x => x.Id != tUser.Id).ToList();
                channel.InternalRecipients = internalRecipients.AsReadOnly();
            }
            var ev = new ChannelRecipientRemovedEventArgs { Channel = channel, User = user };
            await this._channelRecipientRemoved.InvokeAsync(this, ev);
        }

        #endregion

        #region Scheduled Guild Events

        private async Task OnScheduledGuildEventCreateEventAsync(DiscordScheduledGuildEvent evt)
        {
            evt.Discord = this;

            if (evt.Creator != null)
            {
                evt.Creator.Discord = this;
                this.UpdateUserCache(evt.Creator);
            }

            evt.Guild._scheduledEvents[evt.Id] = evt;

            await this._scheduledGuildEventCreated.InvokeAsync(this, new ScheduledGuildEventCreateEventArgs { Event = evt }).ConfigureAwait(false);
        }

        private async Task OnScheduledGuildEventDeleteEventAsync(DiscordScheduledGuildEvent evt)
        {
            var guild = this.InternalGetCachedGuild(evt.GuildId);

            if (guild == null) // ??? //
                return;

            guild._scheduledEvents.TryRemove(evt.Id, out _);

            evt.Discord = this;

            if (evt.Creator != null)
            {
                evt.Creator.Discord = this;
                this.UpdateUserCache(evt.Creator);
            }

            await this._scheduledGuildEventDeleted.InvokeAsync(this, new ScheduledGuildEventDeleteEventArgs { Event = evt }).ConfigureAwait(false);
        }

        private async Task OnScheduledGuildEventUpdateEventAsync(DiscordScheduledGuildEvent evt)
        {
            evt.Discord = this;

            if (evt.Creator != null)
            {
                evt.Creator.Discord = this;
                this.UpdateUserCache(evt.Creator);
            }

            var guild = this.InternalGetCachedGuild(evt.GuildId);
            guild._scheduledEvents.TryGetValue(evt.GuildId, out var oldEvt);

            evt.Guild._scheduledEvents[evt.Id] = evt;

            if (evt.Status is ScheduledGuildEventStatus.Completed)
                await this._scheduledGuildEventCompleted.InvokeAsync(this, new ScheduledGuildEventCompletedEventArgs() { Event = evt }).ConfigureAwait(false);
            else
                await this._scheduledGuildEventUpdated.InvokeAsync(this, new ScheduledGuildEventUpdateEventArgs() { EventBefore = oldEvt, EventAfter = evt }).ConfigureAwait(false);
        }

        private async Task OnScheduledGuildEventUserAddEventAsync(ulong guildId, ulong eventId, ulong userId)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var evt = guild._scheduledEvents.GetOrAdd(eventId, new DiscordScheduledGuildEvent()
            {
                Id = eventId,
                GuildId = guildId,
                Discord = this,
                UserCount = 0
            });

            evt.UserCount++;

            var user =
                guild.Members.TryGetValue(userId, out var mbr) ? mbr :
                this.GetCachedOrEmptyUserInternal(userId) ?? new DiscordUser() {Id = userId , Discord = this};

            await this._scheduledGuildEventUserAdded.InvokeAsync(this, new ScheduledGuildEventUserAddEventArgs() { Event = evt, User = user }).ConfigureAwait(false);
        }

        private async Task OnScheduledGuildEventUserRemoveEventAsync(ulong guildId, ulong eventId, ulong userId)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var evt = guild._scheduledEvents.GetOrAdd(eventId, new DiscordScheduledGuildEvent()
            {
                Id = eventId,
                GuildId = guildId,
                Discord = this,
                UserCount = 0
            });

            evt.UserCount = evt.UserCount is 0 ? 0 : evt.UserCount - 1;

            var user =
                guild.Members.TryGetValue(userId, out var mbr) ? mbr :
                this.GetCachedOrEmptyUserInternal(userId) ?? new DiscordUser() {Id = userId , Discord = this};

            await this._scheduledGuildEventUserRemoved.InvokeAsync(this, new ScheduledGuildEventUserRemoveEventArgs() { Event = evt, User = user }).ConfigureAwait(false);
        }

        #endregion

        #region Guild

        internal async Task OnGuildCreateEventAsync(DiscordGuild guild, JArray rawMembers, IEnumerable<DiscordPresence> presences)
        {
            if (presences != null)
            {
                foreach (var xp in presences)
                {
                    xp.Discord = this;
                    xp.GuildId = guild.Id;
                    xp.Activity = new DiscordActivity(xp.RawActivity);
                    if (xp.RawActivities != null)
                    {
                        xp._internalActivities = new DiscordActivity[xp.RawActivities.Length];
                        for (var i = 0; i < xp.RawActivities.Length; i++)
                            xp._internalActivities[i] = new DiscordActivity(xp.RawActivities[i]);
                    }
                    this._presences[xp.User.Id] = xp;
                }
            }

            var exists = this._guilds.TryGetValue(guild.Id, out var foundGuild);

            guild.Discord = this;
            guild.IsUnavailable = false;
            var eventGuild = guild;

            if (exists)
                guild = foundGuild;

            guild._channels ??= new ConcurrentDictionary<ulong, DiscordChannel>();
            guild._threads ??= new ConcurrentDictionary<ulong, DiscordThreadChannel>();
            guild._roles ??= new ConcurrentDictionary<ulong, DiscordRole>();
            guild._emojis ??= new ConcurrentDictionary<ulong, DiscordEmoji>();
            guild._stickers ??= new ConcurrentDictionary<ulong, DiscordMessageSticker>();
            guild._voiceStates ??= new ConcurrentDictionary<ulong, DiscordVoiceState>();
            guild._members ??= new ConcurrentDictionary<ulong, DiscordMember>();
            guild._stageInstances ??= new ConcurrentDictionary<ulong, DiscordStageInstance>();
            guild._scheduledEvents ??= new ConcurrentDictionary<ulong, DiscordScheduledGuildEvent>();

            this.UpdateCachedGuild(eventGuild, rawMembers);

            guild.JoinedAt = eventGuild.JoinedAt;
            guild.IsLarge = eventGuild.IsLarge;
            guild.MemberCount = Math.Max(eventGuild.MemberCount, guild._members.Count);
            guild.IsUnavailable = eventGuild.IsUnavailable;
            guild.PremiumSubscriptionCount = eventGuild.PremiumSubscriptionCount;
            guild.PremiumTier = eventGuild.PremiumTier;
            guild.Banner = eventGuild.Banner;
            guild.VanityUrlCode = eventGuild.VanityUrlCode;
            guild.Description = eventGuild.Description;
            guild.IsNSFW = eventGuild.IsNSFW;


            foreach (var kvp in eventGuild._voiceStates ??= new())
                guild._voiceStates[kvp.Key] = kvp.Value;

            foreach (var xe in guild._scheduledEvents.Values)
            {
                xe.Discord = this;

                if (xe.Creator != null)
                    xe.Creator.Discord = this;
            }

            foreach (var xc in guild._channels.Values)
            {
                xc.GuildId = guild.Id;
                xc.Discord = this;
                foreach (var xo in xc._permissionOverwrites)
                {
                    xo.Discord = this;
                    xo._channel_id = xc.Id;
                }
            }
            foreach (var xt in guild._threads.Values)
            {
                xt.GuildId = guild.Id;
                xt.Discord = this;
            }
            foreach (var xe in guild._emojis.Values)
                xe.Discord = this;
            foreach (var xs in guild._stickers.Values)
                xs.Discord = this;
            foreach (var xvs in guild._voiceStates.Values)
                xvs.Discord = this;
            foreach (var xr in guild._roles.Values)
            {
                xr.Discord = this;
                xr._guild_id = guild.Id;
            }

            foreach (var instance in guild._stageInstances.Values)
                instance.Discord = this;

            var old = Volatile.Read(ref this._guildDownloadCompleted);
            var dcompl = this._guilds.Values.All(xg => !xg.IsUnavailable);
            Volatile.Write(ref this._guildDownloadCompleted, dcompl);

            if (exists)
                await this._guildAvailable.InvokeAsync(this, new GuildCreateEventArgs { Guild = guild }).ConfigureAwait(false);
            else
                await this._guildCreated.InvokeAsync(this, new GuildCreateEventArgs { Guild = guild }).ConfigureAwait(false);

            if (dcompl && !old)
                await this._guildDownloadCompletedEv.InvokeAsync(this, new GuildDownloadCompletedEventArgs(this.Guilds)).ConfigureAwait(false);
        }

        internal async Task OnGuildUpdateEventAsync(DiscordGuild guild, JArray rawMembers)
        {
            DiscordGuild oldGuild;

            if (!this._guilds.ContainsKey(guild.Id))
            {
                this._guilds[guild.Id] = guild;
                oldGuild = null;
            }
            else
            {
                var gld = this._guilds[guild.Id];

                oldGuild = new DiscordGuild
                {
                    Discord = gld.Discord,
                    Name = gld.Name,
                    _afkChannelId = gld._afkChannelId,
                    AfkTimeout = gld.AfkTimeout,
                    DefaultMessageNotifications = gld.DefaultMessageNotifications,
                    ExplicitContentFilter = gld.ExplicitContentFilter,
                    Features = gld.Features,
                    IconHash = gld.IconHash,
                    Id = gld.Id,
                    IsLarge = gld.IsLarge,
                    _isSynced = gld._isSynced,
                    IsUnavailable = gld.IsUnavailable,
                    JoinedAt = gld.JoinedAt,
                    MemberCount = gld.MemberCount,
                    MaxMembers = gld.MaxMembers,
                    MaxPresences = gld.MaxPresences,
                    ApproximateMemberCount = gld.ApproximateMemberCount,
                    ApproximatePresenceCount = gld.ApproximatePresenceCount,
                    MaxVideoChannelUsers = gld.MaxVideoChannelUsers,
                    DiscoverySplashHash = gld.DiscoverySplashHash,
                    PreferredLocale = gld.PreferredLocale,
                    MfaLevel = gld.MfaLevel,
                    OwnerId = gld.OwnerId,
                    SplashHash = gld.SplashHash,
                    _systemChannelId = gld._systemChannelId,
                    SystemChannelFlags = gld.SystemChannelFlags,
                    WidgetEnabled = gld.WidgetEnabled,
                    _widgetChannelId = gld._widgetChannelId,
                    VerificationLevel = gld.VerificationLevel,
                    _rulesChannelId = gld._rulesChannelId,
                    _publicUpdatesChannelId = gld._publicUpdatesChannelId,
                    _voiceRegionId = gld._voiceRegionId,
                    PremiumProgressBarEnabled = gld.PremiumProgressBarEnabled,
                    IsNSFW = gld.IsNSFW,
                    _channels = new ConcurrentDictionary<ulong, DiscordChannel>(),
                    _threads = new ConcurrentDictionary<ulong, DiscordThreadChannel>(),
                    _emojis = new ConcurrentDictionary<ulong, DiscordEmoji>(),
                    _members = new ConcurrentDictionary<ulong, DiscordMember>(),
                    _roles = new ConcurrentDictionary<ulong, DiscordRole>(),
                    _voiceStates = new ConcurrentDictionary<ulong, DiscordVoiceState>()
                };

                foreach (var kvp in gld._channels ??= new()) oldGuild._channels[kvp.Key] = kvp.Value;
                foreach (var kvp in gld._threads ??= new()) oldGuild._threads[kvp.Key] = kvp.Value;
                foreach (var kvp in gld._emojis ??= new()) oldGuild._emojis[kvp.Key] = kvp.Value;
                foreach (var kvp in gld._roles ??= new()) oldGuild._roles[kvp.Key] = kvp.Value;
                foreach (var kvp in gld._voiceStates ??= new()) oldGuild._voiceStates[kvp.Key] = kvp.Value;
                foreach (var kvp in gld._members ??= new()) oldGuild._members[kvp.Key] = kvp.Value;
            }

            guild.Discord = this;
            guild.IsUnavailable = false;
            var eventGuild = guild;
            guild = this._guilds[eventGuild.Id];

            guild._channels ??= new ConcurrentDictionary<ulong, DiscordChannel>();
            guild._threads ??= new ConcurrentDictionary<ulong, DiscordThreadChannel>();
            guild._roles ??= new ConcurrentDictionary<ulong, DiscordRole>();
            guild._emojis ??= new ConcurrentDictionary<ulong, DiscordEmoji>();
            guild._voiceStates ??= new ConcurrentDictionary<ulong, DiscordVoiceState>();
            guild._members ??= new ConcurrentDictionary<ulong, DiscordMember>();

            this.UpdateCachedGuild(eventGuild, rawMembers);

            foreach (var xc in guild._channels.Values)
            {
                xc.GuildId = guild.Id;
                xc.Discord = this;
                foreach (var xo in xc._permissionOverwrites)
                {
                    xo.Discord = this;
                    xo._channel_id = xc.Id;
                }
            }
            foreach (var xc in guild._threads.Values)
            {
                xc.GuildId = guild.Id;
                xc.Discord = this;
            }
            foreach (var xe in guild._emojis.Values)
                xe.Discord = this;
            foreach (var xvs in guild._voiceStates.Values)
                xvs.Discord = this;
            foreach (var xr in guild._roles.Values)
            {
                xr.Discord = this;
                xr._guild_id = guild.Id;
            }

            await this._guildUpdated.InvokeAsync(this, new GuildUpdateEventArgs { GuildBefore = oldGuild, GuildAfter = guild }).ConfigureAwait(false);
        }

        internal async Task OnGuildDeleteEventAsync(DiscordGuild guild, JArray rawMembers)
        {
            if (guild.IsUnavailable)
            {
                if (!this._guilds.TryGetValue(guild.Id, out var gld))
                    return;

                gld.IsUnavailable = true;

                await this._guildUnavailable.InvokeAsync(this, new GuildDeleteEventArgs { Guild = guild, Unavailable = true }).ConfigureAwait(false);
            }
            else
            {
                if (!this._guilds.TryRemove(guild.Id, out var gld))
                    return;

                await this._guildDeleted.InvokeAsync(this, new GuildDeleteEventArgs { Guild = gld }).ConfigureAwait(false);
            }
        }

        internal async Task OnGuildSyncEventAsync(DiscordGuild guild, bool isLarge, JArray rawMembers, IEnumerable<DiscordPresence> presences)
        {
            presences = presences.Select(xp => { xp.Discord = this; xp.Activity = new DiscordActivity(xp.RawActivity); return xp; });
            foreach (var xp in presences)
                this._presences[xp.InternalUser.Id] = xp;

            guild._isSynced = true;
            guild.IsLarge = isLarge;

            this.UpdateCachedGuild(guild, rawMembers);

            await this._guildAvailable.InvokeAsync(this, new GuildCreateEventArgs { Guild = guild }).ConfigureAwait(false);
        }

        internal async Task OnGuildEmojisUpdateEventAsync(DiscordGuild guild, IEnumerable<DiscordEmoji> newEmojis)
        {
            var oldEmojis = new ConcurrentDictionary<ulong, DiscordEmoji>(guild._emojis);
            guild._emojis.Clear();

            foreach (var emoji in newEmojis)
            {
                emoji.Discord = this;
                guild._emojis[emoji.Id] = emoji;
            }

            var ea = new GuildEmojisUpdateEventArgs
            {
                Guild = guild,
                EmojisAfter = guild.Emojis,
                EmojisBefore = oldEmojis
            };
            await this._guildEmojisUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildIntegrationsUpdateEventAsync(DiscordGuild guild)
        {
            var ea = new GuildIntegrationsUpdateEventArgs
            {
                Guild = guild
            };
            await this._guildIntegrationsUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Guild Ban

        internal async Task OnGuildBanAddEventAsync(TransportUser user, DiscordGuild guild)
        {
            var usr = new DiscordUser(user) { Discord = this };
            usr = this.UpdateUserCache(usr);

            if (!guild.Members.TryGetValue(user.Id, out var mbr))
                mbr = new DiscordMember(usr) { Discord = this, _guild_id = guild.Id };
            var ea = new GuildBanAddEventArgs
            {
                Guild = guild,
                Member = mbr
            };
            await this._guildBanAdded.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildBanRemoveEventAsync(TransportUser user, DiscordGuild guild)
        {
            var usr = new DiscordUser(user) { Discord = this };
            usr = this.UpdateUserCache(usr);

            if (!guild.Members.TryGetValue(user.Id, out var mbr))
                mbr = new DiscordMember(usr) { Discord = this, _guild_id = guild.Id };
            var ea = new GuildBanRemoveEventArgs
            {
                Guild = guild,
                Member = mbr
            };
            await this._guildBanRemoved.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Guild Member

        internal async Task OnGuildMemberAddEventAsync(TransportMember member, DiscordGuild guild)
        {
            var usr = new DiscordUser(member.User) { Discord = this };
            usr = this.UpdateUserCache(usr);

            var mbr = new DiscordMember(member)
            {
                Discord = this,
                _guild_id = guild.Id
            };

            guild._members[mbr.Id] = mbr;
            guild.MemberCount++;

            var ea = new GuildMemberAddEventArgs
            {
                Guild = guild,
                Member = mbr
            };
            await this._guildMemberAdded.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildMemberRemoveEventAsync(TransportUser user, DiscordGuild guild)
        {
            var usr = new DiscordUser(user);

            if (!guild._members.TryRemove(user.Id, out var mbr))
                mbr = new DiscordMember(usr) { Discord = this, _guild_id = guild.Id };
            guild.MemberCount--;

            this.UpdateUserCache(usr);

            var ea = new GuildMemberRemoveEventArgs
            {
                Guild = guild,
                Member = mbr
            };
            await this._guildMemberRemoved.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildMemberUpdateEventAsync(TransportMember member, DiscordGuild guild)
        {
            var userAfter = new DiscordUser(member.User) { Discord = this };
            _ = this.UpdateUserCache(userAfter);

            var memberAfter = new DiscordMember(member) { Discord = this, _guild_id = guild.Id };

            if (!guild.Members.TryGetValue(member.User.Id, out var memberBefore))
                memberBefore = new DiscordMember(member) { Discord = this, _guild_id = guild.Id };

            guild._members.AddOrUpdate(member.User.Id, memberAfter, (_, _) => memberAfter);

            var ea = new GuildMemberUpdateEventArgs
            {
                Guild = guild,
                MemberAfter = memberAfter,
                MemberBefore = memberBefore,
            };

            await this._guildMemberUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildMembersChunkEventAsync(JObject dat)
        {
            var guild = this.Guilds[(ulong)dat["guild_id"]];
            var chunkIndex = (int)dat["chunk_index"];
            var chunkCount = (int)dat["chunk_count"];
            var nonce = (string)dat["nonce"];

            var mbrs = new HashSet<DiscordMember>();
            var pres = new HashSet<DiscordPresence>();

            var members = dat["members"].ToDiscordObject<TransportMember[]>();

            var memCount = members.Count();
            for (var i = 0; i < memCount; i++)
            {
                var mbr = new DiscordMember(members[i]) { Discord = this, _guild_id = guild.Id };
                this.UpdateUserCache(new DiscordUser(members[i].User) { Discord = this });

                guild._members[mbr.Id] = mbr;

                mbrs.Add(mbr);
            }

            guild.MemberCount = guild._members.Count;

            var ea = new GuildMembersChunkEventArgs
            {
                Guild = guild,
                Members = mbrs.ToImmutableDictionary(m => m.Id),
                ChunkIndex = chunkIndex,
                ChunkCount = chunkCount,
                Nonce = nonce,
            };

            if (dat["presences"] != null)
            {
                var presences = dat["presences"].ToDiscordObject<DiscordPresence[]>();

                var presCount = presences.Count();
                for (var i = 0; i < presCount; i++)
                {
                    var xp = presences[i];
                    xp.Discord = this;
                    xp.Activity = new DiscordActivity(xp.RawActivity);

                    if (xp.RawActivities != null)
                    {
                        xp._internalActivities = new DiscordActivity[xp.RawActivities.Length];
                        for (var j = 0; j < xp.RawActivities.Length; j++)
                            xp._internalActivities[j] = new DiscordActivity(xp.RawActivities[j]);
                    }

                    this._presences.AddOrUpdate(xp.InternalUser.Id, xp, (id, _) => xp);

                    pres.Add(xp);
                }

                //ea.Presences = new ReadOnlySet<DiscordPresence>(pres);
                ea.Presences = pres.ToImmutableDictionary(k => k.InternalUser.Id);
            }

            if (dat["not_found"] != null)
            {
                var nf = dat["not_found"].ToDiscordObject<ISet<ulong>>();
                ea.NotFound = new ReadOnlySet<ulong>(nf);
            }

            await this._guildMembersChunked.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Guild Role

        internal async Task OnGuildRoleCreateEventAsync(DiscordRole role, DiscordGuild guild)
        {
            role.Discord = this;
            role._guild_id = guild.Id;

            guild._roles[role.Id] = role;

            var ea = new GuildRoleCreateEventArgs
            {
                Guild = guild,
                Role = role
            };
            await this._guildRoleCreated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildRoleUpdateEventAsync(DiscordRole role, DiscordGuild guild)
        {
            var newRole = guild.GetRole(role.Id);
            var oldRole = new DiscordRole
            {
                _guild_id = guild.Id,
                _color = newRole._color,
                Discord = this,
                IsHoisted = newRole.IsHoisted,
                Id = newRole.Id,
                IsManaged = newRole.IsManaged,
                IsMentionable = newRole.IsMentionable,
                Name = newRole.Name,
                Permissions = newRole.Permissions,
                Position = newRole.Position,
                IconHash = newRole.IconHash,
                _emoji = newRole._emoji
            };

            newRole._guild_id = guild.Id;
            newRole._color = role._color;
            newRole.IsHoisted = role.IsHoisted;
            newRole.IsManaged = role.IsManaged;
            newRole.IsMentionable = role.IsMentionable;
            newRole.Name = role.Name;
            newRole.Permissions = role.Permissions;
            newRole.Position = role.Position;
            newRole._emoji = role._emoji;
            newRole.IconHash = role.IconHash;

            var ea = new GuildRoleUpdateEventArgs
            {
                Guild = guild,
                RoleAfter = newRole,
                RoleBefore = oldRole
            };
            await this._guildRoleUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnGuildRoleDeleteEventAsync(ulong roleId, DiscordGuild guild)
        {
            if (!guild._roles.TryRemove(roleId, out var role))
                this.Logger.LogWarning($"Attempted to delete a nonexistent role ({roleId}) from guild ({guild}).");

            var ea = new GuildRoleDeleteEventArgs
            {
                Guild = guild,
                Role = role
            };
            await this._guildRoleDeleted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Invite

        internal async Task OnInviteCreateEventAsync(ulong channelId, ulong guildId, DiscordInvite invite)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var channel = this.InternalGetCachedChannel(channelId);

            invite.Discord = this;

            guild._invites[invite.Code] = invite;

            var ea = new InviteCreateEventArgs
            {
                Channel = channel,
                Guild = guild,
                Invite = invite
            };
            await this._inviteCreated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnInviteDeleteEventAsync(ulong channelId, ulong guildId, JToken dat)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var channel = this.InternalGetCachedChannel(channelId);

            if (!guild._invites.TryRemove(dat["code"].ToString(), out var invite))
            {
                invite = dat.ToDiscordObject<DiscordInvite>();
                invite.Discord = this;
            }

            invite.IsRevoked = true;

            var ea = new InviteDeleteEventArgs
            {
                Channel = channel,
                Guild = guild,
                Invite = invite
            };
            await this._inviteDeleted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Message

        internal async Task OnMessageAckEventAsync(DiscordChannel chn, ulong messageId)
        {
            if (chn == null) return;

            if (this.MessageCache == null || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == chn.Id, out var msg))
            {
                msg = new DiscordMessage
                {
                    Id = messageId,
                    ChannelId = chn.Id,
                    Discord = this,
                };
            }

            if (this.ReadStates.TryGetValue(chn.Id, out var state))
            {
                state.LastMessageId = messageId;
                state.MentionCount = 0;
            }
            else
            {
                state = new DiscordReadState
                {
                    Id = chn.Id,
                    LastMessageId = messageId,
                    MentionCount = 0
                };
            }

            this._readStates[chn.Id] = state;

            await this._readStateUpdated.InvokeAsync(this, new ReadStateUpdateEventArgs() { ReadState = state }).ConfigureAwait(false);


            await this._messageAcknowledged.InvokeAsync(this, new MessageAcknowledgeEventArgs { Message = msg }).ConfigureAwait(false);
        }

        internal async Task OnMessageCreateEventAsync(DiscordMessage message, TransportUser author, TransportMember member, TransportUser referenceAuthor, TransportMember referenceMember)
        {
            message.Discord = this;
            this.PopulateMessageReactionsAndCache(message, author, member);
            message.PopulateMentions();

            if (message.Channel == null && message.ChannelId == default)
                this.Logger.LogWarning(LoggerEvents.WebSocketReceive, "Channel which the last message belongs to is not in cache - cache state might be invalid!");

            if (message.ReferencedMessage != null)
            {
                message.ReferencedMessage.Discord = this;
                this.PopulateMessageReactionsAndCache(message.ReferencedMessage, referenceAuthor, referenceMember);
                message.ReferencedMessage.PopulateMentions();
            }

            foreach (var sticker in message.Stickers)
                sticker.Discord = this;

            await this.UpdateMessageReadStatesAsync(message)
                .ConfigureAwait(false);

            var ea = new MessageCreateEventArgs
            {
                Message = message,

                MentionedUsers = new ReadOnlyCollection<DiscordUser>(message._mentionedUsers),
                MentionedRoles = message._mentionedRoles != null ? new ReadOnlyCollection<DiscordRole>(message._mentionedRoles) : null,
                MentionedChannels = message._mentionedChannels != null ? new ReadOnlyCollection<DiscordChannel>(message._mentionedChannels) : null
            };
            await this._messageCreated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        private async Task UpdateMessageReadStatesAsync(DiscordMessage message)
        {
            if (this.ReadStates.TryGetValue(message.ChannelId, out var readState))
            {
                if (message.Author.Id != this.CurrentUser.Id)
                {
                    if (message.MentionEveryone || message.MentionedUsers.Any(u => u?.Id == this.CurrentUser.Id) || message.Channel is DiscordDmChannel)
                    {
                        readState.MentionCount += 1;
                    }
                }
                else
                {
                    readState.MentionCount = 0;
                    readState.LastMessageId = message.Id;
                }

                await _readStateUpdated.InvokeAsync(this, new ReadStateUpdateEventArgs() { ReadState = readState })
                    .ConfigureAwait(false);
            }
        }

        internal async Task OnMessageUpdateEventAsync(DiscordMessage message, TransportUser author, TransportMember member, TransportUser referenceAuthor, TransportMember referenceMember)
        {
            DiscordGuild guild;

            message.Discord = this;
            var event_message = message;

            DiscordMessage oldmsg = null;
            if (this.Configuration.MessageCacheSize == 0
                || this.MessageCache == null
                || !this.MessageCache.TryGet(xm => xm.Id == event_message.Id && xm.ChannelId == event_message.ChannelId, out message)) // previous message was not in cache
            {
                message = event_message;
                this.PopulateMessageReactionsAndCache(message, author, member);
                guild = message.Channel?.Guild;

                if (message.ReferencedMessage != null)
                {
                    message.ReferencedMessage.Discord = this;
                    this.PopulateMessageReactionsAndCache(message.ReferencedMessage, referenceAuthor, referenceMember);
                    message.ReferencedMessage.PopulateMentions();
                }
            }
            else // previous message was fetched in cache
            {
                oldmsg = new DiscordMessage(message);

                // cached message is updated with information from the event message
                guild = message.Channel?.Guild;
                message.EditedTimestamp = event_message.EditedTimestamp;
                if (event_message.Content != null)
                    message.Content = event_message.Content;
                message._embeds.Clear();
                message._embeds.AddRange(event_message._embeds);
                message._attachments.Clear();
                message._attachments.AddRange(event_message._attachments);
                message.Pinned = event_message.Pinned;
                message.IsTTS = event_message.IsTTS;

                // Mentions
                message._mentionedUsers.Clear();
                message._mentionedUsers.AddRange(event_message._mentionedUsers ?? new());
                message._mentionedRoles.Clear();
                message._mentionedRoles.AddRange(event_message._mentionedRoles ?? new());
                message._mentionedChannels.Clear();
                message._mentionedChannels.AddRange(event_message._mentionedChannels ?? new());
                message.MentionEveryone = event_message.MentionEveryone;
            }

            message.PopulateMentions();

            var ea = new MessageUpdateEventArgs
            {
                Message = message,
                MessageBefore = oldmsg,
                MentionedUsers = new ReadOnlyCollection<DiscordUser>(message._mentionedUsers),
                MentionedRoles = message._mentionedRoles != null ? new ReadOnlyCollection<DiscordRole>(message._mentionedRoles) : null,
                MentionedChannels = message._mentionedChannels != null ? new ReadOnlyCollection<DiscordChannel>(message._mentionedChannels) : null
            };
            await this._messageUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnMessageDeleteEventAsync(ulong messageId, ulong channelId, ulong? guildId)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);

            if (channel == null)
            {
                channel = new DiscordDmChannel
                {
                    Id = channelId,
                    Discord = this,
                    Type = ChannelType.Private,
                    Recipients = Array.Empty<DiscordUser>()

                };
                this._privateChannels[channelId] = (DiscordDmChannel)channel;
            }

            if (channel == null
                || this.Configuration.MessageCacheSize == 0
                || this.MessageCache == null
                || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == channelId, out var msg))
            {
                msg = new DiscordMessage
                {

                    Id = messageId,
                    ChannelId = channelId,
                    Discord = this,
                };
            }

            if (this.Configuration.MessageCacheSize > 0)
                this.MessageCache?.Remove(xm => xm.Id == msg.Id && xm.ChannelId == channelId);

            var ea = new MessageDeleteEventArgs
            {
                Message = msg,
                Channel = channel,
                Guild = guild,
            };
            await this._messageDeleted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnMessageBulkDeleteEventAsync(ulong[] messageIds, ulong channelId, ulong? guildId)
        {
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);

            var msgs = new List<DiscordMessage>(messageIds.Length);
            foreach (var messageId in messageIds)
            {
                if (channel == null
                    || this.Configuration.MessageCacheSize == 0
                    || this.MessageCache == null
                    || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == channelId, out var msg))
                {
                    msg = new DiscordMessage
                    {
                        Id = messageId,
                        ChannelId = channelId,
                        Discord = this,
                    };
                }
                if (this.Configuration.MessageCacheSize > 0)
                    this.MessageCache?.Remove(xm => xm.Id == msg.Id && xm.ChannelId == channelId);
                msgs.Add(msg);
            }

            var guild = this.InternalGetCachedGuild(guildId);

            var ea = new MessageBulkDeleteEventArgs
            {
                Channel = channel,
                Messages = new ReadOnlyCollection<DiscordMessage>(msgs),
                Guild = guild
            };
            await this._messagesBulkDeleted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Message Reaction

        internal async Task OnMessageReactionAddAsync(ulong userId, ulong messageId, ulong channelId, ulong? guildId, TransportMember mbr, DiscordEmoji emoji)
        {
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);
            var guild = this.InternalGetCachedGuild(guildId);

            emoji.Discord = this;

            DiscordUser usr = null!;
            if (!this.TryGetCachedUserInternal(userId, out usr))
            {
                usr = this.UpdateUser(new DiscordUser { Id = userId, Discord = this }, guildId, guild, mbr);
            }
            else
            {
                usr = this.UpdateUser(usr, guild?.Id, guild, mbr);
            }

            if (channel == null)
            {
                channel = new DiscordDmChannel
                {
                    Id = channelId,
                    Discord = this,
                    Type = ChannelType.Private,
                    Recipients = new DiscordUser[] { usr }
                };
                this._privateChannels[channelId] = (DiscordDmChannel)channel;
            }


            if (channel == null
                || this.Configuration.MessageCacheSize == 0
                || this.MessageCache == null
                || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == channelId, out var msg))
            {
                msg = new DiscordMessage
                {
                    Id = messageId,
                    ChannelId = channelId,
                    Discord = this,
                    _reactions = new List<DiscordReaction>()
                };
            }

            var react = msg._reactions.FirstOrDefault(xr => xr.Emoji == emoji);
            if (react == null)
            {
                msg._reactions.Add(react = new DiscordReaction
                {
                    Count = 1,
                    Emoji = emoji,
                    IsMe = this.CurrentUser.Id == userId
                });
            }
            else
            {
                react.Count++;
                react.IsMe |= this.CurrentUser.Id == userId;
            }

            var ea = new MessageReactionAddEventArgs
            {
                Message = msg,
                User = usr,
                Guild = guild,
                Emoji = emoji
            };
            await this._messageReactionAdded.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnMessageReactionRemoveAsync(ulong userId, ulong messageId, ulong channelId, ulong? guildId, DiscordEmoji emoji)
        {
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);

            emoji.Discord = this;

            if (!this.UserCache.TryGetValue(userId, out var usr))
                usr = new DiscordUser { Id = userId, Discord = this };

            if (channel == null)
            {
                channel = new DiscordDmChannel
                {
                    Id = channelId,
                    Discord = this,
                    Type = ChannelType.Private,
                    Recipients = new DiscordUser[] { usr }
                };
                this._privateChannels[channelId] = (DiscordDmChannel)channel;
            }

            if (channel?.Guild != null)
                usr = channel.Guild.Members.TryGetValue(userId, out var member)
                    ? member
                    : new DiscordMember(usr) { Discord = this, _guild_id = channel.GuildId.Value };

            if (channel == null
                || this.Configuration.MessageCacheSize == 0
                || this.MessageCache == null
                || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == channelId, out var msg))
            {
                msg = new DiscordMessage
                {
                    Id = messageId,
                    ChannelId = channelId,
                    Discord = this
                };
            }

            var react = msg._reactions?.FirstOrDefault(xr => xr.Emoji == emoji);
            if (react != null)
            {
                react.Count--;
                react.IsMe &= this.CurrentUser.Id != userId;

                if (msg._reactions != null && react.Count <= 0) // shit happens
                    for (var i = 0; i < msg._reactions.Count; i++)
                        if (msg._reactions[i].Emoji == emoji)
                        {
                            msg._reactions.RemoveAt(i);
                            break;
                        }
            }

            var guild = this.InternalGetCachedGuild(guildId);

            var ea = new MessageReactionRemoveEventArgs
            {
                Message = msg,
                User = usr,
                Guild = guild,
                Emoji = emoji
            };
            await this._messageReactionRemoved.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnMessageReactionRemoveAllAsync(ulong messageId, ulong channelId, ulong? guildId)
        {
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);

            if (channel == null
                || this.Configuration.MessageCacheSize == 0
                || this.MessageCache == null
                || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == channelId, out var msg))
            {
                msg = new DiscordMessage
                {
                    Id = messageId,
                    ChannelId = channelId,
                    Discord = this
                };
            }

            msg._reactions?.Clear();

            var guild = this.InternalGetCachedGuild(guildId);

            var ea = new MessageReactionsClearEventArgs
            {
                Message = msg,
            };

            await this._messageReactionsCleared.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnMessageReactionRemoveEmojiAsync(ulong messageId, ulong channelId, ulong guildId, JToken dat)
        {
            var guild = this.InternalGetCachedGuild(guildId);
            var channel = this.InternalGetCachedChannel(channelId) ?? this.InternalGetCachedThread(channelId);

            if (channel == null)
            {
                channel = new DiscordDmChannel
                {
                    Id = channelId,
                    Discord = this,
                    Type = ChannelType.Private,
                    Recipients = Array.Empty<DiscordUser>()
                };
                this._privateChannels[channelId] = (DiscordDmChannel)channel;
            }

            if (channel == null
                || this.Configuration.MessageCacheSize == 0
                || this.MessageCache == null
                || !this.MessageCache.TryGet(xm => xm.Id == messageId && xm.ChannelId == channelId, out var msg))
            {
                msg = new DiscordMessage
                {
                    Id = messageId,
                    ChannelId = channelId,
                    Discord = this
                };
            }

            var partialEmoji = dat.ToDiscordObject<DiscordEmoji>();

            if (!guild._emojis.TryGetValue(partialEmoji.Id, out var emoji))
            {
                emoji = partialEmoji;
                emoji.Discord = this;
            }

            msg._reactions?.RemoveAll(r => r.Emoji.Equals(emoji));

            var ea = new MessageReactionRemoveEmojiEventArgs
            {
                Message = msg,
                Channel = channel,
                Guild = guild,
                Emoji = emoji
            };

            await this._messageReactionRemovedEmoji.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region User/Presence Update
         
        internal async Task OnPresenceUpdateEventAsync(JObject rawPresence, JObject rawUser, bool skipEvents = false)
        {
            var uid = rawUser != null ? (ulong)rawUser["id"] : (ulong)rawPresence["user_id"];
            DiscordPresence old = null;

            if (this._presences.TryGetValue(uid, out var presence))
            {
                old = new DiscordPresence(presence);
                DiscordJson.PopulateObject(rawPresence, presence);

                if (rawPresence["game"] == null || rawPresence["game"].Type == JTokenType.Null)
                    presence.RawActivity = null;

                presence.Activity ??= new DiscordActivity(presence.RawActivity);
                presence.Activity.UpdateWith(presence.RawActivity);

            }
            else
            {
                presence = rawPresence.ToObject<DiscordPresence>();
                presence.Discord = this;
                presence.Activity = new DiscordActivity(presence.RawActivity);
                presence.Activity.UpdateWith(presence.RawActivity);
                this._presences[uid] = presence;
            }

            // reuse arrays / avoid linq (this is a hot zone)
            if (presence.Activities == null || rawPresence["activities"] == null)
            {
                presence._internalActivities = Array.Empty<DiscordActivity>();
            }
            else
            {
                if (presence._internalActivities.Length != presence.RawActivities.Length)
                    presence._internalActivities = new DiscordActivity[presence.RawActivities.Length];

                for (var i = 0; i < presence._internalActivities.Length; i++)
                    presence._internalActivities[i] = new DiscordActivity(presence.RawActivities[i]);
            }

            if (this.UserCache.TryGetValue(uid, out var usr))
            {
                if (old != null && old.InternalUser != null)
                {
                    old.InternalUser.Username = usr.Username;
                    old.InternalUser.Discriminator = usr.Discriminator;
                    old.InternalUser.AvatarHash = usr.AvatarHash;
                }

                if (rawUser != null)
                {
                    if (rawUser["username"] is not null)
                        usr.Username = (string)rawUser["username"];
                    if (rawUser["discriminator"] is not null)
                        usr.Discriminator = (string)rawUser["discriminator"];
                    if (rawUser["avatar"] is not null)
                        usr.AvatarHash = (string)rawUser["avatar"];
                }

                presence.InternalUser ??= new TransportUser();
                presence.InternalUser.Username = usr.Username;
                presence.InternalUser.Discriminator = usr.Discriminator;
                presence.InternalUser.AvatarHash = usr.AvatarHash;
            }
            else if (presence.InternalUser != null)
            {
                usr = new DiscordUser(presence.InternalUser);
                this.UserCache[usr.Id] = usr;
            }

            if (!skipEvents)
            {
                var ea = new PresenceUpdateEventArgs
                {
                    Status = presence.Status,
                    Activity = presence.Activity,
                    User = usr,
                    PresenceBefore = old,
                    PresenceAfter = presence,
                    UserBefore = old?.InternalUser != null ? new DiscordUser(old.InternalUser) : usr,
                    UserAfter = usr
                };
                await this._presenceUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
            }
        }

        internal async Task OnUserSettingsUpdateEventAsync(JObject json)
        {
            var usr = new DiscordUser(json.ToObject<TransportUser>()) { Discord = this };

            if (json.TryGetValue("theme", out var t))
            {
                this.UserSettings.Theme = t.ToObject<string>();
            }

            if (json.TryGetValue("guild_positions", out var jsonPositions))
            {
                var positions = jsonPositions.ToObject<List<ulong>>();
                _guilds = new ConcurrentDictionary<ulong, DiscordGuild>(_guilds.OrderBy(k => positions.IndexOf(k.Key)).ToDictionary(k => k.Key, v => v.Value));

                this.UserSettings.GuildPositions = positions;
            }

            if (json.TryGetValue("status", out var jStatus))
            {
                this._presences[this.CurrentUser.Id].Status = jStatus.ToDiscordObject<UserStatus>();
            }

            var ea = new UserSettingsUpdateEventArgs()
            {
                User = usr
            };
            await this._userSettingsUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnUserSettingsProtoUpdateEventAsync(JObject json)
        {
            if (json.TryGetValue("settings", out var userSettingsToken))
            {
                DiscordUserSettings userSettings = userSettingsToken.ToObject<DiscordUserSettings>();
                this.UserSettingsProto = userSettings.Proto;
            }

            var ea = new UserSettingsProtoUpdateEventArgs()
            {
                Base64EncodedProto = this.UserSettingsProto
            };
            await this._userSettingsProtoUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnUserUpdateEventAsync(TransportUser user)
        {
            var usr_old = new DiscordUser
            {
                AvatarHash = this.CurrentUser.AvatarHash,
                Discord = this,
                Discriminator = this.CurrentUser.Discriminator,
                Email = this.CurrentUser.Email,
                Id = this.CurrentUser.Id,
                IsBot = this.CurrentUser.IsBot,
                MfaEnabled = this.CurrentUser.MfaEnabled,
                Username = this.CurrentUser.Username,
                Verified = this.CurrentUser.Verified,
                GlobalName = user.GlobalName
            };

            this.CurrentUser.AvatarHash = user.AvatarHash;
            this.CurrentUser.Discriminator = user.Discriminator;
            this.CurrentUser.Email = user.Email;
            this.CurrentUser.Id = user.Id;
            this.CurrentUser.IsBot = user.IsBot;
            this.CurrentUser.MfaEnabled = user.MfaEnabled;
            this.CurrentUser.Username = user.Username;
            this.CurrentUser.Verified = user.Verified;
            this.CurrentUser.GlobalName = user.GlobalName;

            var ea = new UserUpdateEventArgs
            {
                UserAfter = this.CurrentUser,
                UserBefore = usr_old
            };
            await this._userUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Relationships
        private async Task OnRelationshipAddAsync(JToken json)
        {
            var rel = json.ToObject<DiscordRelationship>();
            rel.Discord = this;

            if (this._relationships.TryGetValue(rel.Id, out var oldRel))
            {
                oldRel.RelationshipType = rel.RelationshipType;
            }
            else
            {
                this.UpdateUserCache(new DiscordUser(rel.InternalUser) { Discord = this });
                _relationships[rel.Id] = rel;
            }

            await _relationshipAdded?.InvokeAsync(this, new RelationshipAddEventArgs() { Relationship = rel });
        }

        private async Task OnRelationshipRemoveAsync(JToken json)
        {
            var rel = json.ToObject<DiscordRelationship>();

            if (_relationships.TryRemove(rel.Id, out rel))
                await _relationshipRemoved?.InvokeAsync(this, new RelationshipRemoveEventArgs() { Relationship = rel });
        }
        #endregion

        private async Task OnUserGuildSettingsUpdated(JToken json)
        {
            // TODO: verify if version is always higher mn
            var rel = json.ToObject<DiscordUserGuildSettings>();
            _userGuildSettings[rel.GuildId ?? default] = rel;

            if (rel.GuildId != null && this.TryGetCachedGuild(rel.GuildId.Value, out var guild))
            {
                foreach (var channel in guild.Channels.Values)
                {
                    if (this.ReadStates.TryGetValue(channel.Id, out var rs))
                        await _readStateUpdated.InvokeAsync(this, new ReadStateUpdateEventArgs() { ReadState = rs });
                }
            }
            else
            {
                foreach (var channel in this.PrivateChannels.Values)
                {
                    if (this.ReadStates.TryGetValue(channel.Id, out var rs))
                        await _readStateUpdated.InvokeAsync(this, new ReadStateUpdateEventArgs() { ReadState = rs });
                }
            }
        }


        #region Voice

        internal async Task OnVoiceStateUpdateEventAsync(JObject raw)
        {
            var gid = (ulong)raw["guild_id"];
            var uid = (ulong)raw["user_id"];
            var gld = this._guilds[gid];

            var vstateNew = raw.ToDiscordObject<DiscordVoiceState>();
            vstateNew.Discord = this;

            gld._voiceStates.TryRemove(uid, out var vstateOld);

            if (vstateNew.Channel != null)
            {
                gld._voiceStates[vstateNew.UserId] = vstateNew;
            }

            if (gld._members.TryGetValue(uid, out var mbr))
            {
                mbr.IsMuted = vstateNew.IsServerMuted;
                mbr.IsDeafened = vstateNew.IsServerDeafened;
            }
            else
            {
                var transportMbr = vstateNew.TransportMember;
                this.UpdateUser(new DiscordUser(transportMbr.User) { Discord = this }, gid, gld, transportMbr);
            }

            var ea = new VoiceStateUpdateEventArgs
            {
                Guild = vstateNew.Guild,
                Channel = vstateNew.Channel,
                User = vstateNew.User,
                SessionId = vstateNew.SessionId,

                Before = vstateOld,
                After = vstateNew
            };
            await this._voiceStateUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnVoiceServerUpdateEventAsync(string endpoint, string token, DiscordGuild guild)
        {
            var ea = new VoiceServerUpdateEventArgs
            {
                Endpoint = endpoint,
                VoiceToken = token,
                Guild = guild
            };
            await this._voiceServerUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Thread

        internal async Task OnThreadCreateEventAsync(DiscordThreadChannel thread, bool isNew)
        {
            thread.Discord = this;
            this.InternalGetCachedGuild(thread.GuildId)._threads[thread.Id] = thread;

            await this._threadCreated.InvokeAsync(this, new ThreadCreateEventArgs { Thread = thread, Guild = thread.Guild, Parent = thread.Parent }).ConfigureAwait(false);
        }

        internal async Task OnThreadUpdateEventAsync(DiscordThreadChannel thread)
        {
            if (thread == null)
                return;

            DiscordThreadChannel threadOld;
            ThreadUpdateEventArgs updateEvent;

            thread.Discord = this;

            var guild = thread.Guild;
            guild.Discord = this;

            var cthread = this.InternalGetCachedThread(thread.Id);

            if (cthread != null) //thread is cached
            {
                threadOld = new DiscordThreadChannel
                {
                    Discord = this,
                    GuildId = cthread.GuildId,
                    CreatorId = cthread.CreatorId,
                    ParentId = cthread.ParentId,
                    Id = cthread.Id,
                    Name = cthread.Name,
                    Type = cthread.Type,
                    LastMessageId = cthread.LastMessageId,
                    MessageCount = cthread.MessageCount,
                    MemberCount = cthread.MemberCount,
                    ThreadMetadata = cthread.ThreadMetadata,
                    CurrentMember = cthread.CurrentMember,
                };

                updateEvent = new ThreadUpdateEventArgs
                {
                    ThreadAfter = thread,
                    ThreadBefore = threadOld,
                    Guild = thread.Guild,
                    Parent = thread.Parent
                };
            }
            else
            {
                updateEvent = new ThreadUpdateEventArgs
                {
                    ThreadAfter = thread,
                    Guild = thread.Guild,
                    Parent = thread.Parent
                };
                guild._threads[thread.Id] = thread;
            }

            await this._threadUpdated.InvokeAsync(this, updateEvent).ConfigureAwait(false);
        }

        internal async Task OnThreadDeleteEventAsync(DiscordThreadChannel thread)
        {
            if (thread == null)
                return;

            thread.Discord = this;

            var gld = thread.Guild;
            if (gld._threads.TryRemove(thread.Id, out var cachedThread))
                thread = cachedThread;

            await this._threadDeleted.InvokeAsync(this, new ThreadDeleteEventArgs { Thread = thread, Guild = thread.Guild, Parent = thread.Parent }).ConfigureAwait(false);
        }

        internal async Task OnThreadListSyncEventAsync(DiscordGuild guild, JObject dat)
        {
            var channel_ids = dat["channel_ids"]?.ToDiscordObject<IReadOnlyList<ulong>>();
            var threads = dat["threads"].ToDiscordObject<IReadOnlyList<DiscordThreadChannel>>();
            var members = dat["members"]?.ToDiscordObject<IReadOnlyList<DiscordThreadChannelMember>>();
            var messages = dat["most_recent_messages"]?.ToDiscordObject<IReadOnlyList<DiscordMessage>>();

            guild.Discord = this;
            var channels = channel_ids != null ?
                channel_ids.Select(x => guild.GetChannel(x) ?? new DiscordChannel{ Id = x, GuildId = guild.Id}) :
                threads.Select(t => guild.GetChannel(t.ParentId.Value) ?? new DiscordChannel{ Id = t.ParentId.Value, GuildId = guild.Id});

            foreach (var channel in channels)
            {
                channel.Discord = this;
            }

            foreach (var thread in threads)
            {
                thread.Discord = this;
                guild._threads[thread.Id] = thread;
            }

            if (members != null)
            {
                foreach (var member in members)
                {
                    member.Discord = this;
                    member._guild_id = guild.Id;

                    var thread = threads.SingleOrDefault(x => x.Id == member.ThreadId);
                    if (thread != null)
                        thread.CurrentMember = member;
                }
            }

            if (messages != null)
            {
                foreach (var message in messages)
                {
                    message.Discord = this;

                    var thread = threads.FirstOrDefault(t => t.Id == message.ChannelId);
                    if (thread != null)
                        thread.FirstMessage = message;
                }
            }

            await this._threadListSynced.InvokeAsync(this, new ThreadListSyncEventArgs
            {
                Guild = guild,
                Channels = channels.ToList(),
                Threads = threads,
                CurrentMembers = members,
                MostRecentMessages = messages
            }).ConfigureAwait(false);
        }

        internal async Task OnThreadMemberUpdateEventAsync(DiscordThreadChannelMember member)
        {
            member.Discord = this;

            var thread = this.InternalGetCachedThread(member.ThreadId);
            member._guild_id = thread.Guild.Id;
            thread.CurrentMember = member;
            thread.Guild._threads[thread.Id] = thread;

            await this._threadMemberUpdated.InvokeAsync(this, new ThreadMemberUpdateEventArgs { ThreadMember = member, Thread = thread }).ConfigureAwait(false);
        }

        internal async Task OnThreadMembersUpdateEventAsync(DiscordGuild guild, ulong thread_id, IReadOnlyList<DiscordThreadChannelMember> addedMembers, IReadOnlyList<ulong?> removed_member_ids, int member_count)
        {
            var thread = this.InternalGetCachedThread(thread_id);

            if (thread == null) // Should a member of an archived thread leave, THREAD_MEMBERS_UPDATE is fired by Discord. Archived threads are not guaranteed to be in cache. PR ##1120
            {
                thread = new DiscordThreadChannel
                {
                    Id = thread_id,
                    GuildId = guild.Id,
                };
            }

            thread.Discord = this;
            guild.Discord = this;
            thread.MemberCount = member_count;

            var removedMembers = new List<DiscordMember>();
            if (removed_member_ids != null)
            {
                foreach (var removedId in removed_member_ids)
                {
                    removedMembers.Add(guild._members.TryGetValue(removedId.Value, out var member) ? member : new DiscordMember { Id = removedId.Value, _guild_id = guild.Id, Discord = this });
                }

                if (removed_member_ids.Contains(this.CurrentUser.Id)) //indicates the bot was removed from the thread
                    thread.CurrentMember = null;
            }
            else
                removed_member_ids = Array.Empty<ulong?>();

            if (addedMembers != null)
            {
                foreach (var threadMember in addedMembers)
                {
                    threadMember.Discord = this;
                    threadMember._guild_id = guild.Id;
                }

                if (addedMembers.Any(member => member.Id == this.CurrentUser.Id))
                    thread.CurrentMember = addedMembers.Single(member => member.Id == this.CurrentUser.Id);
            }
            else
                addedMembers = Array.Empty<DiscordThreadChannelMember>();

            var threadMembersUpdateArg = new ThreadMembersUpdateEventArgs
            {
                Guild = guild,
                Thread = thread,
                AddedMembers = addedMembers,
                RemovedMembers = removedMembers,
                MemberCount = member_count
            };

            await this._threadMembersUpdated.InvokeAsync(this, threadMembersUpdateArg).ConfigureAwait(false);
        }

        #endregion

        #region Commands

        internal async Task OnApplicationCommandCreateAsync(DiscordApplicationCommand cmd, ulong? guild_id)
        {
            cmd.Discord = this;

            var guild = this.InternalGetCachedGuild(guild_id);

            if (guild == null && guild_id.HasValue)
            {
                guild = new DiscordGuild
                {
                    Id = guild_id.Value,
                    Discord = this
                };
            }

            var ea = new ApplicationCommandEventArgs
            {
                Guild = guild,
                Command = cmd
            };

            await this._applicationCommandCreated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnApplicationCommandUpdateAsync(DiscordApplicationCommand cmd, ulong? guild_id)
        {
            cmd.Discord = this;

            var guild = this.InternalGetCachedGuild(guild_id);

            if (guild == null && guild_id.HasValue)
            {
                guild = new DiscordGuild
                {
                    Id = guild_id.Value,
                    Discord = this
                };
            }

            var ea = new ApplicationCommandEventArgs
            {
                Guild = guild,
                Command = cmd
            };

            await this._applicationCommandUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnApplicationCommandPermissionsUpdateAsync(JObject obj)
        {
            var ev = obj.ToObject<ApplicationCommandPermissionsUpdatedEventArgs>();

            await this._applicationCommandPermissionsUpdated.InvokeAsync(this, ev).ConfigureAwait(false);
        }

        internal async Task OnApplicationCommandDeleteAsync(DiscordApplicationCommand cmd, ulong? guild_id)
        {
            cmd.Discord = this;

            var guild = this.InternalGetCachedGuild(guild_id);

            if (guild == null && guild_id.HasValue)
            {
                guild = new DiscordGuild
                {
                    Id = guild_id.Value,
                    Discord = this
                };
            }

            var ea = new ApplicationCommandEventArgs
            {
                Guild = guild,
                Command = cmd
            };

            await this._applicationCommandDeleted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Integration

        internal async Task OnIntegrationCreateAsync(DiscordIntegration integration, ulong guild_id)
        {
            var guild = this.InternalGetCachedGuild(guild_id);

            if (guild == null)
            {
                guild = new DiscordGuild
                {
                    Id = guild_id,
                    Discord = this
                };
            }

            var ea = new IntegrationCreateEventArgs
            {
                Guild = guild,
                Integration = integration
            };

            await this._integrationCreated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnIntegrationUpdateAsync(DiscordIntegration integration, ulong guild_id)
        {
            var guild = this.InternalGetCachedGuild(guild_id);

            if (guild == null)
            {
                guild = new DiscordGuild
                {
                    Id = guild_id,
                    Discord = this
                };
            }

            var ea = new IntegrationUpdateEventArgs
            {
                Guild = guild,
                Integration = integration
            };

            await this._integrationUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnIntegrationDeleteAsync(ulong integration_id, ulong guild_id, ulong? application_id)
        {
            var guild = this.InternalGetCachedGuild(guild_id);

            if (guild == null)
            {
                guild = new DiscordGuild
                {
                    Id = guild_id,
                    Discord = this
                };
            }

            var ea = new IntegrationDeleteEventArgs
            {
                Guild = guild,
                Applicationid = application_id,
                IntegrationId = integration_id
            };

            await this._integrationDeleted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #region Stage Instance

        internal async Task OnStageInstanceCreateAsync(DiscordStageInstance instance)
        {
            instance.Discord = this;

            var guild = this.InternalGetCachedGuild(instance.GuildId);

            guild._stageInstances[instance.Id] = instance;

            var eventArgs = new StageInstanceCreateEventArgs
            {
                StageInstance = instance
            };

            await this._stageInstanceCreated.InvokeAsync(this, eventArgs).ConfigureAwait(false);
        }

        internal async Task OnStageInstanceUpdateAsync(DiscordStageInstance instance)
        {
            instance.Discord = this;

            var guild = this.InternalGetCachedGuild(instance.GuildId);

            if (!guild._stageInstances.TryRemove(instance.Id, out var oldInstance))
                oldInstance = new DiscordStageInstance { Id = instance.Id, GuildId = instance.GuildId, ChannelId = instance.ChannelId };

            guild._stageInstances[instance.Id] = instance;

            var eventArgs = new StageInstanceUpdateEventArgs
            {
                StageInstanceBefore = oldInstance,
                StageInstanceAfter = instance
            };

            await this._stageInstanceUpdated.InvokeAsync(this, eventArgs).ConfigureAwait(false);
        }

        internal async Task OnStageInstanceDeleteAsync(DiscordStageInstance instance)
        {
            instance.Discord = this;

            var guild = this.InternalGetCachedGuild(instance.GuildId);

            guild._stageInstances.TryRemove(instance.Id, out _);

            var eventArgs = new StageInstanceDeleteEventArgs
            {
                StageInstance = instance
            };

            await this._stageInstanceDeleted.InvokeAsync(this, eventArgs).ConfigureAwait(false);
        }

        #endregion

        #region Misc

        internal async Task OnInteractionCreateAsync(ulong? guildId, ulong channelId, TransportUser user, TransportMember member, DiscordInteraction interaction)
        {
            interaction.Data ??= new DiscordInteractionData();
            interaction.ChannelId = channelId;
            interaction.GuildId = guildId;
            interaction.Discord = this;
            interaction.Data.Discord = this;

            if (user != null)
            {
                var usr = new DiscordUser(user) { Discord = this };
                if (member != null)
                {
                    usr = new DiscordMember(member) { _guild_id = guildId.Value, Discord = this };
                    this.UpdateUser(usr, guildId, interaction.Guild, member);
                }
                else
                    this.UpdateUserCache(usr);

                interaction.User = usr;
            }

            var resolved = interaction.Data.Resolved;
            if (resolved != null)
            {
                if (resolved.Users != null)
                {
                    foreach (var c in resolved.Users)
                    {
                        c.Value.Discord = this;
                        this.UpdateUserCache(c.Value);
                    }
                }

                if (resolved.Members != null)
                {
                    foreach (var c in resolved.Members)
                    {
                        c.Value.Discord = this;
                        c.Value.Id = c.Key;
                        c.Value._guild_id = guildId.Value;
                        c.Value.User.Discord = this;

                        this.UpdateUserCache(c.Value.User);
                    }
                }

                if (resolved.Channels != null)
                {
                    foreach (var c in resolved.Channels)
                    {
                        c.Value.Discord = this;

                        if (guildId.HasValue)
                            c.Value.GuildId = guildId.Value;
                    }
                }

                if (resolved.Roles != null)
                {
                    foreach (var c in resolved.Roles)
                    {
                        c.Value.Discord = this;

                        if (guildId.HasValue)
                            c.Value._guild_id = guildId.Value;
                    }
                }

                if (resolved.Messages != null)
                {
                    foreach (var m in resolved.Messages)
                    {
                        m.Value.Discord = this;

                        if (guildId.HasValue)
                            m.Value._guildId = guildId.Value;
                    }
                }
            }

            if (interaction.Type is InteractionType.Component)
            {
                interaction.Message.Discord = this;
                interaction.Message.ChannelId = interaction.ChannelId;
                var cea = new ComponentInteractionCreateEventArgs
                {
                    Message = interaction.Message,
                    Interaction = interaction
                };

                await this._componentInteractionCreated.InvokeAsync(this, cea).ConfigureAwait(false);
            }
            else if (interaction.Type is InteractionType.ModalSubmit)
            {
                var mea = new ModalSubmitEventArgs(interaction);

                await this._modalSubmitted.InvokeAsync(this, mea).ConfigureAwait(false);
            }
            else
            {
                if (interaction.Data.Target.HasValue) // Context-Menu. //
                {
                    var targetId = interaction.Data.Target.Value;
                    DiscordUser targetUser = null;
                    DiscordMember targetMember = null;
                    DiscordMessage targetMessage = null;

                    interaction.Data.Resolved.Messages?.TryGetValue(targetId, out targetMessage);
                    interaction.Data.Resolved.Members?.TryGetValue(targetId, out targetMember);
                    interaction.Data.Resolved.Users?.TryGetValue(targetId, out targetUser);

                    var ctea = new ContextMenuInteractionCreateEventArgs
                    {
                        Interaction = interaction,
                        TargetUser = targetMember ?? targetUser,
                        TargetMessage = targetMessage,
                        Type = interaction.Data.Type,
                    };
                    await this._contextMenuInteractionCreated.InvokeAsync(this, ctea).ConfigureAwait(false);
                }
                else
                {
                    var ea = new InteractionCreateEventArgs
                    {
                        Interaction = interaction
                    };

                    await this._interactionCreated.InvokeAsync(this, ea).ConfigureAwait(false);
                }
            }
        }

        internal async Task OnTypingStartEventAsync(ulong userId, ulong channelId, DiscordChannel channel, ulong? guildId, DateTimeOffset started, TransportMember mbr)
        {
            if (channel == null)
            {
                channel = new DiscordChannel
                {
                    Discord = this,
                    Id = channelId,
                    GuildId = guildId ?? default,
                };
            }

            var guild = this.InternalGetCachedGuild(guildId);
            var usr = this.UpdateUser(new DiscordUser { Id = userId, Discord = this }, guildId, guild, mbr);

            var ea = new TypingStartEventArgs
            {
                Channel = channel,
                User = usr,
                Guild = guild,
                StartedAt = started
            };
            await this._typingStarted.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnWebhooksUpdateAsync(DiscordChannel channel, DiscordGuild guild)
        {
            var ea = new WebhooksUpdateEventArgs
            {
                Channel = channel,
                Guild = guild
            };
            await this._webhooksUpdated.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        internal async Task OnStickersUpdatedAsync(IEnumerable<DiscordMessageSticker> newStickers, JObject raw)
        {
            var guild = this.InternalGetCachedGuild((ulong)raw["guild_id"]);
            var oldStickers = new ConcurrentDictionary<ulong, DiscordMessageSticker>(guild._stickers);

            guild._stickers.Clear();

            foreach (var nst in newStickers)
            {
                if (nst.User != null)
                    nst.User.Discord = this;

                nst.Discord = this;

                guild._stickers[nst.Id] = nst;
            }

            var sea = new GuildStickersUpdateEventArgs
            {
                Guild = guild,
                StickersBefore = oldStickers,
                StickersAfter = guild.Stickers
            };

            await this._guildStickersUpdated.InvokeAsync(this, sea).ConfigureAwait(false);
        }

        internal async Task OnUnknownEventAsync(GatewayPayload payload)
        {
            var ea = new UnknownEventArgs { EventName = payload.EventName, Json = (payload.Data as JObject)?.ToString() };
            await this._unknownEvent.InvokeAsync(this, ea).ConfigureAwait(false);
        }

        #endregion

        #endregion
    }
}
