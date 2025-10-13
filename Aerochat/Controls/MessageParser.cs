using Aerochat.Hoarder;
using DSharpPlus.Entities;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vanara.Extensions.Reflection;

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
        public event EventHandler<ContextMenuEventArgs> TextBlockContextMenuOpening;

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

#if FEATURE_SELECTABLE_MESSAGE_TEXT
            var textBlock = new SelectableTextBlock();
#else
            var textBlock = new TextBlock();
#endif

            //// Prevent the usual context menu from showing up:
            //textBlock.ContextMenu = null;

            //Throwing this in bcos i can :3 (messy nullcheck sawry :()
            if (Message.MentionedUsers != null)
            {
                if (Message.MentionedUsers.Contains(Discord.Client.CurrentUser) && Settings.SettingsManager.Instance.HighlightMentions)
                {
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(73, 164, 218));
                }
            }

            var words = Message.Content.Split(' ');
            foreach (var word in words)
            {
                string text = word;
                if (word.StartsWith("<") && word.EndsWith(">"))
                {
                    string id = word.Replace("<", "").Replace(">", "");
                    var link = new Hyperlink();
                    HyperlinkType? type = null;
                    object associatedObject = null;

                    // if there's no element at 0, continue
                    if (id.Length != 0)
                    {
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
                            case ':':
                                {
                                    string[] emojiParts = id.Split(":");

                                    if (emojiParts.Length != 3)
                                    {
                                        break;
                                    }

                                    string emojiName = emojiParts[1];
                                    string emojiIdStr = emojiParts[2];

                                    if (!ulong.TryParse(emojiIdStr, out ulong emojiId))
                                    {
                                        break;
                                    }

                                    var emojiDefinition = Message?.Channel?.Guild?.Emojis?.FirstOrDefault(x => x.Key == emojiId);

                                    if (emojiDefinition?.Value?.Url == null)
                                    {
                                        break;
                                    }

                                    InlineUIContainer inlineContainer = new();
                                    Image emojiImage = new();
                                    emojiImage.Source = new BitmapImage(new Uri(emojiDefinition.Value.Value.Url));
                                    emojiImage.Width = 19;
                                    emojiImage.Height = 19;
                                    emojiImage.VerticalAlignment = VerticalAlignment.Center;
                                    emojiImage.ToolTip = $":{emojiName}:";
                                    inlineContainer.Child = emojiImage;

                                    textBlock.Inlines.Add(inlineContainer);
                                    textBlock.Inlines.Add(new Run(" "));
                                    textBlock.TextWrapping = TextWrapping.Wrap;

                                    // Make the below loop continue:
                                    type = HyperlinkType.ServerEmoji;

                                    break;
                                }
                        }

                        if (link.Inlines.Count > 0 && type != null)
                        {
                            link.Click += (s, e) => OnHyperlinkClicked(type.Value, associatedObject);
                            textBlock.Inlines.Add(link);
                            continue;
                        }
                        else if (type == HyperlinkType.ServerEmoji)
                        {
                            continue;
                        }
                    }
                }
                else if (word.StartsWith("http://") || word.StartsWith("https://"))
                {
                    // This is a link. Links cannot contain spaces, so we can easily just consider the
                    // whole part a link (in the case of standard links). Of course, we try to parse an
                    // actual URI here, and if we cannot deduce one, then we disregard the part.
                    if (Uri.IsWellFormedUriString(word, UriKind.Absolute))
                    {
                        Hyperlink link = new();
                        Uri uriSanitised = new(word);

                        link.Click += (s, e) => OnHyperlinkClicked(HyperlinkType.WebLink, uriSanitised.ToString());
                        link.Inlines.Add(uriSanitised.ToString());
                        textBlock.Inlines.Add(link);
                        textBlock.Inlines.Add(" ");
                        continue;
                    }
                }

                List<Inline> inlines = new();
                Run currentRun = new Run();

                if (ContainsEmoji(text)) // stops the iteration if the text can't possibly contain emoji
                {
                    DiscordEmoji? emoji = null;
                    StringInfo info = new(text);
                    int loopCount = info.LengthInTextElements;
                    for (int i = 0; i < loopCount; i++)
                    {
                        string c = info.SubstringByTextElements(i, 1);

                        if (text.StartsWith(":") && text.EndsWith(":"))
                        {
                            try
                            {
                                emoji = DiscordEmoji.FromName(Discord.Client, text);
                                loopCount = 1;
                            }
                            catch { }
                        }

                        else
                        {
                            DiscordEmoji.TryFromUnicode(c, out emoji);
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
                        if (!EmojiDictionary.Map.TryGetValue(emoji.SearchName.Replace(":", ""), out var emojiName))
                            emojiName = null; // fallback

                        if (emojiName is null)
                        {
                            inlines.Add(new Run(emoji.Name));
                        }
                        else
                        {
                            InlineUIContainer inline = new();
                            Image image = new();
                            //image.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/Emoji/{emojiName}"));
                            // see if its in the cache
                            if (!EmojiCache.TryGetValue(emojiName, out BitmapSource? value))
                            {
                                value = new BitmapImage(new Uri($"pack://application:,,,/Resources/Emoji/{emojiName}"));
                                value.Freeze();
                                EmojiCache[emojiName] = value;
                            }
                            image.Source = value;
                            image.Width = 19;
                            image.Height = 19;
                            inline.Child = image;
                            inlines.Add(inline);
                        }
                    }
                }

                else
                {
                    currentRun.Text = text;
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

        private bool ContainsEmoji(string text)
        {
            if (text.Contains(':') && text.IndexOf(':') != text.LastIndexOf(':'))
                return true;

            foreach (char c in text)
            {
                if (char.IsSurrogate(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherSymbol)
                    return true;
            }

            return false;
        }

        private void TextBlock_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            TextBlockContextMenuOpening?.Invoke(this, e);
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
            Loaded += (_, _) => Window.GetWindow(this).Closing += (s, e) =>
            {

            };
        }
    }

    public enum HyperlinkType
    {
        Channel,
        Role,
        User,
        WebLink,
        ServerEmoji, // Internal parsing purposes only.
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
