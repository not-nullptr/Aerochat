using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace Aerochat.BBCode
{
    public enum BBCodeTokenType
    {
        Tag,
        Text,
        Root
    }
    public class BBCodeToken
    {
        public BBCodeTokenType Type;
        public string Content;
        public List<BBCodeToken> Children = new();
        public Dictionary<string, string> Attributes = new();
        public BBCodeToken? Parent;

        public BBCodeToken(BBCodeTokenType type)
        {
            Type = type;
            Content = "";
        }

        public BBCodeToken(BBCodeTokenType type, string content)
        {
            Type = type;
            Content = content;
        }

        public override string ToString()
        {
            var indentations = 0;
            // check how long the tree of parents goes for before it reaches the root
            BBCodeToken? parent = Parent;
            while (parent != null)
            {
                indentations++;
                parent = parent.Parent;
            }
            // create a string with the correct number of indentations
            string indentationString = "";
            for (int i = 0; i < indentations; i++)
            {
                indentationString += "  ";
            }
            // create a string representation of the token
            string result = $"{indentationString}{Type} \"{Content}\"{(Attributes.Count > 0 ? $", {string.Join(", ", Attributes.Select(x => $"{x.Key}={x.Value}"))}" : "")}\n";
            foreach (var child in Children)
            {
                result += child.ToString();
            }
            return result;
        }

        public Span ToXaml()
        {
            var span = new Span();

            Span inline = Content switch
            {
                "" => new Span(),
                "link" => new Hyperlink(),
                "b" => new Bold(),
                "i" => new Italic(),
                "u" => new Underline(),
                "s" => new Span(),
                "color" => new Span(),
                _ when Type == BBCodeTokenType.Text => new Span(),
                _ when Type == BBCodeTokenType.Tag => new Span(),
                _ => new Span()
            };

            switch (Content)
            {
                case "link":
                    ((Hyperlink)inline).NavigateUri = new Uri(Attributes["url"]);
                    ((Hyperlink)inline).Click += (s, e) =>
                    {
                        Process.Start(new ProcessStartInfo(((Hyperlink)s).NavigateUri.AbsoluteUri) { UseShellExecute = true });
                    };
                    break;
                case "color":
                    inline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Attributes["color"]));
                    break;
            }

            if (Type == BBCodeTokenType.Text)
            {
                inline.Inlines.Add(new Run(Content));
            }
            else
            {
                // recursively add children
                foreach (var child in Children)
                {
                    inline.Inlines.Add(child.ToXaml());
                }
            }

            span.Inlines.Add(inline);
            return span;
        }
    }
}
