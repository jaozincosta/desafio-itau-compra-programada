using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers
{
    [ApiController]
    [Route("api/admin/cesta")]
    public class AdminCestaController : ControllerBase
    {
        private readonly ICestaService _cestaService;

        public AdminCestaController(ICestaService cestaService)
        {
            _cestaService = cestaService;
        }

        [HttpPost]
        public async Task<IActionResult> CadastrarOuAlterar([FromBody] CestaRequest request)
        {
            try
            {
                var resultado = await _cestaService.CadastrarOuAlterarAsync(request);
                return Created("/api/admin/cesta/atual", resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErroResponse(ex.Message, ex.Message));
            }
        }

        [HttpGet("atual")]
        public async Task<IActionResult> ObterAtual()
        {
            try
            {
                var resultado = await _cestaService.ObterAtualAsync();
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
        }

        [HttpGet("historico")]
        public async Task<IActionResult> ObterHistorico()
        {
            var resultado = await _cestaService.ObterHistoricoAsync();
            return Ok(resultado);
        }
    }
}