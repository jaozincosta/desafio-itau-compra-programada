using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.Interfaces
{
    public interface IRebalanceamentoService
    {
        Task RebalancearPorMudancaCestaAsync(long cestaAntigaId, long cestaNovaId);
        Task RebalancearPorDesvioAsync(long clienteId, decimal limiarDesvio = 5.0m);
    }
}
