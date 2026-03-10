using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidoService;

    // Inyectamos nuestro servicio transaccional
    public PedidosController(PedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPost]
    public async Task<IActionResult> CrearPedido([FromBody] Pedido nuevoPedido)
    {
        try
        {
            // Validamos que el JSON no venga vacío
            if (nuevoPedido == null || !nuevoPedido.Items.Any())
            {
                return BadRequest(new { mensaje = "El pedido debe contener al menos un artículo." });
            }

            // Transacción ACID
            var pedidoCreado = await _pedidoService.CrearPedidoTransaccionalAsync(nuevoPedido);
            
            // Retornamos un 201 Created con el objeto resultante y su nuevo ID
            return CreatedAtAction(nameof(CrearPedido), new { id = pedidoCreado.Id }, pedidoCreado);
        }
        catch (Exception ex)
        {
            // Si la transacción falla
            // Atrapamos el error y le decimos al 
            // cliente exactamente qué pasó.
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> CancelarPedido(string id)
    {
        try
        {
            // Llamamos a la transacción
            await _pedidoService.CancelarPedidoTransaccionalAsync(id);
            
            return Ok(new { mensaje = $"El pedido {id} ha sido cancelado y el inventario fue restaurado exitosamente." });
        }
        catch (Exception ex)
        {
            // Devolvemos el error específico (ej. "El pedido ya fue entregado")
            return BadRequest(new { error = ex.Message });
        }
    }
}