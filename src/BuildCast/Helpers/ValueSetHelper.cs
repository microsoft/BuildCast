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
using Windows.Foundation.Collections;

namespace BuildCast.Helpers
{
    public static class ValueSetHelper
    {
        public static ValueSet GetValueSet(this ValueSet values, string key)
        {
            ValueSet result = null;

            if (values.TryGetValue(key, out object retrieveObject))
            {
                result = (ValueSet)retrieveObject;
            }

            return result;
        }

        public static TimeSpan GetTimeSpan(this ValueSet values, string key)
        {
            TimeSpan value = TimeSpan.MinValue;

            if (values.TryGetValue(key, out object retrieveObject))
            {
                if (retrieveObject != null)
                {
                    value = TimeSpan.FromMilliseconds((double)retrieveObject);
                }
            }

            return value;
        }

        public static DateTimeOffset GetDateTimeOffset(this ValueSet values, string key)
        {
            DateTimeOffset value = DateTimeOffset.MinValue;

            if (values.TryGetValue(key, out object retrieveObject))
            {
                if (retrieveObject != null)
                {
                    value = DateTimeOffset.FromUnixTimeMilliseconds((long)retrieveObject);
                }
            }

            return value;
        }

        public static Uri GetURI(this ValueSet values, string key)
        {
            Uri value = null;

            if (values.TryGetValue(key, out object retrieveObject))
            {
                if (retrieveObject != null)
                {
                    value = new Uri((string)retrieveObject);
                }
            }

            return value;
        }

        public static string GetString(this ValueSet values, string key)
        {
            string result = string.Empty;

            if (values.TryGetValue(key, out object retrieveObject))
            {
                result = (string)retrieveObject;
            }

            return result;
        }
    }
}
