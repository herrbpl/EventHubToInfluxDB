using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace EventhubToInfluxDB
{
    public class InfluxOptions
    {
        public string Url { get; set; }
    }

    public class InfluxInjector
    {
        protected HttpClientHandler _handler = null;
        protected HttpClient _client = null;
        protected ILogger _logger;
        protected InfluxOptions _options;
        public InfluxInjector(ILogger<InfluxInjector> logger, IOptionsMonitor<InfluxOptions> options)
        {
            _options = options.CurrentValue;
            _handler = new HttpClientHandler();
            _handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            _logger = logger;
            /*
            IOptionsMonitor<InfluxOptions>
            // TODO: implement valid SSL cert checking
            if (_options.NoSSLValidation)
            {
                _handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    _logger.Debug($"Server SSL CERT: {cert.ToString()}", () => { });
                    return true;

                };
            }
            */
            Console.WriteLine($"{_options.Url}");
        }

        public async Task InjectAsync(string payload)
        {
            Console.WriteLine("Injecting call");
            _logger.LogDebug($"Injecting {payload}...");

        }
    }
}
