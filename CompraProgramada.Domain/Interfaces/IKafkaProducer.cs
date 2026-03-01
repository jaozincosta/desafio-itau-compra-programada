using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Domain.Interfaces
{
    public interface IKafkaProducer
    {
        Task PublicarAsync(string topico, string mensagem);
    }
}
