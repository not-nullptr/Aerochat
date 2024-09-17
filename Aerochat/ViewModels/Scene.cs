using Aerochat.Hoarder;
using Aerochat.ViewModels;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Aerochat.Theme
{
    public class SceneViewModel : ViewModelBase
    {
        // scene will be a scene element, containing an id, file, default, displayname and color attribute
        private int _id;
        private string _file;
        private bool _default;
        private string _displayName;
        private string _color;
        private string _textColor;
        private string _shadowColor;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string File
        {
            get => _file;
            set => SetProperty(ref _file, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public bool Default
        {
            get => _default;
            set => SetProperty(ref _default, value);
        }

        public string TextColor
        {
            get => _textColor;
            set => SetProperty(ref _textColor, value);
        }

        public string ShadowColor
        {
            get => _shadowColor;
            set => SetProperty(ref _shadowColor, value);
        }

        public static SceneViewModel FromScene(XElement scene)
        {
            string colourStr = scene.Attribute("color").Value;
            byte R = byte.Parse(colourStr.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            byte G = byte.Parse(colourStr.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            byte B = byte.Parse(colourStr.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            double threshold = 186 / 255.0;
            bool textBlack = R * 0.299 + G * 0.587 + B * 0.114 > threshold * 255;
            return new SceneViewModel
            {
                Id = int.Parse(scene.Attribute("id").Value),
                File = $"/Scenes/{scene.Attribute("file").Value}",
                DisplayName = scene.Attribute("displayname").Value,
                Color = colourStr,
                Default = bool.Parse(scene.Attribute("default").Value),
                TextColor = textBlack ? "#000000" : "#ffffff",
                ShadowColor = textBlack ? "#ffffff" : "#000000"
            };
        }

        public static SceneViewModel FromUser(DiscordUser user)
        {
            DiscordColor? colour = user.BannerColor;
            if (colour != null)
            {
                string hex = $"#{colour.Value.R:X2}{colour.Value.G:X2}{colour.Value.B:X2}";
                // try and find a scene in ThemeService.Instance.Scenes where the color matches the banner color
                SceneViewModel? scene = ThemeService.Instance.Scenes.FirstOrDefault(s => s.Color.ToLower() == hex.ToLower()) ?? ThemeService.Instance.Scenes.FirstOrDefault(s => s.Default);
                if (scene?.Default == true)
                {
                    bool textBlack = colour.Value.R * 0.299 + colour.Value.G * 0.587 + colour.Value.B * 0.114 > 186;
                    // clone the scene, set the color to the banner color and return it
                    SceneViewModel newScene = new SceneViewModel
                    {
                        Id = scene.Id,
                        File = scene.File,
                        DisplayName = scene.DisplayName,
                        Color = hex,
                        Default = true,
                        TextColor = textBlack ? "#000000" : "#ffffff",
                        ShadowColor = textBlack ? "#ffffff" : "#000000"
                    };
                    return newScene;
                }
                return scene;
            }
            else return ThemeService.Instance.Scenes.FirstOrDefault(s => s.Default)!;
        }
    }
}
