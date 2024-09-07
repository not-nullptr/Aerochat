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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Net;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.Models;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    /// <summary>
    /// Represents a Discord guild member.
    /// </summary>
    public class DiscordMember : DiscordUser, IEquatable<DiscordMember>
    {
        internal DiscordMember()
        {
            this.IsLocal = true;
        }

        internal DiscordMember(DiscordUser user)
        {
            this.Discord = user.Discord;

            this.Id = user.Id;
            this.IsLocal = true;

            this._role_ids = new List<ulong>();
        }

        internal DiscordMember(TransportMember member)
        {
            this.Id = member.User?.Id ?? member.UserId;
            this.IsDeafened = member.IsDeafened;
            this.IsMuted = member.IsMuted;
            this.JoinedAt = member.JoinedAt;
            this.Nickname = member.Nickname;
            this.PremiumSince = member.PremiumSince;
            this.IsPending = member.IsPending;
            this._avatarHash = member.AvatarHash;
            this._role_ids = member.Roles ?? new List<ulong>();
            this.CommunicationDisabledUntil = member.CommunicationDisabledUntil;
            this.IsLocal = false;
        }

        [JsonIgnore]
        public bool IsLocal { get; internal set; }

        /// <summary>
        /// Gets the member's avatar for the current guild.
        /// </summary>
        [JsonIgnore]
        public string GuildAvatarHash => this._avatarHash;

        /// <summary>
        /// Gets the members avatar url for the current guild.
        /// </summary>
        [JsonIgnore]
        public string GuildAvatarUrl => string.IsNullOrWhiteSpace(this.GuildAvatarHash) ? null : $"https://cdn.discordapp.com{Endpoints.GUILDS}/{this._guild_id}{Endpoints.USERS}/{this.Id}{Endpoints.AVATARS}/{this.GuildAvatarHash}.{(this.GuildAvatarHash.StartsWith("a_") ? "gif" : "png")}?size=1024";

        [JsonIgnore]
        internal string _avatarHash;

        /// <summary>
        /// Gets this member's nickname.
        /// </summary>
        [JsonProperty("nick", NullValueHandling = NullValueHandling.Ignore)]
        public string Nickname { get; internal set; }

        /// <summary>
        /// Gets this member's display name.
        /// </summary>
        [JsonIgnore]
        public override string DisplayName => this.Nickname ?? base.DisplayName;

        [JsonIgnore]
        public override string GlobalName
            => this.User.GlobalName;

        /// <summary>
        /// How long this member's communication will be suppressed for.
        /// </summary>
        [JsonProperty("communication_disabled_until", NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? CommunicationDisabledUntil { get; internal set; }

        /// <summary>
        /// List of role IDs
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<ulong> RoleIds =>
            this._role_ids;

        [JsonProperty("roles", NullValueHandling = NullValueHandling.Ignore)]
        internal List<ulong> _role_ids;

        /// <summary>
        /// Gets the list of roles associated with this member.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<DiscordRole> Roles
            => this.RoleIds.Select(id => this.Guild.GetRole(id)).Where(x => x != null);

        /// <summary>
        /// Gets the color associated with this user's top color-giving role, otherwise 0 (no color).
        /// </summary>
        [JsonIgnore]
        public DiscordColor Color
        {
            get
            {
                var role = this.Roles.OrderByDescending(xr => xr.Position).FirstOrDefault(xr => xr.Color.Value != 0);
                return role != null ? role.Color : new DiscordColor();
            }
        }

        /// <summary>
        /// Date the user joined the guild
        /// </summary>
        [JsonProperty("joined_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset JoinedAt { get; internal set; }

        /// <summary>
        /// Date the user started boosting this server
        /// </summary>
        [JsonProperty("premium_since", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? PremiumSince { get; internal set; }

        /// <summary>
        /// If the user is deafened
        /// </summary>
        [JsonProperty("is_deafened", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsDeafened { get; internal set; }

        /// <summary>
        /// If the user is muted
        /// </summary>
        [JsonProperty("is_muted", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsMuted { get; internal set; }

        /// <summary>
        /// If the user has passed the guild's Membership Screening requirements
        /// </summary>
        [JsonProperty("pending", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPending { get; internal set; }

        /// <summary>
        /// Gets this member's voice state.
        /// </summary>
        [JsonIgnore]
        public DiscordVoiceState VoiceState
            => this.Discord.Guilds[this._guild_id].VoiceStates.TryGetValue(this.Id, out var voiceState) ? voiceState : null;

        [JsonIgnore]
        internal ulong _guild_id = 0;

        /// <summary>
        /// Gets the guild of which this member is a part of.
        /// </summary>
        [JsonIgnore]
        public DiscordGuild Guild
            => this.Discord.Guilds[this._guild_id];

        /// <summary>
        /// Gets whether this member is the Guild owner.
        /// </summary>
        [JsonIgnore]
        public bool IsOwner
            => this.Id == this.Guild.OwnerId;

        /// <summary>
        /// Gets the member's position in the role hierarchy, which is the member's highest role's position. Returns <see cref="int.MaxValue"/> for the guild's owner.
        /// </summary>
        [JsonIgnore]
        public int Hierarchy
            => this.IsOwner ? int.MaxValue : this.RoleIds.Count == 0 ? 0 : this.Roles.Max(x => x.Position);


        /// <summary>
        /// Gets the permissions for the current member.
        /// </summary>
        [JsonIgnore]
        public Permissions Permissions => this.GetPermissions();



        #region Overridden user properties
        [JsonIgnore]
        internal DiscordUser User
            => this.Discord.UserCache[this.Id];

        /// <summary>
        /// Gets this member's username.
        /// </summary>
        [JsonIgnore]
        public override string Username
        {
            get => this.User.Username;
            internal set => this.User.Username = value;
        }

        /// <summary>
        /// Gets the member's 4-digit discriminator.
        /// </summary>
        [JsonIgnore]
        public override string Discriminator
        {
            get => this.User.Discriminator;
            internal set => this.User.Discriminator = value;
        }

        /// <summary>
        /// Gets the member's banner hash.
        /// </summary>
        [JsonIgnore]
        public override string BannerHash
        {
            get => this.User.BannerHash;
            internal set => this.User.BannerHash = value;
        }

        /// <summary>
        /// The color of this member's banner. Mutually exclusive with <see cref="BannerHash"/>.
        /// </summary>
        [JsonIgnore]
        public override DiscordColor? BannerColor => this.User.BannerColor;

        /// <summary>
        /// Gets the member's avatar hash.
        /// </summary>
        [JsonIgnore]
        public override string AvatarHash
        {
            get => this.User.AvatarHash;
            internal set => this.User.AvatarHash = value;
        }

        /// <summary>
        /// Gets whether the member is a bot.
        /// </summary>
        [JsonIgnore]
        public override bool IsBot
        {
            get => this.User.IsBot;
            internal set => this.User.IsBot = value;
        }

        /// <summary>
        /// Gets the member's email address.
        /// <para>This is only present in OAuth.</para>
        /// </summary>
        [JsonIgnore]
        public override string Email
        {
            get => this.User.Email;
            internal set => this.User.Email = value;
        }

        /// <summary>
        /// Gets whether the member has multi-factor authentication enabled.
        /// </summary>
        [JsonIgnore]
        public override bool? MfaEnabled
        {
            get => this.User.MfaEnabled;
            internal set => this.User.MfaEnabled = value;
        }

        /// <summary>
        /// Gets whether the member is verified.
        /// <para>This is only present in OAuth.</para>
        /// </summary>
        [JsonIgnore]
        public override bool? Verified
        {
            get => this.User.Verified;
            internal set => this.User.Verified = value;
        }

        /// <summary>
        /// Gets the member's chosen language
        /// </summary>
        [JsonIgnore]
        public override string Locale
        {
            get => this.User.Locale;
            internal set => this.User.Locale = value;
        }

        /// <summary>
        /// Gets the user's flags.
        /// </summary>
        [JsonIgnore]
        public override UserFlags? OAuthFlags
        {
            get => this.User.OAuthFlags;
            internal set => this.User.OAuthFlags = value;
        }

        /// <summary>
        /// Gets the member's flags for OAuth.
        /// </summary>
        [JsonIgnore]
        public override UserFlags? Flags
        {
            get => this.User.Flags;
            internal set => this.User.Flags = value;
        }
        #endregion

        /// <summary>
        /// Creates a direct message channel to this member.
        /// </summary>
        /// <returns>Direct message channel to this member.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the member has the bot blocked, the member is no longer in the guild, or if the member has Allow DM from server members off.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task<DiscordDmChannel> CreateDmChannelAsync()
        {
            DiscordDmChannel dm = default;

            if (this.Discord is DiscordClient dc)
                dm = dc._privateChannels.Values.FirstOrDefault(x => x.Recipients.FirstOrDefault() == this);

            return dm != null ? Task.FromResult(dm) : this.Discord.ApiClient.CreateDmAsync(this.Id);
        }

        /// <summary>
        /// Sends a direct message to this member. Creates a direct message channel if one does not exist already.
        /// </summary>
        /// <param name="content">Content of the message to send.</param>
        /// <returns>The sent message.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the member has the bot blocked, the member is no longer in the guild, or if the member has Allow DM from server members off.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordMessage> SendMessageAsync(string content)
        {
            if (this.IsBot && this.Discord.CurrentUser.IsBot)
                throw new ArgumentException("Bots cannot DM each other.");

            var chn = await this.CreateDmChannelAsync().ConfigureAwait(false);
            return await chn.SendMessageAsync(content).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a direct message to this member. Creates a direct message channel if one does not exist already.
        /// </summary>
        /// <param name="embed">Embed to attach to the message.</param>
        /// <returns>The sent message.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the member has the bot blocked, the member is no longer in the guild, or if the member has Allow DM from server members off.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordMessage> SendMessageAsync(DiscordEmbed embed)
        {
            if (this.IsBot && this.Discord.CurrentUser.IsBot)
                throw new ArgumentException("Bots cannot DM each other.");

            var chn = await this.CreateDmChannelAsync().ConfigureAwait(false);
            return await chn.SendMessageAsync(embed).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a direct message to this member. Creates a direct message channel if one does not exist already.
        /// </summary>
        /// <param name="content">Content of the message to send.</param>
        /// <param name="embed">Embed to attach to the message.</param>
        /// <returns>The sent message.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the member has the bot blocked, the member is no longer in the guild, or if the member has Allow DM from server members off.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordMessage> SendMessageAsync(string content, DiscordEmbed embed)
        {
            if (this.IsBot && this.Discord.CurrentUser.IsBot)
                throw new ArgumentException("Bots cannot DM each other.");

            var chn = await this.CreateDmChannelAsync().ConfigureAwait(false);
            return await chn.SendMessageAsync(content, embed).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends a direct message to this member. Creates a direct message channel if one does not exist already.
        /// </summary>
        /// <param name="message">Builder to with the message.</param>
        /// <returns>The sent message.</returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the member has the bot blocked, the member is no longer in the guild, or if the member has Allow DM from server members off.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task<DiscordMessage> SendMessageAsync(DiscordMessageBuilder message)
        {
            if (this.IsBot && this.Discord.CurrentUser.IsBot)
                throw new ArgumentException("Bots cannot DM each other.");

            var chn = await this.CreateDmChannelAsync().ConfigureAwait(false);
            return await chn.SendMessageAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Times-out a member and restricts their ability to send messages, add reactions, speak in threads, and join voice channels.
        /// </summary>
        /// <param name="until">How long the timeout should last. Set to <see langword="null"/> or a time in the past to remove the timeout.</param>
        /// <param name="reason">Why this member is being restricted.</param>
        public Task TimeoutAsync(DateTimeOffset? until, string reason = default)
            => this.Discord.ApiClient.ModifyGuildMemberAsync(this._guild_id, this.Id, default, default, default, default, default, until, reason);

        /// <summary>
        /// Sets this member's voice mute status.
        /// </summary>
        /// <param name="mute">Whether the member is to be muted.</param>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.MuteMembers"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task SetMuteAsync(bool mute, string reason = null)
            => this.Discord.ApiClient.ModifyGuildMemberAsync(this._guild_id, this.Id, default, default, mute, default, default, default, reason);

        /// <summary>
        /// Sets this member's voice deaf status.
        /// </summary>
        /// <param name="deaf">Whether the member is to be deafened.</param>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.DeafenMembers"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task SetDeafAsync(bool deaf, string reason = null)
            => this.Discord.ApiClient.ModifyGuildMemberAsync(this._guild_id, this.Id, default, default, default, deaf, default, default, reason);

        /// <summary>
        /// Modifies this member.
        /// </summary>
        /// <param name="action">Action to perform on this member.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.ManageNicknames"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public async Task ModifyAsync(Action<MemberEditModel> action)
        {
            var mdl = new MemberEditModel();
            action(mdl);

            if (mdl.VoiceChannel.HasValue && mdl.VoiceChannel.Value != null && mdl.VoiceChannel.Value.Type != ChannelType.Voice && mdl.VoiceChannel.Value.Type != ChannelType.Stage)
                throw new ArgumentException("Given channel is not a voice or stage channel.", nameof(mdl.VoiceChannel));

            if (mdl.Nickname.HasValue && this.Discord.CurrentUser.Id == this.Id)
            {
                await this.Discord.ApiClient.ModifyCurrentMemberAsync(this.Guild.Id, mdl.Nickname.Value,
                    mdl.AuditLogReason).ConfigureAwait(false);

                await this.Discord.ApiClient.ModifyGuildMemberAsync(this.Guild.Id, this.Id, Optional.FromNoValue<string>(),
                    mdl.Roles.IfPresent(e => e.Select(xr => xr.Id)), mdl.Muted, mdl.Deafened,
                    mdl.VoiceChannel.IfPresent(e => e?.Id), default, mdl.AuditLogReason).ConfigureAwait(false);
            }
            else
            {
                await this.Discord.ApiClient.ModifyGuildMemberAsync(this.Guild.Id, this.Id, mdl.Nickname,
                    mdl.Roles.IfPresent(e => e.Select(xr => xr.Id)), mdl.Muted, mdl.Deafened,
                    mdl.VoiceChannel.IfPresent(e => e?.Id), mdl.CommunicationDisabledUntil, mdl.AuditLogReason).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Grants a role to the member.
        /// </summary>
        /// <param name="role">Role to grant.</param>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.ManageRoles"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task GrantRoleAsync(DiscordRole role, string reason = null)
            => this.Discord.ApiClient.AddGuildMemberRoleAsync(this.Guild.Id, this.Id, role.Id, reason);

        /// <summary>
        /// Revokes a role from a member.
        /// </summary>
        /// <param name="role">Role to revoke.</param>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.ManageRoles"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task RevokeRoleAsync(DiscordRole role, string reason = null)
            => this.Discord.ApiClient.RemoveGuildMemberRoleAsync(this.Guild.Id, this.Id, role.Id, reason);

        /// <summary>
        /// Sets the member's roles to ones specified.
        /// </summary>
        /// <param name="roles">Roles to set.</param>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.ManageRoles"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task ReplaceRolesAsync(IEnumerable<DiscordRole> roles, string reason = null)
            => this.Discord.ApiClient.ModifyGuildMemberAsync(this.Guild.Id, this.Id, default,
                new Optional<IEnumerable<ulong>>(roles.Select(xr => xr.Id)), default, default, default, default, reason);

        /// <summary>
        /// Bans a this member from their guild.
        /// </summary>
        /// <param name="delete_message_days">How many days to remove messages from.</param>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.BanMembers"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task BanAsync(int delete_message_days = 0, string reason = null)
            => this.Guild.BanMemberAsync(this, delete_message_days, reason);

        /// <exception cref = "Exceptions.UnauthorizedException" > Thrown when the client does not have the<see cref="Permissions.BanMembers"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task UnbanAsync(string reason = null) => this.Guild.UnbanMemberAsync(this, reason);

        /// <summary>
        /// Kicks this member from their guild.
        /// </summary>
        /// <param name="reason">Reason for audit logs.</param>
        /// <returns></returns>
        /// <remarks>[alias="KickAsync"]</remarks>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.KickMembers"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task RemoveAsync(string reason = null)
            => this.Discord.ApiClient.RemoveGuildMemberAsync(this._guild_id, this.Id, reason);

        /// <summary>
        /// Moves this member to the specified voice channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        /// <exception cref="Exceptions.UnauthorizedException">Thrown when the client does not have the <see cref="Permissions.MoveMembers"/> permission.</exception>
        /// <exception cref="Exceptions.NotFoundException">Thrown when the member does not exist.</exception>
        /// <exception cref="Exceptions.BadRequestException">Thrown when an invalid parameter was provided.</exception>
        /// <exception cref="Exceptions.ServerErrorException">Thrown when Discord is unable to process the request.</exception>
        public Task PlaceInAsync(DiscordChannel channel)
            => channel.PlaceMemberAsync(this);

        /// <summary>
        /// Updates the member's suppress state in a stage channel.
        /// </summary>
        /// <param name="channel">The channel the member is currently in.</param>
        /// <param name="suppress">Toggles the member's suppress state.</param>
        /// <exception cref="ArgumentException">Thrown when the channel in not a voice channel.</exception>
        public async Task UpdateVoiceStateAsync(DiscordChannel channel, bool? suppress)
        {
            if (channel.Type != ChannelType.Stage)
                throw new ArgumentException("Voice state can only be updated in a stage channel.");

            await this.Discord.ApiClient.UpdateUserVoiceStateAsync(this.Guild.Id, this.Id, channel.Id, suppress).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates permissions in a given channel for this member.
        /// </summary>
        /// <param name="channel">Channel to calculate permissions for.</param>
        /// <returns>Calculated permissions for this member in the channel.</returns>
        public Permissions PermissionsIn(DiscordChannel channel)
            => channel?.PermissionsFor(this) ?? this.GetDefaultPermissions();

        private Permissions GetDefaultPermissions()
        {
            // default permissions
            const Permissions def = Permissions.None;

            if (this.Guild == null)
            {
                return def;
            }

            if (this.Guild.OwnerId == this.Id)
            {
                return ~def;
            }

            Permissions perms;

            // assign @everyone permissions
            var everyoneRole = this.Guild.EveryoneRole;
            perms = everyoneRole.Permissions;

            // roles that member is in
            var mbRoles = this.Roles.ToArray();
            perms |= mbRoles.Aggregate(def, (c, role) => c | role.Permissions);

            return perms;
        }

        /// <summary>
        /// Constructs the url for a guild member's avatar, defaulting to the user's avatar if none is set.
        /// </summary>
        /// <param name="imageFormat">The image format of the avatar to get.</param>
        /// <param name="imageSize">The maximum size of the avatar. Must be a power of two, minimum 16, maximum 4096.</param>
        /// <returns>The URL of the user's avatar.</returns>
        public string GetGuildAvatarUrl(ImageFormat imageFormat, ushort imageSize = 1024)
        {
            // Run this if statement before any others to prevent running the if statements twice.
            if (string.IsNullOrWhiteSpace(this.GuildAvatarHash))
                return this.GetAvatarUrl(imageFormat, imageSize);

            if (imageFormat == ImageFormat.Unknown)
                throw new ArgumentException("You must specify valid image format.", nameof(imageFormat));

            // Makes sure the image size is in between Discord's allowed range.
            if (imageSize < 16 || imageSize > 4096)
                throw new ArgumentOutOfRangeException("Image Size is not in between 16 and 4096: " + nameof(imageSize));

            // Checks to see if the image size is not a power of two.
            if (!(imageSize is not 0 && (imageSize & (imageSize - 1)) is 0))
                throw new ArgumentOutOfRangeException("Image size is not a power of two: " + nameof(imageSize));

            // Get the string variants of the method parameters to use in the urls.
            var stringImageFormat = imageFormat switch
            {
                ImageFormat.Gif => "gif",
                ImageFormat.Jpeg => "jpg",
                ImageFormat.Png => "png",
                ImageFormat.WebP => "webp",
                ImageFormat.Auto => !string.IsNullOrWhiteSpace(this.GuildAvatarHash) ? (this.GuildAvatarHash.StartsWith("a_") ? "gif" : "png") : "png",
                _ => throw new ArgumentOutOfRangeException(nameof(imageFormat)),
            };
            var stringImageSize = imageSize.ToString(CultureInfo.InvariantCulture);

            return $"https://cdn.discordapp.com{Endpoints.GUILDS}/{this._guild_id}{Endpoints.USERS}/{this.Id}{Endpoints.AVATARS}/{this.GuildAvatarHash}.{stringImageFormat}?size={stringImageSize}";
        }


        /// <summary>
        /// Returns a string representation of this member.
        /// </summary>
        /// <returns>String representation of this member.</returns>
        public override string ToString() => $"Member {this.Id}; {this.Username}#{this.Discriminator} ({this.DisplayName})";

        /// <summary>
        /// Checks whether this <see cref="DiscordMember"/> is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns>Whether the object is equal to this <see cref="DiscordMember"/>.</returns>
        public override bool Equals(object obj) => this.Equals(obj as DiscordMember);

        /// <summary>
        /// Checks whether this <see cref="DiscordMember"/> is equal to another <see cref="DiscordMember"/>.
        /// </summary>
        /// <param name="e"><see cref="DiscordMember"/> to compare to.</param>
        /// <returns>Whether the <see cref="DiscordMember"/> is equal to this <see cref="DiscordMember"/>.</returns>
        public bool Equals(DiscordMember e)
        {
            if (e is null)
                return false;

            return ReferenceEquals(this, e) || (this.Id == e.Id && this._guild_id == e._guild_id);
        }

        /// <summary>
        /// Gets the hash code for this <see cref="DiscordMember"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="DiscordMember"/>.</returns>
        public override int GetHashCode()
        {
            var hash = 13;

            hash = (hash * 7) + this.Id.GetHashCode();
            hash = (hash * 7) + this._guild_id.GetHashCode();

            return hash;
        }

        /// <summary>
        /// Gets whether the two <see cref="DiscordMember"/> objects are equal.
        /// </summary>
        /// <param name="e1">First member to compare.</param>
        /// <param name="e2">Second member to compare.</param>
        /// <returns>Whether the two members are equal.</returns>
        public static bool operator ==(DiscordMember e1, DiscordMember e2)
        {
            var o1 = e1 as object;
            var o2 = e2 as object;

            if ((o1 == null && o2 != null) || (o1 != null && o2 == null))
                return false;

            return (o1 == null && o2 == null) || (e1.Id == e2.Id && e1._guild_id == e2._guild_id);
        }

        /// <summary>
        /// Gets whether the two <see cref="DiscordMember"/> objects are not equal.
        /// </summary>
        /// <param name="e1">First member to compare.</param>
        /// <param name="e2">Second member to compare.</param>
        /// <returns>Whether the two members are not equal.</returns>
        public static bool operator !=(DiscordMember e1, DiscordMember e2)
            => !(e1 == e2);

        /// <summary>
        /// Get's the current member's roles based on the sum of the permissions of their given roles.
        /// </summary>
        private Permissions GetPermissions()
        {
            if (this.Guild.OwnerId == this.Id)
                return PermissionMethods.FULL_PERMS;

            Permissions perms;

            // assign @everyone permissions
            var everyoneRole = this.Guild.EveryoneRole;
            perms = everyoneRole.Permissions;

            // assign permissions from member's roles (in order)
            perms |= this.Roles.Aggregate(Permissions.None, (c, role) => c | role.Permissions);

            // Administrator grants all permissions and cannot be overridden
            if ((perms & Permissions.Administrator) == Permissions.Administrator)
                return PermissionMethods.FULL_PERMS;

            return perms;
        }
    }
}
