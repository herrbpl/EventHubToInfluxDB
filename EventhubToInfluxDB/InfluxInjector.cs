using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace EventhubToInfluxDB
{
    public class InfluxOptions
    {
        public string Url { get; set; }
        public bool NoSSLValidation { get; set; } = true;
        public int TimeOut { get; set; } = 30;
        public bool UseBasicAuth { get; set; } = false;
        public string Username { get; set; } = null;
        public string Password { get; set; } = null;        
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
            
            if (_options.NoSSLValidation)
            {
                _handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    _logger.LogDebug($"Server SSL CERT: {cert.ToString()}");
                    return true;

                };
            }
            

            

        }

        public async Task InjectAsync(string payload)
        {
            if (_client == null)
            {
                _logger.LogDebug($"Creating HttpClient");

                _client = new HttpClient(_handler);

                if (_options.TimeOut > 0) _client.Timeout = TimeSpan.FromSeconds(_options.TimeOut);
                if (_options.UseBasicAuth && _options.Username != null && _options.Password != null)
                {
                    var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _options.Username, _options.Password));
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }

            }

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Content = new StringContent(payload);

            httpRequestMessage.Method = new HttpMethod("POST");
            UriBuilder ub = new UriBuilder(_options.Url);            

            httpRequestMessage.RequestUri = ub.Uri;

            HttpResponseMessage response = await _client.SendAsync(httpRequestMessage);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Unsuccessful call to server: {response.StatusCode}: {response.ReasonPhrase}");
            }

        }
    }
}
