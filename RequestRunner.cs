using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace requests_http
{
    public class RequestRunner
    {
        private readonly HttpClient _client;
        private readonly RequestConfig _config;

        public RequestRunner(RequestConfig config)
        {
            _client = new HttpClient();
            _config = config;
        }

        public async Task Start()
        {
            var tasks = new List<Task<List<Metric>>>();

            for (int i = 0; i < _config.Workers; i++)
            {
                int runs = _config.N / _config.Workers;
                tasks.Add(Task.Run(() => InvokeRequests(runs)));
            }

            var results = (await Task.WhenAll(tasks))
                .SelectMany(x => x)
                .GroupBy(k => k.Status)
                .ToDictionary(g => g.Key, g => g.ToList());

            Print(results);
        }

        private void Print(Dictionary<HttpStatusCode, List<Metric>> results)
        {
            foreach (var pair in results)
            {
                Console.WriteLine(pair.Key);
                foreach (var item in pair.Value)
                {
                    Console.Write($"{string.Join(", ", item)}");
                }
            }
        }

        private HttpRequestMessage BuildRequestMessage()
        {
            var method = _config.RequestType switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                _ => HttpMethod.Get
            };

            return new HttpRequestMessage
            {
                Method = method,
                RequestUri = _config.Url
            };
        }

        private async Task<List<Metric>> InvokeRequests(int runs)
        {
            List<Metric> results = new();
            var message = BuildRequestMessage();

            for (int i = 0; i < runs; i++)
            {
                var watch = new Stopwatch();

                watch.Start();
                var response = await _client.SendAsync(new HttpRequestMessage(message.Method, message.RequestUri));
                watch.Stop();

                var metric = new Metric(response.StatusCode, watch.Elapsed);

                results.Add(metric);
            }

            return results;
        }
    }

    public record RequestConfig(int N, TimeSpan Timeout, int Workers, Uri Url, string RequestType = "GET");
    public record Metric(HttpStatusCode Status, TimeSpan Duration);
}