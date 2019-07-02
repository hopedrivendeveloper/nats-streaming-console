using System;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using STAN.Client;

namespace Nats.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = Middleware.ConfigureApp();
            
            var config = services.GetService<IOptions<AppConfig>>();
            
            var conn = services.GetService<INatsConnectionProvider>();

            var opts = StanSubscriptionOptions.GetDefaultOptions();
            opts.DurableName = $"{config.Value.App.AppName}.Durable";
            
            EventHandler<StanMsgHandlerArgs> eventHandler = async (sender, handlerArgs) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(handlerArgs.Message.Data);
                Console.WriteLine($"Received a message: {message}");
            };
            
            var topicName = "bukapor.nats.topic.name";
            using (var connection = conn.GetIStanConnection())
            {
                var ev = new AutoResetEvent(false);
                using (var s = connection.Subscribe(topicName, opts, eventHandler))
                {
                    Console.WriteLine($"Connected to nats {config.Value.NatsConnection.ClusterId} running on {config.Value.NatsConnection.ConnectionUrl}."
                        );
                    Log.Information($"Waiting on messages from {topicName}.");
                    for (int i = 0; i < 100; i++)
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                        connection.Publish(topicName, Encoding.UTF8.GetBytes(i.ToString()));
                    }
                    ev.WaitOne();
                }
                
            }
        }
    }
    
    public interface INatsConnectionProvider
    {
        IStanConnection GetIStanConnection();
    }

    public class NatsConnectionProvider : INatsConnectionProvider
    {
        private readonly StanConnectionFactory _connectionFactory;
        private readonly IOptions<AppConfig> _appConfig;

        public NatsConnectionProvider(StanConnectionFactory connectionFactory, IOptions<AppConfig> appConfig)
        {
            _connectionFactory = connectionFactory;
            _appConfig = appConfig;
        }

        public IStanConnection GetIStanConnection()
        {
            var options = StanOptions.GetDefaultOptions();
            options.NatsURL = _appConfig.Value.NatsConnection.ConnectionUrl;

            return _connectionFactory.CreateConnection(_appConfig.Value.NatsConnection.ClusterId, _appConfig.Value.App.AppName, options);
        }
    }

    public class Middleware
    {
        public static IServiceProvider ConfigureApp()
        {
            Console.WriteLine("Started Configuration...");
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .Build();


            Console.WriteLine("Started Services Configuration...");
            var container = new ServiceCollection()
                .Configure<AppConfig>(configuration)
                .AddOptions()
                .AddScoped<StanConnectionFactory>()
                .AddScoped<INatsConnectionProvider, NatsConnectionProvider>()
                .BuildServiceProvider();

            Console.WriteLine("Configuration finished.");
            return container;
        }
    }

    public class AppConfig
    {
        public AppSettings App { get; set; }
        public NatsConnectionSettings NatsConnection { get; set; }
    }

    public class AppSettings
    {
        public string AppName { get; set; }
    }

    public class NatsConnectionSettings
    {
        public string ConnectionUrl { get; set; }
        public string ClusterId { get; set; }
    }
    
}