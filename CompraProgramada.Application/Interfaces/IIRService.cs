using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.Interfaces
{
    public interface IIRService
    {
        decimal CalcularIRDedoDuro(decimal valorOperacao);
        Task<decimal> CalcularIRVendaAsync(long clienteId, int mesReferencia, int anoReferencia);
    }
}