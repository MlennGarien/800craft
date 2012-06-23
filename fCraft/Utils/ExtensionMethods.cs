// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft
{

    public static class IPAddressUtil
    {
        /// <summary> Checks whether an IP address may belong to LAN (192.168.0.0/16 or 10.0.0.0/24). </summary>
        public static bool IsLAN([NotNull] this IPAddress addr)
        {
            if (addr == null) throw new ArgumentNullException("addr");
            byte[] bytes = addr.GetAddressBytes();
            return (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 10);
        }

        public static uint AsUInt([NotNull] this IPAddress thisAddr)
        {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            return (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(thisAddr.GetAddressBytes(), 0));
        }

        public static int AsInt([NotNull] this IPAddress thisAddr)
        {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            return IPAddress.HostToNetworkOrder(BitConverter.ToInt32(thisAddr.GetAddressBytes(), 0));
        }

        public static bool Match([NotNull] this IPAddress thisAddr, uint otherAddr, uint mask)
        {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            uint thisAsUInt = thisAddr.AsUInt();
            return (thisAsUInt & mask) == (otherAddr & mask);
        }

        public static IPAddress RangeMin([NotNull] this IPAddress thisAddr, byte range)
        {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            if (range > 32) throw new ArgumentOutOfRangeException("range");
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)NetMask(range);
            return new IPAddress(IPAddress.HostToNetworkOrder(thisAsInt & mask));
        }

        public static IPAddress RangeMax([NotNull] this IPAddress thisAddr, byte range)
        {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            if (range > 32) throw new ArgumentOutOfRangeException("range");
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)~NetMask(range);
            return new IPAddress((uint)IPAddress.HostToNetworkOrder(thisAsInt | mask));
        }

        public static uint NetMask(byte range)
        {
            if (range > 32) throw new ArgumentOutOfRangeException("range");
            if (range == 0)
            {
                return 0;
            }
            else
            {
                return 0xffffffff << (32 - range);
            }
        }
    }


    public static class DateTimeUtil
    {
        static readonly NumberFormatInfo NumberFormatter = CultureInfo.InvariantCulture.NumberFormat;
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly long TicksToUnixEpoch;
        const long TicksPerMillisecond = 10000;

        static DateTimeUtil()
        {
            TicksToUnixEpoch = UnixEpoch.Ticks;
        }


        #region To Unix Time

        /// <summary> Converts a DateTime to Utc Unix Timestamp. </summary>
        public static long ToUnixTime(this DateTime date)
        {
            return (long)date.Subtract(UnixEpoch).TotalSeconds;
        }


        public static long ToUnixTimeLegacy(this DateTime date)
        {
            return (date.Ticks - TicksToUnixEpoch) / TicksPerMillisecond;
        }


        /// <summary> Converts a DateTime to a string containing the Utc Unix Timestamp.
        /// If the date equals DateTime.MinValue, returns an empty string. </summary>
        public static string ToUnixTimeString(this DateTime date)
        {
            if (date == DateTime.MinValue)
            {
                return "";
            }
            else
            {
                return date.ToUnixTime().ToString(NumberFormatter);
            }
        }


        /// <summary> Appends a Utc Unix Timestamp to the given StringBuilder.
        /// If the date equals DateTime.MinValue, nothing is appended. </summary>
        public static StringBuilder ToUnixTimeString(this DateTime date, StringBuilder sb)
        {
            if (date != DateTime.MinValue)
            {
                sb.Append(date.ToUnixTime());
            }
            return sb;
        }

        #endregion


        #region To Date Time

        /// <summary> Creates a DateTime from a Utc Unix Timestamp. </summary>
        public static DateTime ToDateTime(this long timestamp)
        {
            return UnixEpoch.AddSeconds(timestamp);
        }


        /// <summary> Tries to create a DateTime from a string containing a Utc Unix Timestamp.
        /// If the string was empty, returns false and does not affect result. </summary>
        public static bool ToDateTime(this string str, ref DateTime result)
        {
            long t;
            if (str.Length > 1 && Int64.TryParse(str, out t))
            {
                result = UnixEpoch.AddSeconds(Int64.Parse(str));
                return true;
            }
            return false;
        }


        public static DateTime ToDateTimeLegacy(long timestamp)
        {
            return new DateTime(timestamp * TicksPerMillisecond + TicksToUnixEpoch, DateTimeKind.Utc);
        }


        public static bool ToDateTimeLegacy(this string str, ref DateTime result)
        {
            if (str.Length <= 1)
            {
                return false;
            }
            result = ToDateTimeLegacy(Int64.Parse(str));
            return true;
        }

        #endregion


        /// <summary> Converts a TimeSpan to a string containing the number of seconds.
        /// If the timestamp is zero seconds, returns an empty string. </summary>
        public static string ToTickString(this TimeSpan time)
        {
            if (time == TimeSpan.Zero)
            {
                return "";
            }
            else
            {
                return (time.Ticks / TimeSpan.TicksPerSecond).ToString(NumberFormatter);
            }
        }


        public static long ToSeconds(this TimeSpan time)
        {
            return (time.Ticks / TimeSpan.TicksPerSecond);
        }


        /// <summary> Tries to create a TimeSpan from a string containing the number of seconds.
        /// If the string was empty, returns false and sets result to TimeSpan.Zero </summary>
        public static bool ToTimeSpan([NotNull] this string str, out TimeSpan result)
        {
            if (str == null) throw new ArgumentNullException("str");
            if (str.Length == 0)
            {
                result = TimeSpan.Zero;
                return true;
            }
            long ticks;
            if (Int64.TryParse(str, out ticks))
            {
                result = new TimeSpan(ticks * TimeSpan.TicksPerSecond);
                return true;
            }
            else
            {
                result = TimeSpan.Zero;
                return false;
            }
        }


        public static bool ToTimeSpanLegacy(this string str, ref TimeSpan result)
        {
            if (str.Length > 1)
            {
                result = new TimeSpan(Int64.Parse(str) * TicksPerMillisecond);
                return true;
            }
            else
            {
                return false;
            }
        }


        #region MiniString

        public static StringBuilder ToTickString(this TimeSpan time, StringBuilder sb)
        {
            if (time != TimeSpan.Zero)
            {
                sb.Append(time.Ticks / TimeSpan.TicksPerSecond);
            }
            return sb;
        }


        public static string ToMiniString(this TimeSpan span)
        {
            if (span.TotalSeconds < 60)
            {
                return String.Format("{0}s", span.Seconds);
            }
            else if (span.TotalMinutes < 60)
            {
                return String.Format("{0}m{1}s", span.Minutes, span.Seconds);
            }
            else if (span.TotalHours < 48)
            {
                return String.Format("{0}h{1}m", (int)Math.Floor(span.TotalHours), span.Minutes);
            }
            else if (span.TotalDays < 15)
            {
                return String.Format("{0}d{1}h", span.Days, span.Hours);
            }
            else
            {
                return String.Format("{0:0}w{1:0}d", Math.Floor(span.TotalDays / 7), Math.Floor(span.TotalDays) % 7);
            }
        }


        public static bool TryParseMiniTimespan(this string text, out TimeSpan result)
        {
            try
            {
                result = ParseMiniTimespan(text);
                return true;
            }
            catch (ArgumentException)
            {
            }
            catch (OverflowException)
            {
            }
            catch (FormatException) { }
            result = TimeSpan.Zero;
            return false;
        }


        public static readonly TimeSpan MaxTimeSpan = TimeSpan.FromDays(9999);


        public static TimeSpan ParseMiniTimespan([NotNull] this string text)
        {
            if (text == null) throw new ArgumentNullException("text");

            text = text.Trim();
            bool expectingDigit = true;
            TimeSpan result = TimeSpan.Zero;
            int digitOffset = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (expectingDigit)
                {
                    if (text[i] < '0' || text[i] > '9')
                    {
                        throw new FormatException();
                    }
                    expectingDigit = false;
                }
                else
                {
                    if (text[i] >= '0' && text[i] <= '9')
                    {
                        continue;
                    }
                    else
                    {
                        string numberString = text.Substring(digitOffset, i - digitOffset);
                        digitOffset = i + 1;
                        int number = Int32.Parse(numberString);
                        switch (Char.ToLower(text[i]))
                        {
                            case 's':
                                result += TimeSpan.FromSeconds(number);
                                break;
                            case 'm':
                                result += TimeSpan.FromMinutes(number);
                                break;
                            case 'h':
                                result += TimeSpan.FromHours(number);
                                break;
                            case 'd':
                                result += TimeSpan.FromDays(number);
                                break;
                            case 'w':
                                result += TimeSpan.FromDays(number * 7);
                                break;
                            default:
                                throw new FormatException();
                        }
                    }
                }
            }
            return result;
        }

        #endregion


        #region CompactString

        public static string ToCompactString(this DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
        }


        public static string ToCompactString(this TimeSpan span)
        {
            return String.Format("{0}.{1:00}:{2:00}:{3:00}",
                                  span.Days, span.Hours, span.Minutes, span.Seconds);
        }

        #endregion


        static CultureInfo cultureInfo = CultureInfo.CurrentCulture;

        /// <summary> Tries to parse a data in a culture-specific ways.
        /// This method is, unfortunately, necessary because in versions 0.520-0.522,
        /// fCraft saved dates in a culture-specific format. This means that if the
        /// server's culture settings were changed, or if the PlayerDB and IPBanList
        /// files were moved between machines, all dates became unparseable. </summary>
        /// <param name="dateString"> String to parse. </param>
        /// <param name="date"> Date to output. </param>
        /// <returns> True if date string could be parsed and was not empty/MinValue. </returns>
        public static bool TryParseLocalDate([NotNull] string dateString, out DateTime date)
        {
            if (dateString == null) throw new ArgumentNullException("dateString");
            if (dateString.Length <= 1)
            {
                date = DateTime.MinValue;
                return false;
            }
            else
            {
                if (!DateTime.TryParse(dateString, cultureInfo, DateTimeStyles.None, out date))
                {
                    CultureInfo[] cultureList = CultureInfo.GetCultures(CultureTypes.FrameworkCultures);
                    foreach (CultureInfo otherCultureInfo in cultureList)
                    {
                        cultureInfo = otherCultureInfo;
                        try
                        {
                            if (DateTime.TryParse(dateString, cultureInfo, DateTimeStyles.None, out date))
                            {
                                return true;
                            }
                        }
                        catch (NotSupportedException) { }
                    }
                    throw new Exception("Could not find a culture that would be able to parse date/time formats.");
                }
                else
                {
                    return true;
                }
            }
        }
    }


    public static class EnumerableUtil
    {
        /// <summary> Joins all items in a collection into one comma-separated string.
        /// If the items are not strings, .ToString() is called on them. </summary>
        public static string JoinToString<T>([NotNull] this IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (T item in items)
            {
                if (!first) sb.Append(',').Append(' ');
                sb.Append(item);
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// If the items are not strings, .ToString() is called on them. </summary>
        public static string JoinToString<T>([NotNull] this IEnumerable<T> items, [NotNull] string separator)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (separator == null) throw new ArgumentNullException("separator");
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (T item in items)
            {
                if (!first) sb.Append(separator);
                sb.Append(item);
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// A specified string conversion function is called on each item before contactenation. </summary>
        public static string JoinToString<T>([NotNull] this IEnumerable<T> items, [NotNull] Func<T, string> stringConversionFunction)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (stringConversionFunction == null) throw new ArgumentNullException("stringConversionFunction");
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (T item in items)
            {
                if (!first) sb.Append(',').Append(' ');
                sb.Append(stringConversionFunction(item));
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins all items in a collection into one string separated with the given separator.
        /// A specified string conversion function is called on each item before contactenation. </summary>
        public static string JoinToString<T>([NotNull] this IEnumerable<T> items, [NotNull] string separator, [NotNull] Func<T, string> stringConversionFunction)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (separator == null) throw new ArgumentNullException("separator");
            if (stringConversionFunction == null) throw new ArgumentNullException("stringConversionFunction");
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (T item in items)
            {
                if (!first) sb.Append(separator);
                sb.Append(stringConversionFunction(item));
                first = false;
            }
            return sb.ToString();
        }


        /// <summary> Joins formatted names of all IClassy objects in a collection into one comma-separated string. </summary>
        public static string JoinToClassyString([NotNull] this IEnumerable<IClassy> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            return items.JoinToString("  ", p => p.ClassyName);
        }
    }


    unsafe static class FormatUtil
    {
        // Quicker StringBuilder.Append(int) by Sam Allen of http://www.dotnetperls.com
        public static StringBuilder Digits([NotNull] this StringBuilder builder, int number)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (number >= 100000000 || number < 0)
            {
                // Use system ToString.
                builder.Append(number);
                return builder;
            }
            int copy;
            int digit;
            if (number >= 10000000)
            {
                // 8.
                copy = number % 100000000;
                digit = copy / 10000000;
                builder.Append((char)(digit + 48));
            }
            if (number >= 1000000)
            {
                // 7.
                copy = number % 10000000;
                digit = copy / 1000000;
                builder.Append((char)(digit + 48));
            }
            if (number >= 100000)
            {
                // 6.
                copy = number % 1000000;
                digit = copy / 100000;
                builder.Append((char)(digit + 48));
            }
            if (number >= 10000)
            {
                // 5.
                copy = number % 100000;
                digit = copy / 10000;
                builder.Append((char)(digit + 48));
            }
            if (number >= 1000)
            {
                // 4.
                copy = number % 10000;
                digit = copy / 1000;
                builder.Append((char)(digit + 48));
            }
            if (number >= 100)
            {
                // 3.
                copy = number % 1000;
                digit = copy / 100;
                builder.Append((char)(digit + 48));
            }
            if (number >= 10)
            {
                // 2.
                copy = number % 100;
                digit = copy / 10;
                builder.Append((char)(digit + 48));
            }
            if (number >= 0)
            {
                // 1.
                copy = number % 10;
                builder.Append((char)(copy + 48));
            }
            return builder;
        }

        // Quicker Int32.Parse(string) by Karl Seguin
        public static int Parse([NotNull] string stringToConvert)
        {
            if (stringToConvert == null) throw new ArgumentNullException("stringToConvert");
            int value = 0;
            int length = stringToConvert.Length;
            fixed (char* characters = stringToConvert)
            {
                for (int i = 0; i < length; ++i)
                {
                    value = 10 * value + (characters[i] - 48);
                }
            }
            return value;
        }

        // UppercaseFirst by Sam Allen of http://www.dotnetperls.com
        public static string UppercaseFirst(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }


    public unsafe static class BufferUtil
    {
        public static void MemSet([NotNull] this byte[] array, byte value)
        {
            if (array == null) throw new ArgumentNullException("array");
            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64(rawValue, 0);

            fixed (byte* ptr = array)
            {
                Int64* dest = (Int64*)ptr;
                int length = array.Length;
                while (length >= 8)
                {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for (byte i = 0; i < length; i++)
                {
                    *bDest = value;
                    bDest++;
                }
            }
        }


        public static void MemSet([NotNull] this byte[] array, byte value, int startIndex, int length)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (length < 0 || length > array.Length)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (startIndex < 0 || startIndex + length > array.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }

            byte[] rawValue = new[] { value, value, value, value, value, value, value, value };
            Int64 fillValue = BitConverter.ToInt64(rawValue, 0);

            fixed (byte* ptr = &array[startIndex])
            {
                Int64* dest = (Int64*)ptr;
                while (length >= 8)
                {
                    *dest = fillValue;
                    dest++;
                    length -= 8;
                }
                byte* bDest = (byte*)dest;
                for (byte i = 0; i < length; i++)
                {
                    *bDest = value;
                    bDest++;
                }
            }
        }


        public static void MemCpy([NotNull] byte* src, [NotNull] byte* dest, int len)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (dest == null) throw new ArgumentNullException("dest");
            if (len >= 0x10)
            {
                do
                {
                    *((int*)dest) = *((int*)src);
                    *((int*)(dest + 4)) = *((int*)(src + 4));
                    *((int*)(dest + 8)) = *((int*)(src + 8));
                    *((int*)(dest + 12)) = *((int*)(src + 12));
                    dest += 0x10;
                    src += 0x10;
                }
                while ((len -= 0x10) >= 0x10);
            }
            if (len > 0)
            {
                if ((len & 8) != 0)
                {
                    *((int*)dest) = *((int*)src);
                    *((int*)(dest + 4)) = *((int*)(src + 4));
                    dest += 8;
                    src += 8;
                }
                if ((len & 4) != 0)
                {
                    *((int*)dest) = *((int*)src);
                    dest += 4;
                    src += 4;
                }
                if ((len & 2) != 0)
                {
                    *((short*)dest) = *((short*)src);
                    dest += 2;
                    src += 2;
                }
                if ((len & 1) != 0)
                {
                    dest++;
                    src++;
                    dest[0] = src[0];
                }
            }
        }


        public static int SizeOf(object obj)
        {
            return SizeOf(obj.GetType());
        }
    }


    public static class EnumUtil
    {
        public static bool TryParse<TEnum>([NotNull] string value, out TEnum output, bool ignoreCase)
        {
            if (value == null) throw new ArgumentNullException("value");
            try
            {
                output = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
                return Enum.IsDefined(typeof(TEnum), output);
            }
            catch (ArgumentException)
            {
                output = default(TEnum);
                return false;
            }
        }
    }
}