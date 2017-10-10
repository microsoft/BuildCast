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
using System.Collections.Generic;
using System.Linq;

namespace BuildCast.Helpers
{
    public static class LinqExtensions
    {
        public static IEnumerable<T> LambdaDistinct<T>(this IEnumerable<T> list, Func<T, T, bool> lambda)
        {
            return list.Distinct(new LambdaCompare<T>(lambda));
        }

        public class LambdaCompare<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _expr;

            public LambdaCompare(Func<T, T, bool> lambda)
            {
                _expr = lambda;
            }

            public bool Equals(T x, T y)
            {
                return _expr(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}