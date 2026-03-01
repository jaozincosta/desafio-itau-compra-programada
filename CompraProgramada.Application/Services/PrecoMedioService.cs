using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompraProgramada.Application.Interfaces;

namespace CompraProgramada.Application.Services
{
    public class PrecoMedioService : IPrecoMedioService
    {
        //RN-042: PM = (Qtd Anterior x PM Anterior + Qtd Nova x Preco Nova) / (Qtd Anterior + Qtd Nova)
        //RN-043: Venda NAO altera preco medio
        //RN-044: Preco medio so e recalculado em compras

        public decimal CalcularPrecoMedio(int quantidadeAnterior, decimal precoMedioAnterior, int quantidadeNova, decimal precoNovo)
        {
            if (quantidadeNova <= 0)
            {
                throw new ArgumentException("A quantidade nova deve ser maior que zero.");
            }

            if (precoNovo <= 0) 
            {
                throw new ArgumentException("O Preço da nova compra deve ser maior que zero.");
            }

            //Primeira compra
            if (quantidadeAnterior <= 0)
            {
                return Math.Round(precoNovo, 4);
            }

            var totalAnterior = quantidadeAnterior * precoMedioAnterior;
            var totalNovo = quantidadeNova * precoNovo;
            var quantidadeTotal = quantidadeAnterior + quantidadeNova;

            return Math.Round((totalAnterior + totalNovo) / quantidadeTotal, 4);
        }
    }
}
