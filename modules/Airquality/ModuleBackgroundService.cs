using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Airquality
{
    internal class ModuleBackgroundService : BackgroundService
    {
        private ModuleClient? _moduleClient;
        private readonly ILogger<ModuleBackgroundService> _logger;

        public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // 设置MQTT TCP传输
            var mqttSettings = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSettings };

            // 连接到 IoT Edge Runtime
            _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            _moduleClient.SetConnectionStatusChangesHandler((status, reason) =>
                _logger.LogWarning("Connection changed: Status: {status} Reason: {reason}", status, reason));

            await _moduleClient.OpenAsync(cancellationToken);
            _logger.LogInformation("IoT Hub module client initialized.");

            // 循环发送模拟空气质量数据
            var rnd = new Random();

            while (!cancellationToken.IsCancellationRequested)
            {
                double airQualityValue = Math.Round(rnd.NextDouble() * 500, 2); // 0~500的随机空气质量值
                var messageBody = $"{{ \"airQuality\": {airQualityValue}, \"timestamp\": \"{DateTime.UtcNow:o}\" }}";

                var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                await _moduleClient.SendEventAsync("output1", message, cancellationToken);

                _logger.LogInformation("Sent air quality message: {messageBody}", messageBody);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken); // 每5秒发送一次
            }
        }
    }
}
