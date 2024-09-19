using DSharpPlus.Entities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Aerochat.Controls
{
    public class MessageParser : UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(DiscordMessage), typeof(MessageParser), new PropertyMetadata(null, OnMessageChanged));

        public DiscordMessage Message
        {
            get { return (DiscordMessage)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public event EventHandler<HyperlinkClickedEventArgs> HyperlinkClicked;

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MessageParser)d;
            control.RenderMessage();
        }

        private void RenderMessage()
        {
            MainPanel.Children.Clear();
            if (Message == null)
            {
                return;
            }
            var parts = Message.Content.Split(' ');
            foreach (var part in parts)
            {
                string text = part;
                if (part.StartsWith("<") && part.EndsWith(">"))
                {
                    string id = part.Replace("<", "").Replace(">", "");
                    var link = new Hyperlink();
                    HyperlinkType? type = null;
                    object associatedObject = null;

                    switch (id.ElementAt(0))
                    {
                        case '@':
                            id = id.Replace("@", "");
                            switch (id.ElementAt(0))
                            {
                                case '&':
                                    {
                                        id = id.Replace("&", "");
                                        if (!ulong.TryParse(id, out ulong parsedId)) break;
                                        var role = Message.MentionedRoles.FirstOrDefault(x => x?.Id == parsedId);
                                        if (role == null)
                                        {
                                            text = "@unknown-role";
                                            break;
                                        }
                                        link.Inlines.Add($"@{role.Name} ");
                                        type = HyperlinkType.Role;
                                        associatedObject = role;
                                        break;
                                    }
                                default:
                                    {
                                        if (!ulong.TryParse(id, out ulong parsedId)) break;
                                        var user = Message.MentionedUsers.FirstOrDefault(x => x?.Id == parsedId);
                                        if (user == null)
                                        {
                                            text = "@unknown-user";
                                            break;
                                        }
                                        link.Inlines.Add($"@{user.DisplayName} ");
                                        type = HyperlinkType.User;
                                        associatedObject = user;
                                        break;
                                    }
                            }
                            break;
                        case '#':
                            {
                                id = id.Replace("#", "");
                                if (!ulong.TryParse(id, out ulong parsedId)) break;
                                var channel = Message.MentionedChannels.FirstOrDefault(x => x?.Id == parsedId);
                                if (channel == null)
                                {
                                    text = "#unknown-channel";
                                    break;
                                }
                                link.Inlines.Add($"#{channel.Name} ");
                                type = HyperlinkType.Channel;
                                associatedObject = channel;
                                break;
                            }
                    }

                    if (link.Inlines.Count > 0 && type != null)
                    {
                        link.Click += (s, e) => OnHyperlinkClicked(type.Value, associatedObject);
                        var tb = new TextBlock();
                        tb.Inlines.Add(link);
                        MainPanel.Children.Add(tb);
                        continue;
                    }
                }

                var textBlock = new TextBlock();
                textBlock.Text = text + " ";
                MainPanel.Children.Add(textBlock);
            }
        }

        private void OnHyperlinkClicked(HyperlinkType type, object associatedObject)
        {
            HyperlinkClicked?.Invoke(this, new HyperlinkClickedEventArgs(type, associatedObject));
        }

        public WrapPanel MainPanel { get; set; }

        public MessageParser()
        {
            MainPanel = new WrapPanel();
            Content = MainPanel;
        }
    }

    public enum HyperlinkType
    {
        Channel,
        Role,
        User
    }

    public class HyperlinkClickedEventArgs : EventArgs
    {
        public HyperlinkType Type { get; }
        public object AssociatedObject { get; }

        public HyperlinkClickedEventArgs(HyperlinkType type, object associatedObject)
        {
            Type = type;
            AssociatedObject = associatedObject;
        }
    }
}
