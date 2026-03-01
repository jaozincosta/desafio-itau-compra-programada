using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompraProgramada.Application.Interfaces
{
    public interface IPrecoMedioService
    {
        decimal CalcularPrecoMedio(int quantidadeAnterior, decimal precoMedioAnterior, int quantidadeNova, decimal precoNovo);
    }
}