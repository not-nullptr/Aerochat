using Aerochat.Hoarder;
using DiscordProtos.DiscordUsers.V1;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Aerochat.Helpers
{
    public class DiscordUserSettingsManager
    {
        private static DiscordUserSettingsManager _instance = new();

        public static DiscordUserSettingsManager Instance
        {
            get => _instance;
            private set { }
        }

        private DiscordUserSettingsManager()
        {
        }

        public void Startup()
        {
            Discord.Client.UserSettingsProtoUpdated += OnUserSettingsProtoUpdate;
        }

        public void LoadInitialSettingsFromDiscordClient()
        {
            if (Discord.Client.UserSettingsProto.Length > 0)
            {
                byte[] protoBytes = Convert.FromBase64String(Discord.Client.UserSettingsProto);

                if (protoBytes.Length > 0)
                {
                    _userSettingsProto = PreloadedUserSettings.Parser.ParseFrom(protoBytes);
                }
            }
        }

        private PreloadedUserSettings? _userSettingsProto = null;

        public PreloadedUserSettings? UserSettingsProto
        {
            get => _userSettingsProto;
            private set => _userSettingsProto = value;
        }

        public event EventHandler<DiscordUserSettingsUpdateEventArgs> UserSettingsUpdated;

        public async Task UpdateRemote()
        {
            byte[] protoBytes = UserSettingsProto.ToByteArray();
            string base64Proto = Convert.ToBase64String(protoBytes);
            await Discord.Client.UpdateUserSettingsProto(base64Proto);
        }

        private async Task OnUserSettingsProtoUpdate(object sender, UserSettingsProtoUpdateEventArgs e)
        {
            byte[] protoBytes = Convert.FromBase64String(e.Base64EncodedProto);

            if (protoBytes.Length > 0)
            {
                _userSettingsProto = PreloadedUserSettings.Parser.ParseFrom(protoBytes);

                // Set the status from the protobuf settings.
                if (_userSettingsProto.Status.Status != null)
                {
                    UserStatus status = _userSettingsProto.Status.Status.ToUserStatus();
                    await Application.Current.Dispatcher.BeginInvoke(() => App.SetStatus(status, false));
                }               

                UserSettingsUpdated?.Invoke(this, new(_userSettingsProto));
            }
        }
    }

    public class DiscordUserSettingsUpdateEventArgs : EventArgs
    {
        public PreloadedUserSettings NewSettings { get; private set; }

        public DiscordUserSettingsUpdateEventArgs(PreloadedUserSettings newSettings)
            : base()
        {
            NewSettings = newSettings;
        }
    }
}
