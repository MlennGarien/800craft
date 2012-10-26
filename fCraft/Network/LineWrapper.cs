// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// #define DEBUG_LINE_WRAPPER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace fCraft
{
    /// <summary> Intelligent line-wrapper for Minecraft protocol.
    /// Splits long messages into 64-character chunks of ASCII.
    /// Maintains colors between lines. Wraps at word boundaries and hyphens.
    /// Removes invalid characters and color sequences.
    /// Supports optional line prefixes for second and consequent lines.
    /// This class is implemented as IEnumerable of Packets, so it's usable with foreach() and Linq. </summary>
    public sealed class LineWrapper : IEnumerable<Packet>, IEnumerator<Packet>
    {
        const string DefaultPrefixString = "> ";
        static readonly byte[] DefaultPrefix;


        static LineWrapper()
        {
            DefaultPrefix = Encoding.ASCII.GetBytes(DefaultPrefixString);
        }


        const int MaxPrefixSize = 48;
        const int PacketSize = 66; // opcode + id + 64
        const byte DefaultColor = (byte)'f';

        public Packet Current { get; private set; }

        bool expectingColor;// whether next input character is expected to be a color code
        byte color,         // color that the next inserted character should be
             lastColor;     // used to detect duplicate color codes

        bool endsWithSymbol; // used to guarantee suffixes for symbols ("emotes")

        bool hadColor,      // used to see if white (&f) colorcodes should be inserted
             canWrap;       // used to see if a word needs to be forcefully wrapped (i.e. doesnt fit in one line)

        int spaceCount,     // used to track spacing between words
            wordLength;     // used to see whether to wrap at hyphens

        readonly byte[] prefix;

        readonly byte[] input;
        int inputIndex;

        byte[] output;
        int outputStart,
            outputIndex;

        int wrapInputIndex,     // index of the nearest line-wrapping opportunity in the input buffer 
            wrapOutputIndex;    // corresponding index in the output buffer
        byte wrapColor;         // value of "color" field at the wrapping point
        bool wrapEndsWithSymbol; // value of "endsWithSymbol" field at the wrapping point


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
            color = DefaultColor;
            wordLength = 0;
            inputIndex = 0;
            wrapInputIndex = 0;
            wrapOutputIndex = 0;
        }


        public bool MoveNext()
        {
            if (inputIndex >= input.Length)
            {
                return false;
            }

            output = new byte[PacketSize];
            output[0] = (byte)OpCode.Message;
            Current = new Packet(output);

            hadColor = false;
            canWrap = false;
            expectingColor = false;

            outputStart = 2;
            outputIndex = outputStart;
            spaceCount = 0;

            lastColor = DefaultColor;
            endsWithSymbol = false;

            wrapOutputIndex = outputStart;
            wrapColor = color;
            wrapEndsWithSymbol = false;

            // Prepend line prefix, if needed
            if (inputIndex > 0 && prefix.Length > 0)
            {
                int preBufferInputIndex = inputIndex;
                byte preBufferColor = color;
                color = DefaultColor;
                inputIndex = 0;
                wrapInputIndex = 0;
                wordLength = 0;
                while (inputIndex < prefix.Length)
                {
                    byte ch = prefix[inputIndex];
                    if (ProcessChar(ch))
                    {
                        // Should never happen, since prefix is under 48 chars
                        throw new Exception("Prefix required wrapping.");
                    }
                    inputIndex++;
                }
                inputIndex = preBufferInputIndex;
                color = preBufferColor;
                wrapColor = preBufferColor;
            }

            wordLength = 0;
            wrapInputIndex = inputIndex;
            canWrap = false; // to prevent linewrapping at prefix

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

            // No more input (last line)
            PrepareOutput();
            return true;
        }


        bool ProcessChar(byte ch)
        {
            switch (ch)
            {
                case (byte)' ':
                    canWrap = true;
                    expectingColor = false;
                    if (spaceCount == 0)
                    {
                        // first space after a word, set wrapping point
                        wrapInputIndex = inputIndex;
                        wrapOutputIndex = outputIndex;
                        wrapColor = color;
                        wrapEndsWithSymbol = endsWithSymbol;
                    }
                    spaceCount++;
                    break;

                case (byte)'&':
                    // skip double ampersands
                    expectingColor = !expectingColor;
                    break;

                case (byte)'-':
                    if (spaceCount > 0)
                    {
                        // set wrapping point, if at beginning of a word
                        wrapInputIndex = inputIndex;
                        wrapColor = color;
                        wrapEndsWithSymbol = endsWithSymbol;
                    }
                    expectingColor = false;
                    if (!Append(ch))
                    {
                        if (canWrap)
                        {
                            // word doesn't fit in line, backtrack to wrapping point
                            inputIndex = wrapInputIndex;
                            outputIndex = wrapOutputIndex;
                            color = wrapColor;
                            endsWithSymbol = wrapEndsWithSymbol;
                        } // else force wrap (word is too long), don't backtrack
                        return true;
                    }
                    spaceCount = 0;
                    if (wordLength > 2)
                    {
                        // allow wrapping after hyphen, if at least 2 word characters precede this hyphen
                        wrapInputIndex = inputIndex + 1;
                        wrapOutputIndex = outputIndex;
                        wrapColor = color;
                        wrapEndsWithSymbol = endsWithSymbol;
                        wordLength = 0;
                        canWrap = true;
                    }
                    break;

                case (byte)'\n':
                    // break the line early
                    inputIndex++;
                    return true;

                default:
                    if (expectingColor)
                    {
                        expectingColor = false;
                        if (ch == 'N' || ch == 'n')
                        {
                            // newline
                            inputIndex++;
                            return true;
                        }
                        else if (ProcessColor(ref ch))
                        {
                            // valid colorcode
                            color = ch;
                            hadColor = true;
                        } // else colorcode is invalid, skip
                    }
                    else
                    {
                        if (spaceCount > 0)
                        {
                            // set wrapping point, if at beginning of a word
                            wrapInputIndex = inputIndex;
                            wrapColor = color;
                            wrapEndsWithSymbol = endsWithSymbol;
                        }
                        if (ch == 0 || ch > 127)
                        {
                            // replace unprintable chars with '?'
                            ch = (byte)'?';
                        }
                        if (!Append(ch))
                        {
                            if (canWrap)
                            {
                                inputIndex = wrapInputIndex;
                                outputIndex = wrapOutputIndex;
                                color = wrapColor;
                                endsWithSymbol = wrapEndsWithSymbol;
                            } // else word is too long, dont backtrack to wrap
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
            if (endsWithSymbol)
            {
                output[65] = (byte)'.';
            }
#if DEBUG_LINE_WRAPPER
            Console.WriteLine( "\"" + Encoding.ASCII.GetString( output, outputStart, outputIndex - outputStart ) + "\"" );
            Console.WriteLine();
#endif
        }


        bool Append(byte ch)
        {
            bool prependColor =
                // color changed since last inserted character
                lastColor != color ||
                // no characters have been inserted, but color codes have been encountered
                (color == DefaultColor && hadColor && outputIndex == outputStart);

            // calculate the number of characters to insert
            int bytesToInsert = 1 + spaceCount;
            if (prependColor) bytesToInsert += 2;

            // calculating requirements for the next symbol
            if (ch < ' ')
            {
                switch (ch)
                {
                    case 7:
                    case 25:
                        bytesToInsert += 3; // 2 spaces pad AND 1-char terminator
                        break;
                    case 9:
                    case 22:
                    case 24:
                        bytesToInsert += 2; // 2 spaces pad OR 1-char terminator
                        break;
                    case 12:
                    case 18:
                    case 19:
                        bytesToInsert += 2; // 2 spaces pad OR 2-char terminator
                        break;
                    default:
                        bytesToInsert += 1; // 1-char terminator
                        break;
                }
            }

            if (outputIndex + bytesToInsert > PacketSize)
            {
#if DEBUG_LINE_WRAPPER
                Console.WriteLine( "X ii={0} ({1}+{2}+{3}={4}) wl={5} wi={6} woi={7}",
                                   inputIndex,
                                   outputIndex, bytesToInsert, spaceCount, outputIndex + bytesToInsert + spaceCount,
                                   wordLength, wrapInputIndex, wrapOutputIndex );
#endif
                return false;
            }

            // append color, if changed since last inserted character
            if (prependColor)
            {
                output[outputIndex++] = (byte)'&';
                output[outputIndex++] = color;
                lastColor = color;
            }

#if DEBUG_LINE_WRAPPER
            int spacesToAppend = spaceCount;
#endif

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
                               inputIndex, outputIndex, wordLength, wrapInputIndex, wrapOutputIndex, spacesToAppend, bytesToInsert );
#endif

            // append character
            output[outputIndex++] = ch;

            // padding for symbols
            switch (ch)
            {
                case 9:
                    output[outputIndex++] = (byte)' ';
                    output[outputIndex++] = (byte)'.';
                    endsWithSymbol = false;
                    break;
                case 12:
                    output[outputIndex++] = (byte)' ';
                    output[outputIndex++] = (byte)'`';
                    endsWithSymbol = false;
                    break;
                case 18:
                    output[outputIndex++] = (byte)' ';
                    output[outputIndex++] = (byte)'.';
                    endsWithSymbol = false;
                    break;
                case 19:
                    output[outputIndex++] = (byte)' ';
                    output[outputIndex++] = (byte)'!';
                    endsWithSymbol = false;
                    break;
                case 22:
                    output[outputIndex++] = (byte)'.';
                    output[outputIndex++] = (byte)' ';
                    endsWithSymbol = false;
                    break;
                case 24:
                    output[outputIndex++] = (byte)'^';
                    output[outputIndex++] = (byte)' ';
                    endsWithSymbol = false;
                    break;
                case 7:
                case 25:
                    output[outputIndex++] = (byte)' ';
                    output[outputIndex++] = (byte)' ';
                    endsWithSymbol = true;
                    break;
                default:
                    endsWithSymbol = (ch < ' ');
                    break;
            }
            return true;
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


        void IDisposable.Dispose() { }


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
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        public static LineWrapper Wrap(string message)
        {
            return new LineWrapper(message);
        }


        /// <summary> Creates a new line wrapper for a given raw string. </summary>
        /// <exception cref="ArgumentNullException"> prefix or message is null. </exception>
        /// <exception cref="ArgumentException"> prefix length exceeds maximum allowed value (48 characters). </exception>
        public static LineWrapper WrapPrefixed(string prefix, string message)
        {
            return new LineWrapper(prefix, message);
        }
    }
}