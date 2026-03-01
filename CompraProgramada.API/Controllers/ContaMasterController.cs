using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers
{
    [ApiController]
    [Route("api/admin/conta-master")]
    public class ContaMasterController : ControllerBase
    {
        private readonly IContaMasterService _contaMasterService;

        public ContaMasterController(IContaMasterService contaMasterService)
        {
            _contaMasterService = contaMasterService;
        }

        [HttpGet("custodia")]
        public async Task<IActionResult> ConsultarCustodia()
        {
            try
            {
                var resultado = await _contaMasterService.ConsultarCustodiaAsync();
                return Ok(resultado);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ErroResponse(ex.Message, ex.Message));
            }
        }
    }
}