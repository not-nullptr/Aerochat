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

namespace DSharpPlus
{
    /// <summary>
    /// Represents the type of a message.
    /// </summary>
    public enum MessageType : int
    {
        /// <summary>
        /// Indicates a regular message.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Message indicating a recipient was added to a group direct message.
        /// </summary>
        RecipientAdd = 1,

        /// <summary>
        /// Message indicating a recipient was removed from a group direct message.
        /// </summary>
        RecipientRemove = 2,

        /// <summary>
        /// Message indicating a call.
        /// </summary>
        Call = 3,

        /// <summary>
        /// Message indicating a group direct message channel rename.
        /// </summary>
        ChannelNameChange = 4,

        /// <summary>
        /// Message indicating a group direct message channel icon change.
        /// </summary>
        ChannelIconChange = 5,

        /// <summary>
        /// Message indicating a user pinned a message to a channel.
        /// </summary>
        ChannelPinnedMessage = 6,

        /// <summary>
        /// Message indicating a guild member joined. Most frequently seen in newer, smaller guilds.
        /// </summary>
        GuildMemberJoin = 7,

        /// <summary>
        /// Message indicating a member nitro boosted a guild.
        /// </summary>
        UserPremiumGuildSubscription = 8,

        /// <summary>
        /// Message indicating a guild reached tier one of nitro boosts.
        /// </summary>
        TierOneUserPremiumGuildSubscription = 9,

        /// <summary>
        /// Message indicating a guild reached tier two of nitro boosts.
        /// </summary>
        TierTwoUserPremiumGuildSubscription = 10,

        /// <summary>
        /// Message indicating a guild reached tier three of nitro boosts.
        /// </summary>
        TierThreeUserPremiumGuildSubscription = 11,

        /// <summary>
        /// Message indicating a user followed a news channel.
        /// </summary>
        ChannelFollowAdd = 12,

        [Obsolete]
        GuildStream = 13,

        /// <summary>
        /// Message indicating a guild was removed from guild discovery.
        /// </summary>
        GuildDiscoveryDisqualified = 14,

        /// <summary>
        /// Message indicating a guild was re-added to guild discovery.
        /// </summary>
        GuildDiscoveryRequalified = 15,

        /// <summary>
        /// Message indicating that a guild has failed to meet guild discovery requirements for a week.
        /// </summary>
        GuildDiscoveryGracePeriodInitialWarning = 16,

        /// <summary>
        /// Message indicating that a guild has failed to meet guild discovery requirements for 3 weeks.
        /// </summary>
        GuildDiscoveryGracePeriodFinalWarning = 17,

        /// <summary>
        /// Message sent when a thread is created	
        /// </summary>
        ThreadCreated = 18,

        /// <summary>
        /// Message indicating a user replied to another user.
        /// </summary>
        Reply = 19,

        /// <summary>
        /// Message indicating an application command was invoked.
        /// </summary>
        ApplicationCommand = 20,

        /// <summary>
        /// A message sent when a thread starter message is added to a thread	
        /// </summary>
        ThreadStarterMessage = 21,

        /// <summary>
        /// Message reminding you to invite people to help you build the server.
        /// </summary>
        GuildInviteReminder = 22,

        /// <summary>
        /// Message indicating a context menu was executed.
        /// </summary>
        ContextMenuCommand = 23,

        /// <summary>
        /// Message indicating an AutoMod alert.
        /// </summary>
        AutoModAlert = 24,

        /// <summary>
        /// A message sent when a user purchases or renews a role subscription.
        /// </summary>
        RoleSubscriptionPurchase = 25,

        /// <summary>
        /// A message sent when a user is upsold to a premium interaction.
        /// </summary>
        InteractionPremiumUpsell = 26,

        /// <summary>
        /// A message sent when a stage channel starts.
        /// </summary>
        StageStart = 27,

        /// <summary>
        /// A message sent when a stage channel ends.
        /// </summary>
        StageEnd = 28,

        /// <summary>
        /// A message sent when a user starts speaking in a stage channel.
        /// </summary>
        StageSpeaker = 29,

        /// <summary>
        /// A message sent when a user raises their hand in a stage channel.
        /// </summary>
        StageRaiseHand = 30,

        /// <summary>
        /// A message sent when a stage channel's topic is changed.
        /// </summary>
        StageTopic = 31,

        /// <summary>
        /// A message sent when a user purchases an application premium subscription.
        /// </summary>
        GuildApplicationPremiumSubscription = 32,

        /// <summary>
        /// A message sent when a user adds an application to group DM.
        /// </summary>
        [Obsolete]
        PrivateChannelIntegrationAdded = 33,

        /// <summary>
        /// A message sent when a user removed an application from a group DM.
        /// </summary>
        [Obsolete]
        PrivateChannelIntegrationRemoved = 34,

        /// <summary>
        /// A message sent when a user gifts a premium (Nitro) referral.
        /// </summary>
        PremiumReferral = 35,

        /// <summary>
        /// A message sent when a user enables lockdown for the guild.
        /// </summary>
        GuildIncidentAlertModeEnabled = 36,

        /// <summary>
        /// A message sent when a user disables lockdown for the guild.
        /// </summary>
        GuildIncidentAlertModeDisabled = 37,

        /// <summary>
        /// A message sent when a user reports a raid for the guild.
        /// </summary>
        GuildIncidentReportRaid = 38,

        /// <summary>
        /// A message sent when a user reports a false alarm for the guild.
        /// </summary>
        GuildIncidentReportFalseAlarm = 39,

        /// <summary>
        /// A message sent when no one sends a message in the current channel for 1 hour.
        /// </summary>
        GuildDeadChatRevivePrompt = 40, // ew

        /// <summary>
        /// A message sent when a user buys another user a gift.
        /// </summary>
        CustomGift = 41,

        /// <summary>
        /// ???
        /// </summary>
        GuildGamingStatsPrompt = 42,

        /// <summary>
        /// A message sent when a user posts a poll, except not anymore.
        /// </summary>
        [Obsolete]
        Poll =43,

        /// <summary>
        /// A message sent when a user purchases a guild product.
        /// </summary>
        PurchaseNotification = 44,

        /// <summary>
        /// A message sent when a user invites another user to hangout in a voice channel.
        /// </summary>
        [Obsolete]
        VoiceHangoutInvite = 45,

        /// <summary>
        /// A message sent when a poll is finalized.
        /// </summary>
        PollResult = 46,

        /// <summary>
        /// A message sent by the Discord Updates account when a new changelog is posted.
        /// </summary>
        ChangeLog = 47,

        /// <summary>
        /// A message sent when a Nitro promotion is triggered.
        /// </summary>
        NitroNotification = 48
    }
}
