using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// (iL - 21.12.2024) This is basically another abstraction layer for developers.
// Let's say you have a beautiful DropDown for the Settings, but your Enum Variable Names are pretty cryptic
// for the average End-User. With Aerochat.Attributes, you can show the user a beautiful string in the UI,
// while here in the Code you can keep your cryptic and scary sounding Names.

namespace Aerochat.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class DisplayAttribute : Attribute
    {
        public string Name { get; set;  }

        public DisplayAttribute(string name)
        {
            Name = name;
        }
    }
}
