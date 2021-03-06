﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQLAzureDemo.HttpFlooder
{
    class Program
    {
        private static readonly Regex Regex = new Regex(@"There are <span class=""label label-info"">(\d+)</span> that were returned with an average creation year of <span class=""label label-info"">(\d+)</span>");
        static void Main()
        {
            const int noOfRequests = 304;
            var urls = new[]
            {
                "http://mscloudperthdemo1.azurewebsites.net/TransientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo2.azurewebsites.net/TransientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo3.azurewebsites.net/TransientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo4.azurewebsites.net/TransientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo1.azurewebsites.net/ResilientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo2.azurewebsites.net/ResilientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo3.azurewebsites.net/ResilientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo4.azurewebsites.net/ResilientNHibernate?q={0}&page={1}",
                "http://mscloudperthdemo1.azurewebsites.net/TransientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo2.azurewebsites.net/TransientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo3.azurewebsites.net/TransientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo4.azurewebsites.net/TransientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo1.azurewebsites.net/ResilientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo2.azurewebsites.net/ResilientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo3.azurewebsites.net/ResilientEntityFramework?q={0}&page={1}",
                "http://mscloudperthdemo4.azurewebsites.net/ResilientEntityFramework?q={0}&page={1}"
            };
            ServicePointManager.DefaultConnectionLimit = 500;
            ServicePointManager.MaxServicePointIdleTime = 5*60*1000;
            var random = new Random();
            var tasks = new List<Task>();

            for (var i = 0; i < noOfRequests; i++)
            {
                var searchTerm = GetRandomSearchString(random);
                var page = random.Next(1, 250);
                var stopwatch = Stopwatch.StartNew();
                Console.WriteLine("Beginning search for {0}, page {1}, site {2}", searchTerm, page, i%urls.Length+1);
                tasks.Add(
                    SendHttpRequest(string.Format(urls[i%urls.Length], searchTerm, page))
                        .ContinueWith(async r => await ProcessHttpResponse(stopwatch, searchTerm, r.Result)).Unwrap()
                        .ContinueWith(s => Console.WriteLine(s.Result))
                );
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException e)
            {
                foreach (var exception in e.InnerExceptions)
                    Console.WriteLine(exception);
            }
            Console.WriteLine("Finished. Press any key...");
            Console.ReadLine();
        }

        private static async Task<string> ProcessHttpResponse(Stopwatch stopwatch, string searchTerm, HttpResponseMessage response)
        {
            stopwatch.Stop();

            var s = new StringBuilder();
            var statusCode = response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();
            var match = Regex.Match(content);
            
            s.Append(string.Format("Finished search for {0} with HTTP status code of {1} in {2}s.", searchTerm, response.StatusCode, stopwatch.Elapsed.TotalSeconds));

            if (statusCode == HttpStatusCode.OK)
                if (match.Success)
                    s.Append(string.Format(" {0} Results with average creation year of {1}.", match.Groups[1].Value, match.Groups[2].Value));
                else
                    s.Append(" No results returned.");

            return s.ToString();
        }

        public static async Task<HttpResponseMessage> SendHttpRequest(string url)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            return await client.GetAsync(url);
        }

        private static string GetRandomSearchString(Random random)
        {
            var searchTerms = new[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
                'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
            };
            var searchTerm = new StringBuilder();
            var numLetters = random.Next(1, 4);
            for (var j = 0; j < numLetters; j++)
                searchTerm.Append(searchTerms[random.Next(0, searchTerms.Length)]);
            return searchTerm.ToString();
        }
    }
}
