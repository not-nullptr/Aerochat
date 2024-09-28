using Aerochat.BBCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Aerochat.Controls
{
    public class BBCode : TextBlock
    {
        public new string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly new DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(BBCode), new PropertyMetadata("", OnTextChanged));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BBCode bbcode)
            {
                bbcode.ParseText();
            }
        }

        private void ParseText()
        {
            // clear the textblock
            Inlines.Clear();
            var parsed = BBCodeParser.Parse(Text);
            Inlines.Add(parsed.ToXaml());
        }
    }
}
