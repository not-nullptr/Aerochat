// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2023 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.Net;
using DSharpPlus.Net.Abstractions;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    /// <summary>
    /// Represents a Discord user.
    /// </summary>
    public class DiscordProfile : SnowflakeObject, IEquatable<DiscordProfile>
    {
        internal DiscordProfile() { }
        internal DiscordProfile(TransportProfile transport)
        {
            this.User = new DiscordUser(transport.User);
        }

        public virtual DiscordUser User { get; internal set; }

        /// <summary>
        /// Checks whether this <see cref="DiscordProfile"/> is equal to another object.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        /// <returns>Whether the object is equal to this <see cref="DiscordProfile"/>.</returns>
        public override bool Equals(object obj) => this.Equals(obj as DiscordProfile);

        /// <summary>
        /// Checks whether this <see cref="DiscordProfile"/> is equal to another <see cref="DiscordProfile"/>.
        /// </summary>
        /// <param name="e"><see cref="DiscordProfile"/> to compare to.</param>
        /// <returns>Whether the <see cref="DiscordProfile"/> is equal to this <see cref="DiscordProfile"/>.</returns>
        public bool Equals(DiscordProfile e)
        {
            if (e is null)
                return false;

            return ReferenceEquals(this, e) ? true : this.Id == e.Id;
        }

        /// <summary>
        /// Gets the hash code for this <see cref="DiscordProfile"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="DiscordProfile"/>.</returns>
        public override int GetHashCode() => this.Id.GetHashCode();

        /// <summary>
        /// Gets whether the two <see cref="DiscordProfile"/> objects are equal.
        /// </summary>
        /// <param name="e1">First user to compare.</param>
        /// <param name="e2">Second user to compare.</param>
        /// <returns>Whether the two users are equal.</returns>
        public static bool operator ==(DiscordProfile e1, DiscordProfile e2)
        {
            var o1 = e1 as object;
            var o2 = e2 as object;

            if ((o1 == null && o2 != null) || (o1 != null && o2 == null))
                return false;

            return o1 == null && o2 == null ? true : e1.Id == e2.Id;
        }

        /// <summary>
        /// Gets whether the two <see cref="DiscordProfile"/> objects are not equal.
        /// </summary>
        /// <param name="e1">First user to compare.</param>
        /// <param name="e2">Second user to compare.</param>
        /// <returns>Whether the two users are not equal.</returns>
        public static bool operator !=(DiscordProfile e1, DiscordProfile e2)
            => !(e1 == e2);
    }

    internal class DiscordProfileComparer : IEqualityComparer<DiscordProfile>
    {
        public bool Equals(DiscordProfile x, DiscordProfile y) => x.Equals(y);

        public int GetHashCode(DiscordProfile obj) => obj.Id.GetHashCode();
    }
}
