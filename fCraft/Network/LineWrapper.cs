// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
//#define DEBUG_LINE_WRAPPER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> Intelligent line-wrapper for Minecraft protocol.
    /// Splits long messages into 64-character chunks of ASCII.
    /// Maintains colors between lines. Wraps at word boundaries and dashes.
    /// Removes invalid characters and color sequences.
    /// Supports optional line prefixes for second and consequent lines.
    /// This class is implemented as IEnumerable of Packets, so it's usable with foreach() and Linq. </summary>
    sealed class LineWrapper : IEnumerable<Packet>, IEnumerator<Packet>
    {
        const string DefaultPrefixString = "> ";
        static readonly byte[] DefaultPrefix;

        static LineWrapper()
        {
            DefaultPrefix = Encoding.ASCII.GetBytes(DefaultPrefixString);
        }

        const int LineSize = 64;
        const int MaxPrefixSize = 32;
        const int PacketSize = 66; // opcode + id + 64
        const byte NoColor = (byte)'f';

        public Packet Current { get; private set; }

        byte color, lastColor;
        bool hadColor;
        int spaceCount, wordLength;

        readonly byte[] input;
        int inputIndex;

        byte[] output;
        int outputStart, outputIndex;

        readonly byte[] prefix;


        LineWrapper([NotNull] string message)
        {
            if (message == null) throw new ArgumentNullException("message");
            input = Encoding.ASCII.GetBytes(message);
            prefix = DefaultPrefix;
            Reset();
        }


        LineWrapper([NotNull] string prefixString, [NotNull] string message)
        {
            if (prefixString == null) throw new ArgumentNullException("prefixString");
            prefix = Encoding.ASCII.GetBytes(prefixString);
            if (prefix.Length > MaxPrefixSize) throw new ArgumentException("Prefix too long", "prefixString");
            if (message == null) throw new ArgumentNullException("message");
            input = Encoding.ASCII.GetBytes(message);
            Reset();
        }


        public void Reset()
        {
            color = NoColor;
            wordLength = 0;
            inputIndex = 0;
            wrapIndex = 0;
            wrapOutputIndex = 0;
        }


        public bool MoveNext()
        {
            if (inputIndex >= input.Length)
            {
                return false;
            }
            hadColor = false;

            output = new byte[PacketSize];
            output[0] = (byte)OpCode.Message;

            outputStart = 2;
            outputIndex = outputStart;
            spaceCount = 0;
            lastColor = NoColor;
            Current = new Packet(output);

            wrapColor = NoColor;
            expectingColor = false;
            wrapOutputIndex = outputStart;

            // Prepend line prefix, if needed
            if (inputIndex > 0 && prefix.Length > 0)
            {
                int preBufferInputIndex = inputIndex;
                byte preBufferColor = color;
                color = NoColor;
                inputIndex = 0;
                wrapIndex = 0;
                wordLength = 0;
                while (inputIndex < prefix.Length)
                {
                    byte ch = prefix[inputIndex];
                    if (ProcessChar(ch))
                    {
                        // Should never happen, since prefix is under 32 chars
                        throw new Exception("Prefix required wrapping.");
                    }
                    inputIndex++;
                }
                inputIndex = preBufferInputIndex;
                color = preBufferColor;
                wrapColor = preBufferColor;
            }

            wordLength = 0;
            wrapIndex = inputIndex;

            // Append as much of the remaining input as possible
            while (inputIndex < input.Length)
            {
                byte ch = input[inputIndex];
                if (ProcessChar(ch))
                {
                    // Line wrap is needed
                    PrepareOutput();
                    return true;
                }
                inputIndex++;
            }
            PrepareOutput();
            return true;
        }

        int wrapIndex,
            wrapOutputIndex;
        byte wrapColor;
        bool expectingColor;

        bool ProcessChar(byte ch)
        {
            switch (ch)
            {
                case (byte)' ':
                    expectingColor = false;
                    if (spaceCount == 0)
                    {
                        // first space after a word, set wrapping point
                        wrapIndex = inputIndex;
                        wrapOutputIndex = outputIndex;
                        wrapColor = color;
                    }
                    spaceCount++;
                    break;

                case (byte)'&':
                    if (expectingColor)
                    {
                        // append "&&"
                        expectingColor = false;
                        if (!Append(ch))
                        {
                            if (wordLength < LineSize - 2 - prefix.Length)
                            {
                                inputIndex = wrapIndex;
                                outputIndex = wrapOutputIndex;
                                color = wrapColor;
                            }
                            else
                            {
                                inputIndex--;
                                // else word is too long, dont backtrack to wrap
                            }
                            return true;
                        }
                        spaceCount = 0;
                    }
                    else
                    {
                        expectingColor = true;
                    }
                    break;

                case (byte)'-':
                    expectingColor = false;
                    if (!Append(ch))
                    {
                        if (wordLength < LineSize - prefix.Length)
                        {
                            inputIndex = wrapIndex;
                            outputIndex = wrapOutputIndex;
                            color = wrapColor;
                        }
                        return true;
                    }
                    spaceCount = 0;
                    // allow wrapping after dash
                    wrapIndex = inputIndex + 1;
                    wrapOutputIndex = outputIndex;
                    wrapColor = color;
                    break;

                case (byte)'\n':
                    inputIndex++;
                    return true;

                default:
                    if (expectingColor)
                    {
                        expectingColor = false;
                        if (ProcessColor(ref ch))
                        {
                            color = ch;
                            hadColor = true;
                        }// else colorcode is invalid, skip
                    }
                    else
                    {
                        if (spaceCount > 0)
                        {
                            wrapIndex = inputIndex;
                            wrapColor = color;
                        }
                        if (!IsWordChar(ch))
                        {
                            // replace unprintable chars with '?'
                            ch = (byte)'?';
                        }
                        if (!Append(ch))
                        {
                            if (wordLength < LineSize - prefix.Length)
                            {
                                inputIndex = wrapIndex;
                                outputIndex = wrapOutputIndex;
                                color = wrapColor;
                            }// else word is too long, dont backtrack to wrap
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }


        void PrepareOutput()
        {
            // pad the packet with spaces
            for (int i = outputIndex; i < PacketSize; i++)
            {
                output[i] = (byte)' ';
            }
            // make sure there is no trailing ampersand
            /*for( ; outputIndex > outputStart; outputIndex-- ) {
                if( output[outputIndex - 1] == '&' ) {
                    output[outputIndex - 1] = (byte)' ';
                } else if( output[outputIndex - 1] != ' ' ) {
                    break;
                }
            }*/
#if DEBUG_LINE_WRAPPER
            Console.WriteLine( "\"" + Encoding.ASCII.GetString( output, outputStart, outputIndex - outputStart ) + "\"" );
            Console.WriteLine();
#endif
        }


        bool Append(byte ch)
        {
            // calculate the number of characters to insert
            int bytesToInsert = 1;
            if (ch == (byte)'&') bytesToInsert++;

            bool prependColor = (lastColor != color || (color == NoColor && hadColor && outputIndex == outputStart));

            if (prependColor) bytesToInsert += 2;
            if (outputIndex + bytesToInsert + spaceCount > PacketSize)
            {
#if DEBUG_LINE_WRAPPER
                Console.WriteLine( "X ii={0} ({1}+{2}+{3}={4}) wl={5} wi={6} woi={7}",
                                   inputIndex,
                                   outputIndex, bytesToInsert, spaceCount, outputIndex + bytesToInsert + spaceCount,
                                   wordLength, wrapIndex, wrapOutputIndex );
#endif
                return false;
            }

            // append color, if changed since last word
            if (prependColor)
            {
                output[outputIndex++] = (byte)'&';
                output[outputIndex++] = color;
                lastColor = color;
            }

            int spacesToAppend = spaceCount;
            if (spaceCount > 0 && outputIndex > outputStart)
            {
                // append spaces that accumulated since last word
                while (spaceCount > 0)
                {
                    output[outputIndex++] = (byte)' ';
                    spaceCount--;
                }
                wordLength = 0;
            }
            wordLength += bytesToInsert;

#if DEBUG_LINE_WRAPPER
            Console.WriteLine( "ii={0} oi={1} wl={2} wi={3} woi={4} sc={5} bti={6}",
                               inputIndex, outputIndex, wordLength, wrapIndex, wrapOutputIndex, spacesToAppend, bytesToInsert );
#endif

            // append character
            if (ch == (byte)'&') output[outputIndex++] = ch;
            output[outputIndex++] = ch;
            return true;
        }


        static bool IsWordChar(byte ch)
        {
            return (ch > (byte)' ' && ch <= (byte)'~');
        }


        static bool ProcessColor(ref byte ch)
        {
            if (ch >= (byte)'A' && ch <= (byte)'Z')
            {
                ch += 32;
            }
            if (ch >= (byte)'a' && ch <= (byte)'f' ||
                ch >= (byte)'0' && ch <= (byte)'9')
            {
                return true;
            }
            switch (ch)
            {
                case (byte)'s':
                    ch = (byte)Color.Sys[1];
                    return true;

                case (byte)'y':
                    ch = (byte)Color.Say[1];
                    return true;

                case (byte)'p':
                    ch = (byte)Color.PM[1];
                    return true;

                case (byte)'r':
                    ch = (byte)Color.Announcement[1];
                    return true;

                case (byte)'h':
                    ch = (byte)Color.Help[1];
                    return true;

                case (byte)'w':
                    ch = (byte)Color.Warning[1];
                    return true;

                case (byte)'m':
                    ch = (byte)Color.Me[1];
                    return true;

                case (byte)'i':
                    ch = (byte)Color.IRC[1];
                    return true;
            }
            return false;
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }


        public void Dispose() { }


        #region IEnumerable<Packet> Members

        public IEnumerator<Packet> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        #endregion


        /// <summary> Creates a new line wrapper for a given raw string. </summary>
        public static LineWrapper Wrap(string message)
        {
            return new LineWrapper(message);
        }


        /// <summary> Creates a new line wrapper for a given raw string. </summary>
        public static LineWrapper WrapPrefixed(string prefix, string message)
        {
            return new LineWrapper(prefix, message);
        }
    }
}