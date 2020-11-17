using System;
using System.Threading.Tasks;

namespace requests_http
{
    internal static class Program
    {
        internal static async Task Main(string[] args)
        {
            var runner = new RequestRunner(new RequestConfig(100, TimeSpan.FromSeconds(30), 10, new Uri("https://www.google.se")));
            await runner.Start();
        }
    }
}
