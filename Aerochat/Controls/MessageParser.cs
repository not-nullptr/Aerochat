using Aerochat.Hoarder;
using DSharpPlus.Entities;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
                DiscordEmoji? emoji = null;

                if (text.StartsWith(":") && text.EndsWith(":"))
                {
                    try
                    {
                        emoji = DiscordEmoji.FromName(Discord.Client, text);
                    }
                    catch { }
                }

                else if (DiscordEmoji.IsValidUnicode(text))
                {
                    try
                    {
                        emoji = DiscordEmoji.FromUnicode(text);
                    }
                    catch { }
                }

                if (emoji != null)
                {
                    // emoji is not null; add the current run to the inlines list
                    inlines.Add(currentRun);
                    currentRun = new Run();
                    string? emojiName = emoji.SearchName.Replace(":", "") switch
                    {
                        "grinning" or "smiley" or "smile" or "slight_smile" => "Smile.png",
                        "grin" or "laughing" or "sweat_smile" or "joy" or "rofl" => "Grin.png",
                        "sob" => "Sob.png",
                        "pray" => "HighFive.png",
                        "thinking" => "Thinking.png",
                        "flushed" => "Flushed.png",
                        "sunglasses" => "Sunglasses.png",
                        "slight_frown" => "Discontent.png",
                        "frowning" or "frowning2" or "pensive" => "Frown.png",
                        "thumbsup" => "ThumbsUp.png",
                        "thumbsdown" => "ThumbsDown.png",
                        "nerd" => "Nerd.png",
                        "partying_face" => "Party.png",
                        "airplane" => "Plane.png",
                        "rainbow" => "Rainbow.png",
                        "pizza" => "Pizza.png",
                        "rage" => "Rage.png",
                        "rose" => "Rose.png",
                        "angel" or "innocent" => "Angel.png",
                        "angry" => "Anger.png",
                        "bat" => "Bat.png",
                        "beach" or "island" => "Beach.png",
                        "beer" or "beers" => "Beer.png",
                        "broken_heart" => "BrokenHeart.png",
                        "cake" or "birthday_cake" or "moon_cake" => "Cake.png",
                        "camera" or "camera_with_flash" or "movie_camera" or "video_camera" => "Camera.png",
                        "red_car" or "blue_car" or "race_car" => "Car.png",
                        "black_cat" or "cat" or "cat2" => "Cat.png",
                        "mobile_phone" or "calling" => "CellPhone.png",
                        "smoking" => "Cigarette.png",
                        "clock" or "alarm_clock" or "timer_clock" => "Clock.png",
                        "coffee" => "Coffee.png",
                        "computer" or "desktop_computer" => "Computer.png",
                        "confused" => "Confused.png",
                        "people_holding_hands" or "two_men_holding_hands" or "two_women_holding_hands" => "Conversation.png", // Discord hasn't added conversation, this is a good substitute
                        "fingers_crossed" => "CrossedFingers.png",
                        "handcuffs" or "cuffs" => "Cuffs.png", // not on discord
                        "coin" or "moneybag" or "dollar" or "euro" or "pound" or "heavy_dollar_sign" or "yen" => "Currency.png",
                        "imp" or "smiling_imp" => "Demon.png",
                        "dog" or "guide_dog" or "service_dog" or "dog2" or "poodle" => "Dog.png",
                        "film_frames" or "projector" => "Film.png",
                        "soccer" or "soccer_ball" or "actual_football" => "SoccerBall.png",
                        "goat" => "Goat.png",
                        "heart" or "hearts" or "heart_decoration" or "black_heart" or "green_heart" or "blue_heart" or "brown_heart" or "grey_heart" or "light_blue_heart" or "orange_heart" or "pink_heart" or "purple_heart" or "yellow_heart" or "white_heart" => "Heart.png",
                        "pray" or "folded_hands" => "HighFive.png",
                        "jump" => "Jump.png", // not on discord
                        "bulb" or "light_bulb" => "LightBulb.png",
                        "biting_lip" => "LipBite.png",
                        "mailbox_with_mail" or "envelope" or "postbox" or "incoming_envelope" or "e_mail" or "email" or "envelope_with_arrow" => "Mail.png",
                        "man" or "man_beard" => "Man.png",
                        "crescent_moon" or "full_moon" or "full_moon_with_face" => "Moon.png",
                        "musical_note" or "musical_notes" => "Music.png",
                        "telephone" or "telephone_reciever" => "Phone.png",
                        "fork_knife_plate" or "fork_and_knife_with_plate" => "Plate.png",
                        "gift" or "wrapped_gift" => "Present.png",
                        "rabbit" or "rabbit2" => "Rabbit.png",
                        "cloud_rain" => "Rain.png",
                        "reach_left" => "ReachLeft.png", // not on discord
                        "reach_right" => "ReachRight.png", // not on discord
                        "rolling_eyes" => "RollingEyes.png",
                        "wilted_rose" => "RoseWilter.png",
                        "sheep" or "ewe" or "ram" => "Sheep.png",
                        "nauseated_face" or "sick" or "face_vomiting" => "Sick.png",
                        "snail" => "Snail.png",
                        "bowl_with_spoon" or "tea" => "Soup.png",
                        "hushed_face" or "hushed" => "Surprise.png",
                        "astonished" => "Surprised.png",
                        "thunder_cloud_rain" => "Thunder.png",
                        "stuck_out_tongue_closed_eyes" or "stuck_out_tongue" or "stuck_out_tongue_winking_eye" or "tongue" => "Tongue.png",
                        "turtle" => "Tortoise.png",
                        "closed_umbrella" or "umbrella" or "umbrella2" => "Umbrella.png",
                        "wine_glass" => "Wine.png",
                        "wink" => "Wink.png",
                        "wlm" => "WLM.png",
                        "woman" or "woman_beard" => "Woman.png",
                        "video_game" or "xbox" => "Xbox.png",
                        "yawning_face" => "Yawn.png",
                        "zipper_mouth" => "ZipMouth.png",
                        _ => null
                    };

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

                    if (inlines.Count == 0) inlines.Add(currentRun);
                    foreach (var inline in inlines)
                    {
                        textBlock.Inlines.Add(inline);
                    }
                }

                else
                {
                    textBlock.Inlines.Add(new Run(text));
                }

                // add a space
                textBlock.Inlines.Add(new Run(" "));
                textBlock.TextWrapping = TextWrapping.Wrap;
            }
            MainPanel.Children.Add(textBlock);
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
