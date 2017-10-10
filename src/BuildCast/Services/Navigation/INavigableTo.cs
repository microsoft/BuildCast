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

using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;

namespace BuildCast.Services.Navigation
{
    public interface INavigableTo
    {
        /// <summary>
        /// The event that gets called by the Navigation Service after navigation has completed.
        /// </summary>
        /// <remarks>
        /// This gets called prior to <see cref="Windows.UI.Xaml.Controls.Page.OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs)"/>
        /// </remarks>
        /// <param name="navigationMode">The navigation stack characteristic of the navigation.</param>
        /// <param name="parameter">The parameter passed to the navigation service</param>
        /// <returns>An awaitable Task</returns>
        Task NavigatedTo(NavigationMode navigationMode, object parameter);
    }
}
