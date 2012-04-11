/* Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
 * 
 * Based, in part, on SmartIrc4net code. Original license is reproduced below.
 * 
 *
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

namespace fCraft {
    // ReSharper disable FieldCanBeMadeReadOnly.Global
    public sealed class IRCMessage {
        public string From { get; private set; }
        public string Nick { get; private set; }
        public string Ident { get; private set; }
        public string Host { get; private set; }
        public string Channel { get; private set; }
        public string Message { get; private set; }
        public string[] MessageArray { get; private set; }
        public string RawMessage { get; private set; }
        public string[] RawMessageArray { get; private set; }
        public IRCMessageType Type { get; private set; }
        public IRCReplyCode ReplyCode { get; private set; }

        public IRCMessage( string from, string nick, string ident, string host, string channel, string message, string rawMessage, IRCMessageType type, IRCReplyCode replycode ) {
            RawMessage = rawMessage;
            RawMessageArray = rawMessage.Split( new[] { ' ' } );
            Type = type;
            ReplyCode = replycode;
            From = from;
            Nick = nick;
            Ident = ident;
            Host = host;
            Channel = channel;
            if( message != null ) {
                // message is optional
                Message = message;
                MessageArray = message.Split( new[] { ' ' } );
            }
        }
    }
}
