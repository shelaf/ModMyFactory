using System.Threading.Tasks;
using ModMyFactory.Views;

namespace ModMyFactory.Helpers
{
    static class ProgressWindowExtensions
    {
        /// <summary>
        /// Shows this ProgressWindow modally while the given task runs, and closes it safely
        /// once the task completes - even if the task completes before the window has actually
        /// been shown, avoiding the "ShowDialog after Close" race.
        /// </summary>
        public static async Task ShowProgressAsync(this ProgressWindow progressWindow, Task task)
        {
            bool windowShown = false;
            bool closeRequested = false;

            progressWindow.ContentRendered += (sender, e) =>
            {
                windowShown = true;
                if (closeRequested) progressWindow.Close();
            };

            Task closeWindowTask = task.ContinueWith(t => progressWindow.Dispatcher.Invoke(() =>
            {
                if (windowShown) progressWindow.Close();
                else closeRequested = true;
            }));

            progressWindow.ShowDialog();

            await closeWindowTask;
        }
    }
}
