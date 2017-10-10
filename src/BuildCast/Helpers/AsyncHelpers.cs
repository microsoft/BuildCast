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
using System.Threading;
using System.Threading.Tasks;

namespace BuildCast.Helpers
{
    /// <summary>
    /// A helper class to assist with the Asynchronous class initialization pattern
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncInitilizer<T>
    {
        private ManualResetEvent _initializeLock = new ManualResetEvent(false);
        private bool _hasInitialized;
        private bool _hasErrored;
        private Exception _initializationException;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncInitilizer{T}"/> class.
        /// </summary>
        public AsyncInitilizer()
        {
        }

        internal void InitializeWith(Func<Task> initializationTask)
        {
            try
            {
                initializationTask().ContinueWith((e) =>
                {
                    try
                    {
                        if (e.Exception != null)
                        {
                            _hasErrored = true;
                            _initializationException = e.Exception;
                        }
                    }
                    finally
                    {
                        _hasInitialized = true;
                        _initializeLock.Set();
                    }
                });
            }
            catch (Exception synchronous)
            {
                _hasErrored = true;
                _initializationException = synchronous;
            }
        }

        internal void CheckInitialized()
        {
            if (!_hasInitialized)
            {
                _initializeLock.WaitOne();
            }

            if (_hasErrored)
            {
                throw new Exception($"Initialization of {typeof(T).FullName} failed with an exception");
            }
        }
    }
}
