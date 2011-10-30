using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SignalR.Client;

namespace crank
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Crank v{0}", typeof(Program).Assembly.GetName().Version);
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: crank [url] [numclients]");
                return;
            }

            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;

            string url = args[0];
            int clients = Int32.Parse(args[1]);

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var connections = new ConcurrentBag<Connection>();

            var sw = Stopwatch.StartNew();

            Task.Factory.StartNew(() =>
            {
                Parallel.For(0, clients, i =>
                {
                    try
                    {
                        var connection = new Connection(url);
                        connection.Received += data =>
                        {
                            Console.WriteLine(data);
                        };

                        connection.Error += e =>
                        {
                            Console.WriteLine("ERROR: Client {0}, {1}", i, e.GetBaseException());
                        };

                        connection.Closed += () =>
                        {
                            Console.WriteLine("CLOSED: {0}", i);
                        };

                        connection.Start().Wait();
                        connections.Add(connection);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to start client {0}. {1}", i, e);
                    }
                });
                Console.WriteLine("Started {0} connection(s).", connections.Count);
            });

            Console.WriteLine("Press any key to stop running...");
            Console.Read();
            sw.Stop();

            Console.WriteLine("Total Running time: {0}s", sw.Elapsed);
            Console.WriteLine("End point: {0}", url);
            Console.WriteLine("Total connections: {0}", clients);
            Console.WriteLine("Active connections: {0}", connections.Count(c => c.IsActive));
            Console.WriteLine("Stopped connections: {0}", connections.Count(c => !c.IsActive));
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }
    }
}
