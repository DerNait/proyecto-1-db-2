using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Repositories;
using ProyectoRestaurantes.Api.Services;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly PedidoService _pedidoService;
    private readonly PedidoRepository _pedidoRepository;

    public PedidosController(PedidoService pedidoService, PedidoRepository pedidoRepository)
    {
        _pedidoService = pedidoService;
        _pedidoRepository = pedidoRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? usuarioId,
        [FromQuery] string? restauranteId,
        [FromQuery] string? estado,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20)
    {
        var (items, total) = await _pedidoRepository.ObtenerAsync(usuarioId, restauranteId, estado, desde, hasta, skip, limit);
        var resultItems = items.Select(d => new {
            _id = d.GetValue("_id", "").ToString(),
            usuario_id = d.GetValue("usuario_id", "").ToString(),
            restaurante_id = d.GetValue("restaurante_id", "").ToString(),
            estado = d.GetValue("estado", "").ToString(),
            totalPagar = d.GetValue("total_pagar", 0).ToDouble(),
            fechaCreacion = d.GetValue("fecha_creacion", DateTime.UtcNow).ToUniversalTime(),
            usuario = d.Contains("usuario") && d["usuario"].IsBsonDocument ? new {
                nombre = d["usuario"].AsBsonDocument.GetValue("nombre", "").ToString(),
                email = d["usuario"].AsBsonDocument.GetValue("email", "").ToString()
            } : null,
            restaurante = d.Contains("restaurante") && d["restaurante"].IsBsonDocument ? new {
                nombre = d["restaurante"].AsBsonDocument.GetValue("nombre", "").ToString()
            } : null
        });
        return Ok(new { total, items = resultItems });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(string id)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    [HttpGet("entregados/usuario/{usuarioId}")]
    public async Task<IActionResult> ObtenerPedidosEntregados(string usuarioId)
    {
        var (items, total) = await _pedidoRepository.ObtenerAsync(
            usuarioId: usuarioId,
            estado: "ENTREGADO",
            skip: 0,
            limit: 100
        );

        var resultItems = items.Select(d => new {
            _id = d.GetValue("_id", "").ToString(),
            usuario_id = d.GetValue("usuario_id", "").ToString(),
            restaurante_id = d.GetValue("restaurante_id", "").ToString(),
            estado = d.GetValue("estado", "").ToString(),
            totalPagar = d.GetValue("total_pagar", 0).ToDouble(),
            fechaCreacion = d.GetValue("fecha_creacion", DateTime.UtcNow).ToUniversalTime(),
            usuario = d.Contains("usuario") && d["usuario"].IsBsonDocument ? new {
                nombre = d["usuario"].AsBsonDocument.GetValue("nombre", "").ToString(),
                email = d["usuario"].AsBsonDocument.GetValue("email", "").ToString()
            } : null,
            restaurante = d.Contains("restaurante") && d["restaurante"].IsBsonDocument ? new {
                nombre = d["restaurante"].AsBsonDocument.GetValue("nombre", "").ToString()
            } : null
        });

        return Ok(new { total, items = resultItems });
    }

    [HttpPost]
    public async Task<IActionResult> CrearPedido([FromBody] Pedido nuevoPedido)
    {
        try
        {
            if (nuevoPedido == null || !nuevoPedido.Items.Any())
                return BadRequest(new { mensaje = "El pedido debe contener al menos un artículo." });

            var pedidoCreado = await _pedidoService.CrearPedidoTransaccionalAsync(nuevoPedido);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = pedidoCreado.Id }, pedidoCreado);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> CancelarPedido(string id)
    {
        try
        {
            await _pedidoService.CancelarPedidoTransaccionalAsync(id);
            return Ok(new { mensaje = $"El pedido {id} ha sido cancelado y el inventario fue restaurado." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(string id, [FromBody] ActualizarEstadoRequest req)
    {
        try
        {
            await _pedidoRepository.ActualizarEstadoAsync(id, req.Estado);
            return Ok(new { mensaje = $"Pedido actualizado a '{req.Estado}'." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Actualizar estado de muchos pedidos (UpdateMany)
    [HttpPut("bulk/estado")]
    public async Task<IActionResult> ActualizarEstadoBulk([FromBody] BulkEstadoRequest req)
    {
        var modificados = await _pedidoRepository.ActualizarEstadoMultiplesAsync(req.Ids, req.Estado);
        return Ok(new { mensaje = $"{modificados} pedido(s) actualizados a estado '{req.Estado}'." });
    }
}

public record BulkEstadoRequest(List<string> Ids, string Estado);
public record ActualizarEstadoRequest(string Estado);