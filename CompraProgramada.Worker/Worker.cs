using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                GroupId = "compra-programada-worker",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            var topicos = new[] { "ir-dedo-duro", "ir-venda" };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(topicos);

            _logger.LogInformation("Worker Kafka iniciado. Consumindo topicos: {Topicos}", string.Join(", ", topicos));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (result != null)
                    {
                        _logger.LogInformation(
                            "Mensagem recebida | Topico: {Topico} | Partition: {Partition} | Offset: {Offset} | Payload: {Payload}",
                            result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Value);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Erro ao consumir mensagem do Kafka");
                }
            }

            consumer.Close();
            _logger.LogInformation("Worker Kafka encerrado.");
        }
    }
}