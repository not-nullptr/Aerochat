using DSharpPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.BBCode
{
    public static class BBCodeParser
    {
        public static BBCodeToken? Parse(string? text)
        {
            if (text == null)
                return null;

            int index = 0;
            BBCodeToken root = new(BBCodeTokenType.Root);
            BBCodeToken current = root;
            while (index < text.Length)
            {
                if (text[index] == '[')
                {
                    if (text[index + 1] == '/')
                    {
                        index += 2;
                        string tag = "";
                        while (text[index] != ']')
                        {
                            tag += text[index];
                            index++;
                        }
                        index++;
                        var tagParts = tag.Split(" ");
                        if (tagParts[0] == current.Content)
                        {
                            current = current.Parent;
                        }
                        else
                        {
                            throw new Exception("Mismatched tags");
                        }
                    }
                    else
                    {
                        index++;
                        string tag = "";
                        try
                        {
                            while (text[index] != ']')
                            {
                                tag += text[index];
                                index++;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Unclosed tag");
                        }
                        index++;
                        var tagParts = tag.Split(" ");
                        BBCodeToken token = new(BBCodeTokenType.Tag, tagParts[0]);
                        foreach (var paramDef in tagParts.Skip(1))
                        {
                            var equalsIndex = paramDef.IndexOf("=");
                            if (equalsIndex == -1) throw new InvalidDataException("Missing equals in dictionary");
                            var key = new string(paramDef.Take(equalsIndex).ToArray());
                            var value = new string(paramDef.Skip(equalsIndex + 1).ToArray());
                            token.Attributes.TryAdd(key, value);
                        }
                        current.Children.Add(token);
                        token.Parent = current;
                        current = token;
                    }
                }
                else
                {
                    string content = "";
                    while (index < text.Length && text[index] != '[')
                    {
                        content += text[index];
                        index++;
                    }
                    BBCodeToken token = new(BBCodeTokenType.Text, content);
                    token.Parent = current;
                    current.Children.Add(token);
                }
            }
            return root;
        }
    }

}
