using Aerochat.Controls;
using Aerochat.ViewModels;
using Aerochat.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DSharpPlus.Entities;
using Aerochat.Hoarder;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using static Vanara.PInvoke.DwmApi;
using Vanara.PInvoke;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using static Vanara.PInvoke.Shell32;

namespace Aerochat.Pages.Wizard
{
    public partial class WizardCategoryCreationPage : Page
    {
        private CategoryWizard _window = null!;
        public bool IsServer { get; }
        public int CloseButtonSize { get; set; }
        public ObservableCollection<GuildViewModel> Guilds { get; } = new();
        public ObservableCollection<ChannelViewModel> Channels { get; } = new();
        public ObservableCollection<GuildViewModel> FilteredGuilds = new();
        public ObservableCollection<ChannelViewModel> FilteredChannels = new();
        public WizardCategoryCreationPage(bool isServer)
        {
            InitializeComponent();
            Loaded += WizardPage_Loaded;
            IsServer = isServer;
            if (IsServer)
            {
                CodeBox.Text = @"guild.Name == ""Aerochat""";
                PreviewShownItems.ItemsSource = FilteredGuilds;
            } else
            {
                CodeBox.Text = @"channel.Recipients.Select(x => x.Username).Contains(""notnullptr"")";
                PreviewShownItems.ItemsSource = FilteredChannels;
            }

            foreach (var guild in Discord.Client.Guilds.Values)
            {
                Guilds.Add(GuildViewModel.FromGuild(guild));
            }

            foreach (var channel in Discord.Client.PrivateChannels.Values)
            {
                Channels.Add(ChannelViewModel.FromChannel(channel));
            }
        }

        private void WizardPage_Loaded(object sender, RoutedEventArgs e)
        {
            _window = (CategoryWizard)Window.GetWindow(this);
            var hWnd = new WindowInteropHelper(_window).Handle;
            var ptr = Marshal.AllocHGlobal(sizeof(uint));
            DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_CAPTION_BUTTON_BOUNDS, ptr, sizeof(uint));
            var size = Marshal.ReadInt32(ptr);
            Marshal.FreeHGlobal(ptr);
            CloseButtonSize = size;
            Debug.WriteLine(size);
            DataContext = this;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _window.Close();
        }
        private void CommandLink_Loaded(object sender, RoutedEventArgs e)
        {
            var commandLink = (CommandLink)sender;
        }

        private void CommandLink_Click(object sender, EventArgs e)
        {
            var commandLink = (CommandLink)sender;
            if (commandLink is null) return;
            switch (commandLink.Tag)
            {
                case "Server":
                    _window.NavigateTo(new WizardCategoryCreationPage(true));
                    break;
                case "DMs":
                    _window.NavigateTo(new WizardCategoryCreationPage(false));
                    break;
            }
        }

        private void Evaluate()
        {
            var code = CodeBox.Text;
            try
            {
                if (!IsServer)
                {
                    var target = Discord.Client.PrivateChannels.ElementAt(0).Value;
                    var config = new ParsingConfig
                    {
                        CustomTypeProvider = new ProvideAllTypesProvider()
                    };
                    ParameterExpression channelExpression = Expression.Parameter(typeof(DiscordDmChannel), "channel");

                    // Pass the config as an additional parameter
                    LambdaExpression lambda = DynamicExpressionParser.ParseLambda(config, [channelExpression], typeof(bool), code);

                    var compiled = lambda.Compile();
                    bool result = (bool)compiled.DynamicInvoke(target);

                    FilteredChannels.Clear();
                    foreach (var channel in Discord.Client.PrivateChannels)
                    {
                        bool isVisible = (bool)compiled.DynamicInvoke(channel.Value);
                        if (isVisible)
                        {
                            FilteredChannels.Add(ChannelViewModel.FromChannel(channel.Value));
                        }
                    }
                } else
                {
                    var target = Discord.Client.Guilds.ElementAt(0).Value;
                    var config = new ParsingConfig
                    {
                        CustomTypeProvider = new ProvideAllTypesProvider()
                    };
                    ParameterExpression guildExpression = Expression.Parameter(typeof(DiscordGuild), "guild");

                    // Pass the config as an additional parameter
                    LambdaExpression lambda = DynamicExpressionParser.ParseLambda(config, [guildExpression], typeof(bool), code);

                    var compiled = lambda.Compile();
                    bool result = (bool)compiled.DynamicInvoke(target);
                    if (result)
                    {
                        Feedback.Foreground = Brushes.DarkGreen;
                        Feedback.Text = "True";
                    }
                    else
                    {
                        Feedback.Foreground = Brushes.DarkRed;
                        Feedback.Text = "False";
                    }

                    FilteredGuilds.Clear();
                    foreach (var guild in Discord.Client.Guilds)
                    {
                        bool isVisible = (bool)compiled.DynamicInvoke(guild.Value);
                        if (isVisible)
                        {
                            FilteredGuilds.Add(GuildViewModel.FromGuild(guild.Value));
                        }
                    }
                }

                Feedback.Text = "";
            }
            catch (Exception ex)
            {
                Feedback.Foreground = Brushes.DarkRed;
                Feedback.Text = ex.Message;
            }

            Continue.IsEnabled = Feedback.Text == "";
        }

        private async void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // try to validate the code. we need to add usings and namespace
            Evaluate();
        }

        private void PreviewItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Evaluate();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            _window.NavigateTo(new WizardChooseNamePage(IsServer, CodeBox.Text));
        }
    }

    public class ProvideAllTypesProvider : DefaultDynamicLinqCustomTypeProvider
    {
        public override HashSet<Type> GetCustomTypes()
        {
            var types = base.GetCustomTypes();
            // add all types present inside of an instance of DiscordChannel
            foreach (var prop in typeof(DiscordDmChannel).GetProperties())
            {
                types.Add(prop.PropertyType);
            }
            foreach (var method in typeof(DiscordDmChannel).GetMethods())
            {
                types.Add(method.ReturnType);
            }
            foreach (var field in typeof(DiscordDmChannel).GetFields())
            {
                types.Add(field.FieldType);
            }
            // add all types in guild, too
            foreach (var prop in typeof(DiscordGuild).GetProperties())
            {
                types.Add(prop.PropertyType);
            }
            foreach (var method in typeof(DiscordGuild).GetMethods())
            {
                types.Add(method.ReturnType);
            }
            foreach (var field in typeof(DiscordGuild).GetFields())
            {
                types.Add(field.FieldType);
            }
            return types;
        }
    }
}
