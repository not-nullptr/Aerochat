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
using System.IO;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Abstractions;
using Microsoft.Extensions.Logging;

namespace DSharpPlus
{
    public sealed partial class DiscordClient
    {
        internal static TimeSpan EventExecutionLimit { get; } = TimeSpan.FromSeconds(1);

        // oh lord why did you have to pack into regions
        // this makes simple copy-paste ineffective
        // :notlikethis:

        #region WebSocket

        /// <summary>
        /// Fired whenever a WebSocket error occurs within the client.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, SocketErrorEventArgs> SocketErrored
        {
            add => this._socketErrored.Register(value);
            remove => this._socketErrored.Unregister(value);
        }
        private AsyncEvent<DiscordClient, SocketErrorEventArgs> _socketErrored;

        /// <summary>
        /// Fired whenever WebSocket connection is established.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, SocketEventArgs> SocketOpened
        {
            add => this._socketOpened.Register(value);
            remove => this._socketOpened.Unregister(value);
        }
        private AsyncEvent<DiscordClient, SocketEventArgs> _socketOpened;

        /// <summary>
        /// Fired whenever WebSocket connection is terminated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, SocketCloseEventArgs> SocketClosed
        {
            add => this._socketClosed.Register(value);
            remove => this._socketClosed.Unregister(value);
        }
        private AsyncEvent<DiscordClient, SocketCloseEventArgs> _socketClosed;

        /// <summary>
        /// Fired when this client has successfully completed its handshake with the websocket gateway.
        /// </summary>
        /// <remarks>
        /// <i><see cref="Guilds"/> will not be populated when this event is fired.</i><br/>
        /// See also: <see cref="GuildAvailable"/>, <see cref="GuildDownloadCompleted"/>
        /// </remarks>
        public event AsyncEventHandler<DiscordClient, ReadyEventArgs> Ready
        {
            add => this._ready.Register(value);
            remove => this._ready.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ReadyEventArgs> _ready;

        /// <summary>
        /// Fired whenever a session is resumed.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ResumedEventArgs> Resumed
        {
            add => this._resumed.Register(value);
            remove => this._resumed.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ResumedEventArgs> _resumed;

        /// <summary>
        /// Fired on received heartbeat ACK.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, HeartbeatEventArgs> Heartbeated
        {
            add => this._heartbeated.Register(value);
            remove => this._heartbeated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, HeartbeatEventArgs> _heartbeated;

        /// <summary>
        /// Fired on heartbeat attempt cancellation due to too many failed heartbeats.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ZombiedEventArgs> Zombied
        {
            add => this._zombied.Register(value);
            remove => this._zombied.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ZombiedEventArgs> _zombied;

        #endregion

        #region Channel

        /// <summary>
        /// Fired when a new channel is created.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelCreateEventArgs> ChannelCreated
        {
            add => this._channelCreated.Register(value);
            remove => this._channelCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelCreateEventArgs> _channelCreated;

        /// <summary>
        /// Fired when a channel is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelUpdateEventArgs> ChannelUpdated
        {
            add => this._channelUpdated.Register(value);
            remove => this._channelUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelUpdateEventArgs> _channelUpdated;

        /// <summary>
        /// Fired when a channel is deleted
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelDeleteEventArgs> ChannelDeleted
        {
            add => this._channelDeleted.Register(value);
            remove => this._channelDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelDeleteEventArgs> _channelDeleted;

        /// <summary>
        /// Fired when a new DM channel is created.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, DmChannelCreateEventArgs> DmChannelCreated
        {
            add => this._dmChannelCreated.Register(value);
            remove => this._dmChannelCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, DmChannelCreateEventArgs> _dmChannelCreated;


        /// <summary>
        /// Fired when a dm channel is deleted
        /// For this Event you need the <see cref="DiscordIntents.DirectMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, DmChannelDeleteEventArgs> DmChannelDeleted
        {
            add => this._dmChannelDeleted.Register(value);
            remove => this._dmChannelDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, DmChannelDeleteEventArgs> _dmChannelDeleted;

        /// <summary>
        /// Fired whenever a channel's pinned message list is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ChannelPinsUpdateEventArgs> ChannelPinsUpdated
        {
            add => this._channelPinsUpdated.Register(value);
            remove => this._channelPinsUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelPinsUpdateEventArgs> _channelPinsUpdated;

        #endregion

        #region Guild

        /// <summary>
        /// Fired when the user joins a new guild.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        /// <remarks>[alias="GuildJoined"][alias="JoinedGuild"]</remarks>
        public event AsyncEventHandler<DiscordClient, GuildCreateEventArgs> GuildCreated
        {
            add => this._guildCreated.Register(value);
            remove => this._guildCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildCreateEventArgs> _guildCreated;

        /// <summary>
        /// Fired when a guild is becoming available.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildCreateEventArgs> GuildAvailable
        {
            add => this._guildAvailable.Register(value);
            remove => this._guildAvailable.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildCreateEventArgs> _guildAvailable;

        /// <summary>
        /// Fired when a guild is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildUpdateEventArgs> GuildUpdated
        {
            add => this._guildUpdated.Register(value);
            remove => this._guildUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildUpdateEventArgs> _guildUpdated;

        /// <summary>
        /// Fired when the user leaves or is removed from a guild.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildDeleteEventArgs> GuildDeleted
        {
            add => this._guildDeleted.Register(value);
            remove => this._guildDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildDeleteEventArgs> _guildDeleted;

        /// <summary>
        /// Fired when a guild becomes unavailable.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildDeleteEventArgs> GuildUnavailable
        {
            add => this._guildUnavailable.Register(value);
            remove => this._guildUnavailable.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildDeleteEventArgs> _guildUnavailable;

        /// <summary>
        /// Fired when all guilds finish streaming from Discord.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildDownloadCompletedEventArgs> GuildDownloadCompleted
        {
            add => this._guildDownloadCompletedEv.Register(value);
            remove => this._guildDownloadCompletedEv.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildDownloadCompletedEventArgs> _guildDownloadCompletedEv;

        /// <summary>
        /// Fired when a guilds emojis get updated
        /// For this Event you need the <see cref="DiscordIntents.GuildEmojis"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildEmojisUpdateEventArgs> GuildEmojisUpdated
        {
            add => this._guildEmojisUpdated.Register(value);
            remove => this._guildEmojisUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildEmojisUpdateEventArgs> _guildEmojisUpdated;


        public event AsyncEventHandler<DiscordClient, GuildStickersUpdateEventArgs> GuildStickersUpdated
        {
            add => this._guildStickersUpdated.Register(value);
            remove => this._guildStickersUpdated.Unregister(value);
        }

        private AsyncEvent<DiscordClient, GuildStickersUpdateEventArgs> _guildStickersUpdated;

        /// <summary>
        /// Fired when a guild integration is updated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildIntegrationsUpdateEventArgs> GuildIntegrationsUpdated
        {
            add => this._guildIntegrationsUpdated.Register(value);
            remove => this._guildIntegrationsUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildIntegrationsUpdateEventArgs> _guildIntegrationsUpdated;

        #endregion

        #region Scheduled Guild Events

        public event AsyncEventHandler<DiscordClient, ScheduledGuildEventCreateEventArgs> ScheduledGuildEventCreated
        {
            add => this._scheduledGuildEventCreated.Register(value);
            remove => this._scheduledGuildEventCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ScheduledGuildEventCreateEventArgs> _scheduledGuildEventCreated;

        public event AsyncEventHandler<DiscordClient, ScheduledGuildEventUpdateEventArgs> ScheduledGuildEventUpdated
        {
            add => this._scheduledGuildEventUpdated.Register(value);
            remove => this._scheduledGuildEventUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ScheduledGuildEventUpdateEventArgs> _scheduledGuildEventUpdated;

        public event AsyncEventHandler<DiscordClient, ScheduledGuildEventDeleteEventArgs> ScheduledGuildEventDeleted
        {
            add => this._scheduledGuildEventDeleted.Register(value);
            remove => this._scheduledGuildEventDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ScheduledGuildEventDeleteEventArgs> _scheduledGuildEventDeleted;

        public event AsyncEventHandler<DiscordClient, ScheduledGuildEventCompletedEventArgs> ScheduledGuildEventCompleted
        {
            add => this._scheduledGuildEventCompleted.Register(value);
            remove => this._scheduledGuildEventCompleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ScheduledGuildEventCompletedEventArgs> _scheduledGuildEventCompleted;

        public event AsyncEventHandler<DiscordClient, ScheduledGuildEventUserAddEventArgs> ScheduledGuildEventUserAdded
        {
            add => this._scheduledGuildEventUserAdded.Register(value);
            remove => this._scheduledGuildEventUserAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ScheduledGuildEventUserAddEventArgs> _scheduledGuildEventUserAdded;

        public event AsyncEventHandler<DiscordClient, ScheduledGuildEventUserRemoveEventArgs> ScheduledGuildEventUserRemoved
        {
            add => this._scheduledGuildEventUserRemoved.Register(value);
            remove => this._scheduledGuildEventUserRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ScheduledGuildEventUserRemoveEventArgs> _scheduledGuildEventUserRemoved;

        #endregion

        #region Guild Ban

        /// <summary>
        /// Fired when a guild ban gets added
        /// For this Event you need the <see cref="DiscordIntents.GuildBans"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildBanAddEventArgs> GuildBanAdded
        {
            add => this._guildBanAdded.Register(value);
            remove => this._guildBanAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildBanAddEventArgs> _guildBanAdded;

        /// <summary>
        /// Fired when a guild ban gets removed
        /// For this Event you need the <see cref="DiscordIntents.GuildBans"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildBanRemoveEventArgs> GuildBanRemoved
        {
            add => this._guildBanRemoved.Register(value);
            remove => this._guildBanRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildBanRemoveEventArgs> _guildBanRemoved;

        #endregion

        #region Guild Member

        /// <summary>
        /// Fired when a new user joins a guild.
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded
        {
            add => this._guildMemberAdded.Register(value);
            remove => this._guildMemberAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMemberAddEventArgs> _guildMemberAdded;

        /// <summary>
        /// Fired when a user is removed from a guild (leave/kick/ban).
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMemberRemoveEventArgs> GuildMemberRemoved
        {
            add => this._guildMemberRemoved.Register(value);
            remove => this._guildMemberRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMemberRemoveEventArgs> _guildMemberRemoved;

        /// <summary>
        /// Fired when a guild member is updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMemberUpdateEventArgs> GuildMemberUpdated
        {
            add => this._guildMemberUpdated.Register(value);
            remove => this._guildMemberUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMemberUpdateEventArgs> _guildMemberUpdated;

        /// <summary>
        /// Fired in response to Gateway Request Guild Members.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildMembersChunkEventArgs> GuildMembersChunked
        {
            add => this._guildMembersChunked.Register(value);
            remove => this._guildMembersChunked.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildMembersChunkEventArgs> _guildMembersChunked;

        #endregion

        #region Guild Role

        /// <summary>
        /// Fired when a guild role is created.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildRoleCreateEventArgs> GuildRoleCreated
        {
            add => this._guildRoleCreated.Register(value);
            remove => this._guildRoleCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildRoleCreateEventArgs> _guildRoleCreated;

        /// <summary>
        /// Fired when a guild role is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildRoleUpdateEventArgs> GuildRoleUpdated
        {
            add => this._guildRoleUpdated.Register(value);
            remove => this._guildRoleUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildRoleUpdateEventArgs> _guildRoleUpdated;

        /// <summary>
        /// Fired when a guild role is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, GuildRoleDeleteEventArgs> GuildRoleDeleted
        {
            add => this._guildRoleDeleted.Register(value);
            remove => this._guildRoleDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, GuildRoleDeleteEventArgs> _guildRoleDeleted;

        #endregion

        #region Invite

        /// <summary>
        /// Fired when an invite is created.
        /// For this Event you need the <see cref="DiscordIntents.GuildInvites"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, InviteCreateEventArgs> InviteCreated
        {
            add => this._inviteCreated.Register(value);
            remove => this._inviteCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, InviteCreateEventArgs> _inviteCreated;

        /// <summary>
        /// Fired when an invite is deleted.
        /// For this Event you need the <see cref="DiscordIntents.GuildInvites"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, InviteDeleteEventArgs> InviteDeleted
        {
            add => this._inviteDeleted.Register(value);
            remove => this._inviteDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, InviteDeleteEventArgs> _inviteDeleted;

        #endregion

        #region Message

        /// <summary>
        /// Fired when a message is created.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated
        {
            add => this._messageCreated.Register(value);
            remove => this._messageCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageCreateEventArgs> _messageCreated;

        /// <summary>
        /// Fired when message is acknowledged by the user.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageAcknowledgeEventArgs> MessageAcknowledged
        {
            add => this._messageAcknowledged.Register(value);
            remove => this._messageAcknowledged.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageAcknowledgeEventArgs> _messageAcknowledged;

        /// <summary>
        /// Fired when a message is updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageUpdateEventArgs> MessageUpdated
        {
            add => this._messageUpdated.Register(value);
            remove => this._messageUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageUpdateEventArgs> _messageUpdated;

        /// <summary>
        /// Fired when a message is deleted.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted
        {
            add => this._messageDeleted.Register(value);
            remove => this._messageDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageDeleteEventArgs> _messageDeleted;

        /// <summary>
        /// Fired when multiple messages are deleted at once.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessages"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageBulkDeleteEventArgs> MessagesBulkDeleted
        {
            add => this._messagesBulkDeleted.Register(value);
            remove => this._messagesBulkDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageBulkDeleteEventArgs> _messagesBulkDeleted;

        #endregion

        #region Message Reaction

        /// <summary>
        /// Fired when a reaction gets added to a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionAddEventArgs> MessageReactionAdded
        {
            add => this._messageReactionAdded.Register(value);
            remove => this._messageReactionAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionAddEventArgs> _messageReactionAdded;

        /// <summary>
        /// Fired when a reaction gets removed from a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEventArgs> MessageReactionRemoved
        {
            add => this._messageReactionRemoved.Register(value);
            remove => this._messageReactionRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionRemoveEventArgs> _messageReactionRemoved;

        /// <summary>
        /// Fired when all reactions get removed from a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionsClearEventArgs> MessageReactionsCleared
        {
            add => this._messageReactionsCleared.Register(value);
            remove => this._messageReactionsCleared.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionsClearEventArgs> _messageReactionsCleared;

        /// <summary>
        /// Fired when all reactions of a specific reaction are removed from a message.
        /// For this Event you need the <see cref="DiscordIntents.GuildMessageReactions"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, MessageReactionRemoveEmojiEventArgs> MessageReactionRemovedEmoji
        {
            add => this._messageReactionRemovedEmoji.Register(value);
            remove => this._messageReactionRemovedEmoji.Unregister(value);
        }
        private AsyncEvent<DiscordClient, MessageReactionRemoveEmojiEventArgs> _messageReactionRemovedEmoji;

        #endregion

        #region Presence/User Update

        /// <summary>
        /// Fired when a presence has been updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildPresences"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, PresenceUpdateEventArgs> PresenceUpdated
        {
            add => this._presenceUpdated.Register(value);
            remove => this._presenceUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, PresenceUpdateEventArgs> _presenceUpdated;


        /// <summary>
        /// Fired when the current user updates their settings.
        /// For this Event you need the <see cref="DiscordIntents.GuildPresences"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, UserSettingsUpdateEventArgs> UserSettingsUpdated
        {
            add => this._userSettingsUpdated.Register(value);
            remove => this._userSettingsUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UserSettingsUpdateEventArgs> _userSettingsUpdated;

        /// <summary>
        /// Fired when the current user updates their settings when the protobuf settings capability is enabled.
        /// For this Event you need the <see cref="Enums.ClientCapability.UserSettingsProto"/> capability specified in <seealso cref="GatewayIdentify.Capabilities"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, UserSettingsProtoUpdateEventArgs> UserSettingsProtoUpdated
        {
            add => this._userSettingsProtoUpdated.Register(value);
            remove => this._userSettingsProtoUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UserSettingsProtoUpdateEventArgs> _userSettingsProtoUpdated;

        /// <summary>
        /// Fired when properties about the current user change.
        /// </summary>
        /// <remarks>
        /// NB: This event only applies for changes to the <b>current user</b>, the client that is connected to Discord.
        /// For this Event you need the <see cref="DiscordIntents.GuildPresences"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </remarks>
        public event AsyncEventHandler<DiscordClient, UserUpdateEventArgs> UserUpdated
        {
            add => this._userUpdated.Register(value);
            remove => this._userUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UserUpdateEventArgs> _userUpdated;

        #endregion

        #region Voice

        /// <summary>
        /// Fired when someone joins/leaves/moves voice channels.
        /// For this Event you need the <see cref="DiscordIntents.GuildVoiceStates"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, VoiceStateUpdateEventArgs> VoiceStateUpdated
        {
            add => this._voiceStateUpdated.Register(value);
            remove => this._voiceStateUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, VoiceStateUpdateEventArgs> _voiceStateUpdated;

        /// <summary>
        /// Fired when a guild's voice server is updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildVoiceStates"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, VoiceServerUpdateEventArgs> VoiceServerUpdated
        {
            add => this._voiceServerUpdated.Register(value);
            remove => this._voiceServerUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, VoiceServerUpdateEventArgs> _voiceServerUpdated;

        #endregion

        #region Thread

        /// <summary>
        /// Fired when a thread is created.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ThreadCreateEventArgs> ThreadCreated
        {
            add => this._threadCreated.Register(value);
            remove => this._threadCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ThreadCreateEventArgs> _threadCreated;

        /// <summary>
        /// Fired when a thread is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ThreadUpdateEventArgs> ThreadUpdated
        {
            add => this._threadUpdated.Register(value);
            remove => this._threadUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ThreadUpdateEventArgs> _threadUpdated;

        /// <summary>
        /// Fired when a thread is deleted.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ThreadDeleteEventArgs> ThreadDeleted
        {
            add => this._threadDeleted.Register(value);
            remove => this._threadDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ThreadDeleteEventArgs> _threadDeleted;

        /// <summary>
        /// Fired when the current member gains access to a channel(s) that has threads.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ThreadListSyncEventArgs> ThreadListSynced
        {
            add => this._threadListSynced.Register(value);
            remove => this._threadListSynced.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ThreadListSyncEventArgs> _threadListSynced;

        /// <summary>
        /// Fired when the thread member for the current user is updated.
        /// For this Event you need the <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        /// <remarks>
        /// This event is mostly documented for completeness, and it not fired every time
        /// DM channels in which no prior messages were received or sent.
        /// </remarks>
        public event AsyncEventHandler<DiscordClient, ThreadMemberUpdateEventArgs> ThreadMemberUpdated
        {
            add => this._threadMemberUpdated.Register(value);
            remove => this._threadMemberUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ThreadMemberUpdateEventArgs> _threadMemberUpdated;

        /// <summary>
        /// Fired when the thread members are updated.
        /// For this Event you need the <see cref="DiscordIntents.GuildMembers"/> or <see cref="DiscordIntents.Guilds"/> intent specified in <seealso cref="DiscordConfiguration.Intents"/>
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ThreadMembersUpdateEventArgs> ThreadMembersUpdated
        {
            add => this._threadMembersUpdated.Register(value);
            remove => this._threadMembersUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ThreadMembersUpdateEventArgs> _threadMembersUpdated;

        #endregion

        #region Application

        /// <summary>
        /// Fired when a new application command is registered.
        /// </summary>
        [Obsolete("This event has been removed by discord and does not fire anymore.", false)]
        public event AsyncEventHandler<DiscordClient, ApplicationCommandEventArgs> ApplicationCommandCreated
        {
            add => this._applicationCommandCreated.Register(value);
            remove => this._applicationCommandCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ApplicationCommandEventArgs> _applicationCommandCreated;

        /// <summary>
        /// Fired when an application command is updated.
        /// </summary>
        [Obsolete("This event has been removed by discord and does not fire anymore.", false)]
        public event AsyncEventHandler<DiscordClient, ApplicationCommandEventArgs> ApplicationCommandUpdated
        {
            add => this._applicationCommandUpdated.Register(value);
            remove => this._applicationCommandUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ApplicationCommandEventArgs> _applicationCommandUpdated;

        /// <summary>
        /// Fired when an application command is deleted.
        /// </summary>
        [Obsolete("This event has been removed by discord and does not fire anymore.", false)]
        public event AsyncEventHandler<DiscordClient, ApplicationCommandEventArgs> ApplicationCommandDeleted
        {
            add => this._applicationCommandDeleted.Register(value);
            remove => this._applicationCommandDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ApplicationCommandEventArgs> _applicationCommandDeleted;

        [Obsolete("This event may be removed by discord and may not fire anymore.", false)]
        public event AsyncEventHandler<DiscordClient, ApplicationCommandPermissionsUpdatedEventArgs> ApplicationCommandPermissionsUpdated
        {
            add => this._applicationCommandPermissionsUpdated.Register(value);
            remove => this._applicationCommandPermissionsUpdated.Unregister(value);
        }

        private AsyncEvent<DiscordClient, ApplicationCommandPermissionsUpdatedEventArgs> _applicationCommandPermissionsUpdated;

        #endregion

        #region Integration

        /// <summary>
        /// Fired when an integration is created.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, IntegrationCreateEventArgs> IntegrationCreated
        {
            add => this._integrationCreated.Register(value);
            remove => this._integrationCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, IntegrationCreateEventArgs> _integrationCreated;

        /// <summary>
        /// Fired when an integration is updated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, IntegrationUpdateEventArgs> IntegrationUpdated
        {
            add => this._integrationUpdated.Register(value);
            remove => this._integrationUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, IntegrationUpdateEventArgs> _integrationUpdated;

        /// <summary>
        /// Fired when an integration is deleted.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, IntegrationDeleteEventArgs> IntegrationDeleted
        {
            add => this._integrationDeleted.Register(value);
            remove => this._integrationDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, IntegrationDeleteEventArgs> _integrationDeleted;

        #endregion

        #region Stage Instance

        /// <summary>
        /// Fired when a stage instance is created.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, StageInstanceCreateEventArgs> StageInstanceCreated
        {
            add => this._stageInstanceCreated.Register(value);
            remove => this._stageInstanceCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, StageInstanceCreateEventArgs> _stageInstanceCreated;

        /// <summary>
        /// Fired when a stage instance is updated.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, StageInstanceUpdateEventArgs> StageInstanceUpdated
        {
            add => this._stageInstanceUpdated.Register(value);
            remove => this._stageInstanceUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, StageInstanceUpdateEventArgs> _stageInstanceUpdated;

        /// <summary>
        /// Fired when a stage instance is deleted.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, StageInstanceDeleteEventArgs> StageInstanceDeleted
        {
            add => this._stageInstanceDeleted.Register(value);
            remove => this._stageInstanceDeleted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, StageInstanceDeleteEventArgs> _stageInstanceDeleted;

        #endregion

        #region Misc

        /// <summary>
        /// Fired when an interaction is invoked.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, InteractionCreateEventArgs> InteractionCreated
        {
            add => this._interactionCreated.Register(value);
            remove => this._interactionCreated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, InteractionCreateEventArgs> _interactionCreated;

        /// <summary>
        /// Fired when a component is invoked.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ComponentInteractionCreateEventArgs> ComponentInteractionCreated
        {
            add => this._componentInteractionCreated.Register(value);
            remove => this._componentInteractionCreated.Unregister(value);
        }

        private AsyncEvent<DiscordClient, ComponentInteractionCreateEventArgs> _componentInteractionCreated;

        /// <summary>
        /// Fired when a modal is submitted. If a modal is closed, this event is not fired.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ModalSubmitEventArgs> ModalSubmitted
        {
            add => this._modalSubmitted.Register(value);
            remove => this._modalSubmitted.Unregister(value);
        }

        private AsyncEvent<DiscordClient, ModalSubmitEventArgs> _modalSubmitted;

        /// <summary>
        /// Fired when a user uses a context menu.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ContextMenuInteractionCreateEventArgs> ContextMenuInteractionCreated
        {
            add => this._contextMenuInteractionCreated.Register(value);
            remove => this._contextMenuInteractionCreated.Unregister(value);
        }

        private AsyncEvent<DiscordClient, ContextMenuInteractionCreateEventArgs> _contextMenuInteractionCreated;

        /// <summary>
        /// Fired when a user starts typing in a channel.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, TypingStartEventArgs> TypingStarted
        {
            add => this._typingStarted.Register(value);
            remove => this._typingStarted.Unregister(value);
        }
        private AsyncEvent<DiscordClient, TypingStartEventArgs> _typingStarted;

        /// <summary>
        /// Fired when an unknown event gets received.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, UnknownEventArgs> UnknownEvent
        {
            add => this._unknownEvent.Register(value);
            remove => this._unknownEvent.Unregister(value);
        }
        private AsyncEvent<DiscordClient, UnknownEventArgs> _unknownEvent;

        /// <summary>
        /// Fired whenever webhooks update.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, WebhooksUpdateEventArgs> WebhooksUpdated
        {
            add => this._webhooksUpdated.Register(value);
            remove => this._webhooksUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, WebhooksUpdateEventArgs> _webhooksUpdated;

        /// <summary>
        /// Fired whenever an error occurs within an event handler.
        /// </summary>
        public event AsyncEventHandler<DiscordClient, ClientErrorEventArgs> ClientErrored
        {
            add => this._clientErrored.Register(value);
            remove => this._clientErrored.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ClientErrorEventArgs> _clientErrored;

        #endregion

        #region Error Handling

        internal void EventErrorHandler<TSender, TArgs>(AsyncEvent<TSender, TArgs> asyncEvent, Exception ex, AsyncEventHandler<TSender, TArgs> handler, TSender sender, TArgs eventArgs)
            where TArgs : AsyncEventArgs
        {
            this.Logger.LogError(LoggerEvents.EventHandlerException, ex, "Event handler exception for event {Event} thrown from {Method} (defined in {DeclaryingType})", asyncEvent.Name, handler.Method, handler.Method.DeclaringType);
            this._clientErrored.InvokeAsync(this, new ClientErrorEventArgs { EventName = asyncEvent.Name, Exception = ex }).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void Goof<TSender, TArgs>(AsyncEvent<TSender, TArgs> asyncEvent, Exception ex, AsyncEventHandler<TSender, TArgs> handler, TSender sender, TArgs eventArgs)
            where TArgs : AsyncEventArgs => this.Logger.LogCritical(LoggerEvents.EventHandlerException, ex, "Exception event handler {Method} (defined in {DeclaringType}) threw an exception", handler.Method, handler.Method.DeclaringType);

        #endregion

        /// <summary>
        /// Fired when a relationship is added (block/pending request)
        /// </summary>
        public event AsyncEventHandler<DiscordClient, RelationshipAddEventArgs> RelationshipAdded
        {
            add => this._relationshipAdded.Register(value);
            remove => this._relationshipAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, RelationshipAddEventArgs> _relationshipAdded;

        /// <summary>
        /// Fired when a relationship is removed (unfriend)
        /// </summary>
        public event AsyncEventHandler<DiscordClient, RelationshipRemoveEventArgs> RelationshipRemoved
        {
            add => this._relationshipRemoved.Register(value);
            remove => this._relationshipRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, RelationshipRemoveEventArgs> _relationshipRemoved;

        /// <summary>
        /// Fired when you log out
        /// </summary>
        public event AsyncEventHandler<DiscordClient, LoggedOutEventArgs> LoggedOut
        {
            add => this._loggedOut.Register(value);
            remove => this._loggedOut.Unregister(value);
        }
        private AsyncEvent<DiscordClient, LoggedOutEventArgs> _loggedOut;

        public event AsyncEventHandler<DiscordClient, ReadStateUpdateEventArgs> ReadStateUpdated
        {
            add => this._readStateUpdated.Register(value);
            remove => this._readStateUpdated.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ReadStateUpdateEventArgs> _readStateUpdated;


        public event AsyncEventHandler<DiscordClient, ChannelUnreadUpdateEventArgs> ChannelUnreadUpdated
        {
            add => this._channelUnreadUpdate.Register(value);
            remove => this._channelUnreadUpdate.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelUnreadUpdateEventArgs> _channelUnreadUpdate;

        public event AsyncEventHandler<DiscordClient, ChannelRecipientAddedEventArgs> ChannelRecipientAdded
        {
            add => this._channelRecipientAdded.Register(value);
            remove => this._channelRecipientAdded.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelRecipientAddedEventArgs> _channelRecipientAdded;

        public event AsyncEventHandler<DiscordClient, ChannelRecipientRemovedEventArgs> ChannelRecipientRemoved
        {
            add => this._channelRecipientRemoved.Register(value);
            remove => this._channelRecipientRemoved.Unregister(value);
        }
        private AsyncEvent<DiscordClient, ChannelRecipientRemovedEventArgs> _channelRecipientRemoved;

        /// <summary>
        /// Fired when Discord provides an updated authentication token
        /// </summary>
        public event AsyncEventHandler<DiscordClient, AuthTokenUpdatedEventArgs> AuthTokenUpdate
        {
            add => this._authTokenUpdate.Register(value);
            remove => this._authTokenUpdate.Unregister(value);
        }
        private AsyncEvent<DiscordClient, AuthTokenUpdatedEventArgs> _authTokenUpdate;

    }
}
