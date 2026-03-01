using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Application.DTOs;

namespace CompraProgramada.Application.Interfaces
{
    public interface ICestaService
    {
        Task<CestaResponse> CadastrarOuAlterarAsync(CestaRequest request);
        Task<CestaAtualResponse> ObterAtualAsync();
        Task<HistoricoCestasResponse> ObterHistoricoAsync();
    }
}