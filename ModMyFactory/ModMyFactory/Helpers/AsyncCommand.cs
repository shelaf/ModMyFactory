using System;
using System.Threading.Tasks;

namespace ModMyFactory.Helpers
{
    internal static class AsyncCommand
    {
        internal static async void Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                App.Instance.WriteExceptionLog(ex);
            }
        }
    }
}
