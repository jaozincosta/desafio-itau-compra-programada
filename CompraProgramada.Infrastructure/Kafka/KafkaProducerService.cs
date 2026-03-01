using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using CompraProgramada.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Kafka
{
    public class KafkaProducerService : IKafkaProducer, IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000
            };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task PublicarAsync(string topico, string mensagem)
        {
            try
            {
                var result = await _producer.ProduceAsync(topico, new Message<Null, string> 
                { 
                    Value = mensagem 
                });

                _logger.LogInformation(
                    "Mensagem publicada no topo {Topico} | Partition: {Partition} | Offset: {Offset}",
                    topico, result.Partition.Value, result.Offset.Value);
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem no tópico {Topico}", topico);
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}
