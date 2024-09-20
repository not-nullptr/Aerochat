using DSharpPlus.Entities;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Aerochat.Controls
{
    public class MessageParser : UserControl
    {
        public static Dictionary<string, BitmapSource> EmojiCache = new();

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
            // dispose of all emoji images
            foreach (var image in EmojiCache.Values)
            {
                image.Freeze();
            }
            EmojiCache.Clear();
            if (Message == null)
            {
                return;
            }
            var textBlock = new TextBlock();
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

                    // if there's no element at 0, continue
                    if (id.Length == 0) break;
                    switch (id.ElementAt(0))
                    {
                        case '@':
                            id = id.Replace("@", "");
                            if (id.Length == 0) break;
                            switch (id.ElementAt(0))
                            {
                                case '&':
                                    {
                                        id = id.Replace("&", "");
                                        if (!ulong.TryParse(id, out ulong parsedId)) break;
                                        var role = Message.MentionedRoles?.FirstOrDefault(x => x?.Id == parsedId);
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
                                        var user = Message.MentionedUsers?.FirstOrDefault(x => x?.Id == parsedId);
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
                                var channel = Message.MentionedChannels?.FirstOrDefault(x => x?.Id == parsedId);
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
                        case 't':
                            {
                                id = id.Replace("t:", "");
                                string date_format = "MMMM dd, yyyy hh:mm tt";
                                bool relative = false; //ONLY for :R

                                if(id.EndsWith(":t"))
                                {
                                    id = id.Replace(":t", "");
                                    date_format = "hh:mm tt";
                                }
                                else if (id.EndsWith(":T"))
                                {
                                    id = id.Replace(":T", "");
                                    date_format = "hh:mm:ss tt";
                                }
                                else if (id.EndsWith(":d"))
                                {
                                    id = id.Replace(":d", "");
                                    date_format = "MM/dd/yyyy";
                                }
                                else if (id.EndsWith(":D"))
                                {
                                    id = id.Replace(":D", "");
                                    date_format = "MMMM dd, yyyy";
                                }
                                else if (id.EndsWith(":f")) //:f is the same as default, so ignore it.
                                {
                                    id = id.Replace(":f", "");
                                }
                                else if (id.EndsWith(":F"))
                                {
                                    id = id.Replace(":F", "");
                                    date_format = "dddd, MMMM dd, yyyy hh:mm tt";
                                }
                                else if (id.EndsWith(":R"))
                                {
                                    id = id.Replace(":R", "");
                                    relative = true;
                                }

                                if (long.TryParse(id, out long unixTimestamp))
                                {
                                    if (relative)
                                    {
                                        text = "< relative timestamp - soon! >";
                                    }
                                    else
                                    {
                                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
                                        DateTime localDateTime = dateTimeOffset.LocalDateTime;

                                        string formattedDate = localDateTime.ToString(date_format);

                                        text = formattedDate;
                                    }
                                }
                                else
                                {
                                    text = "Invalid Timestamp";
                                }
                                break;
                            }
                    }

                    if (link.Inlines.Count > 0 && type != null)
                    {
                        link.Click += (s, e) => OnHyperlinkClicked(type.Value, associatedObject);
                        textBlock.Inlines.Add(link);
                        continue;
                    }
                }
                List<Inline> inlines = new();

                Run currentRun = new Run();

                StringInfo info = new(text);

                for (int i = 0; i < info.LengthInTextElements; i++)
                {
                    string c = info.SubstringByTextElements(i, 1);
                    DiscordEmoji? emoji = null;
                    if (DiscordEmoji.IsValidUnicode(c))
                    {
                        try
                        {
                            emoji = DiscordEmoji.FromUnicode(c);
                        }
                        catch (Exception) { };
                    }

                    if (emoji == null)
                    {
                        // just add the character to the current run
                        currentRun.Text += c;
                        continue;
                    }

                    // emoji is not null; add the current run to the inlines list
                    inlines.Add(currentRun);
                    currentRun = new Run();
                    string? emojiName = emoji.SearchName.Replace(":", "") switch
                    {
                        "grinning" => "Smile.png",
                        "smiley" => "Smile.png",
                        "smile" => "Smile.png",
                        "grin" => "Grin.png",
                        "laughing" => "Grin.png",
                        "sweat_smile" => "Grin.png",
                        "joy" => "Grin.png",
                        "rofl" => "Grin.png",
                        "sob" => "Sob.png",
                        "pray" => "HighFive.png",
                        "thinking" => "Thinking.png",
                        "flushed" => "Flushed.png",
                        "sunglasses" => "Sunglasses.png",
                        "slight_frown" => "Discontent.png",
                        "confused" => "Discontent.png",
                        "frowning" => "Frown.png",
                        "frowning2" => "Frown.png",
                        "pensive" => "Frown.png",
                        "thumbsup" => "ThumbsUp.png",
                        "thumbsdown" => "ThumbsDown.png",
                        _ => null
                    };
                    if (emojiName is null)
                    {
                        inlines.Add(new Run(emoji.Name));
                    } else
                    {
                        InlineUIContainer inline = new();
                        Image image = new();
                        //image.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/Emoji/{emojiName}"));
                        // see if its in the cache
                        if (!EmojiCache.TryGetValue(emojiName, out BitmapSource? value))
                        {
                            value = new BitmapImage(new Uri($"pack://application:,,,/Resources/Emoji/{emojiName}"));
                            EmojiCache[emojiName] = value;
                        }
                        image.Source = value;
                        image.Width = 19;
                        image.Height = 19;
                        inline.Child = image;
                        inlines.Add(inline);
                    }
                }

                if (inlines.Count == 0) inlines.Add(currentRun);

                foreach (var inline in inlines)
                {
                    textBlock.Inlines.Add(inline);
                }
                // add a space
                textBlock.Inlines.Add(new Run(" "));
                textBlock.TextWrapping = TextWrapping.Wrap;
            }
            MainPanel.Children.Add(textBlock);
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
