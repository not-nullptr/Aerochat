using Aerochat.Hoarder;
using Aerochat.ViewModels;
using Aerochat.Windows;
using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Aerochat.Helpers
{
    public class OpenChatQueue
    {
        private static OpenChatQueue _instance = new();

        public static OpenChatQueue Instance
        {
            get { return _instance; }
            private set { }
        }

        public bool ExecuteOnAdd { get; set; }

        public enum EntryType
        {
            Dm,
            Guild,
        }

        struct Entry
        {
            public EntryType type;
            public ulong id;
        }

        List<Entry> _entries = new();

        public void AddEntry(EntryType type, ulong id)
        {
            _entries.Add(new Entry
            {
                type = type,
                id = id
            });

            if (ExecuteOnAdd)
                ExecuteQueue();
        }

        public void ExecuteQueue()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    Home? homeWindow = Application.Current.Windows.OfType<Home>().FirstOrDefault();

                    foreach (Entry entry in _entries)
                    {
                        PresenceViewModel? initialPresence = null;

                        ulong channelId = entry.id;

                        if (entry.type == EntryType.Dm)
                        {
                            if (homeWindow is not null)
                            {
                                initialPresence = homeWindow.FindPresenceForUserId(entry.id);
                            }
                        }
                        else if (entry.type == EntryType.Guild)
                        {
                            Discord.Client.TryGetCachedGuild(entry.id, out var guild);
                            if (guild == null) continue;

                            var channels = guild.Channels.Values;
                            List<DiscordChannel> channelsList = new();
                            foreach (var c in channels)
                            {
                                if ((c.PermissionsFor(guild.CurrentMember) & Permissions.AccessChannels) == Permissions.AccessChannels && c.Type == ChannelType.Text)
                                {
                                    channelsList.Add(c);
                                }
                            }

                            channelId = channelsList[0].Id;
                        }

                        new Chat(channelId, true, initialPresence, null);
                    }
                }
                catch (Exception ex)
                {
                    // Ignore.
                }
                finally
                {
                    _entries.Clear();
                }
            });
        }
    }
}
