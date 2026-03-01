using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpPost("adesao")]
        public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request)
        {
            try
            {
                var resultado = await _clienteService.AderirAsync(request);
                return Created($"/api/clientes/{resultado.ClienteId}/carteira", resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErroResponse(ex.Message, ex.Message));
            }
        }

        [HttpPost("{id}/saida")]
        public async Task<IActionResult> Sair(long id)
        {
            try
            {
                var resultado = await _clienteService.SairAsync(id);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErroResponse(ex.Message, ex.Message));
            }
        }

        [HttpPut("{id}/valor-mensal")]
        public async Task<IActionResult> AlterarValorMensal(long id, [FromBody] AlterarValorMensalRequest request)
        {
            try
            {
                var resultado = await _clienteService.AlterarValorMensalAsync(id, request);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErroResponse(ex.Message, ex.Message));
            }
        }

        [HttpGet("{id}/carteira")]
        public async Task<IActionResult> ConsultarCarteira(long id)
        {
            try
            {
                var resultado = await _clienteService.ConsultarCarteiraAsync(id);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
        }

        [HttpGet("{id}/rentabilidade")]
        public async Task<IActionResult> ConsultarRentabilidade(long id)
        {
            try
            {
                var resultado = await _clienteService.ConsultarRentabilidadeAsync(id);
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
        }
    }
}