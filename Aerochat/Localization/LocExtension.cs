using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace Aerochat.Localization
{
    /// <summary>
    /// WPF markup extension for inline translations in XAML.
    /// Usage:  Text="{loc:Loc SignIn}"
    /// The extension creates a live binding to <see cref="LocalizationManager.Instance"/>
    /// so the UI updates automatically when the user switches languages.
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class LocExtension : MarkupExtension
    {
        /// <summary>The translation key, e.g. "SignIn", "DialogTitle", etc.</summary>
        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationManager.Instance,
                Mode = BindingMode.OneWay,
            };

            return binding.ProvideValue(serviceProvider);
        }
    }
}
