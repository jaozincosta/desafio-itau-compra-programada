using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers
{
    [ApiController]
    [Route("api/motor")]
    public class MotorCompraController : ControllerBase
    {
        private readonly IMotorCompraService _motorCompraService;

        public MotorCompraController(IMotorCompraService motorCompraService)
        {
            _motorCompraService = motorCompraService;
        }

        [HttpPost("executar-compra")]
        public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest request)
        {
            try
            {
                var resultado = await _motorCompraService.ExecutarCompraAsync(request.DataReferencia);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ErroResponse(ex.Message, ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
        }
    }
}
