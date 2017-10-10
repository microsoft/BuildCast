// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace BuildCast.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTimeOffset ParseDateTimeRFC822(string dateTimeString)
        {
            StringBuilder dateTimeStringBuilder = new StringBuilder(dateTimeString.Trim());
            if (dateTimeStringBuilder.Length < 18)
            {
                throw new FormatException("Invalid date format. Expected date in RFC 822 format");
            }

            if (dateTimeStringBuilder[3] == ',')
            {
                // There is a leading (e.g.) "Tue, ", strip it off
                dateTimeStringBuilder.Remove(0, 4);

                // There's supposed to be a space here but some implementations dont have one
                RemoveExtraWhiteSpaceAtStart(dateTimeStringBuilder);
            }

            ReplaceMultipleWhiteSpaceWithSingleWhiteSpace(dateTimeStringBuilder);
            if (char.IsDigit(dateTimeStringBuilder[1]))
            {
                // two-digit day, we are good
            }
            else
            {
                dateTimeStringBuilder.Insert(0, '0');
            }

            if (dateTimeStringBuilder.Length < 19)
            {
                throw new FormatException("Invalid date format. Expected date in RFC 822 format");
            }

            bool thereAreSeconds = dateTimeStringBuilder[17] == ':';
            int timeZoneStartIndex;
            if (thereAreSeconds)
            {
                timeZoneStartIndex = 21;
            }
            else
            {
                timeZoneStartIndex = 18;
            }

            string timeZoneSuffix = dateTimeStringBuilder.ToString().Substring(timeZoneStartIndex);
            dateTimeStringBuilder.Remove(timeZoneStartIndex, dateTimeStringBuilder.Length - timeZoneStartIndex);
            bool isUtc;
            dateTimeStringBuilder.Append(NormalizeTimeZone(timeZoneSuffix, out isUtc));
            string wellFormattedString = dateTimeStringBuilder.ToString();

            DateTimeOffset theTime;
            string parseFormat;
            if (thereAreSeconds)
            {
                parseFormat = "dd MMM yyyy HH:mm:ss zzz";
            }
            else
            {
                parseFormat = "dd MMM yyyy HH:mm zzz";
            }

            if (DateTimeOffset.TryParseExact(wellFormattedString, parseFormat,
                CultureInfo.InvariantCulture.DateTimeFormat,
                isUtc ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None, out theTime))
            {
                return theTime;
            }

            throw new FormatException("Invalid date format. Expected date in RFC 822 format");
        }

        private static string NormalizeTimeZone(string rfc822TimeZone, out bool isUtc)
        {
            isUtc = false;

            // return a string in "-08:00" format
            if (rfc822TimeZone[0] == '+' || rfc822TimeZone[0] == '-')
            {
                // the time zone is supposed to be 4 digits but some feeds omit the initial 0
                StringBuilder result = new StringBuilder(rfc822TimeZone);
                if (result.Length == 4)
                {
                    // the timezone is +/-HMM. Convert to +/-HHMM
                    result.Insert(1, '0');
                }

                result.Insert(3, ':');
                return result.ToString();
            }

            switch (rfc822TimeZone)
            {
                case "UT":
                case "Z":
                    isUtc = true;
                    return "-00:00";
                case "GMT":
                    return "-00:00";
                case "A":
                    return "-01:00";
                case "B":
                    return "-02:00";
                case "C":
                    return "-03:00";
                case "D":
                case "EDT":
                    return "-04:00";
                case "E":
                case "EST":
                case "CDT":
                    return "-05:00";
                case "F":
                case "CST":
                case "MDT":
                    return "-06:00";
                case "G":
                case "MST":
                case "PDT":
                    return "-07:00";
                case "H":
                case "PST":
                    return "-08:00";
                case "I":
                    return "-09:00";
                case "K":
                    return "-10:00";
                case "L":
                    return "-11:00";
                case "M":
                    return "-12:00";
                case "N":
                    return "+01:00";
                case "O":
                    return "+02:00";
                case "P":
                    return "+03:00";
                case "Q":
                    return "+04:00";
                case "R":
                    return "+05:00";
                case "S":
                    return "+06:00";
                case "T":
                    return "+07:00";
                case "U":
                    return "+08:00";
                case "V":
                    return "+09:00";
                case "W":
                    return "+10:00";
                case "X":
                    return "+11:00";
                case "Y":
                    return "+12:00";
                default:
                    return string.Empty;
            }
        }

        private static void RemoveExtraWhiteSpaceAtStart(StringBuilder stringBuilder)
        {
            int i = 0;
            while (i < stringBuilder.Length)
            {
                if (!char.IsWhiteSpace(stringBuilder[i]))
                {
                    break;
                }

                ++i;
            }

            if (i > 0)
            {
                stringBuilder.Remove(0, i);
            }
        }

        private static void ReplaceMultipleWhiteSpaceWithSingleWhiteSpace(StringBuilder builder)
        {
            int index = 0;
            int whiteSpaceStart = -1;
            while (index < builder.Length)
            {
                if (char.IsWhiteSpace(builder[index]))
                {
                    if (whiteSpaceStart < 0)
                    {
                        whiteSpaceStart = index;

                        // normalize all white spaces to be ' ' so that the date time parsing works
                        builder[index] = ' ';
                    }
                }
                else if (whiteSpaceStart >= 0)
                {
                    if (index > whiteSpaceStart + 1)
                    {
                        // there are at least 2 spaces... replace by 1
                        builder.Remove(whiteSpaceStart, index - whiteSpaceStart - 1);
                        index = whiteSpaceStart + 1;
                    }

                    whiteSpaceStart = -1;
                }

                ++index;
            }

            // we have already trimmed the start and end so there cannot be a trail of white spaces in the end
            Debug.Assert(builder.Length == 0 || builder[builder.Length - 1] != ' ', "The string builder doesnt end in a white space");
        }
    }
}
