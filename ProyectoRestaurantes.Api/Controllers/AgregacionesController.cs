using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgregacionesController : ControllerBase
{
    private readonly IMongoDatabase _database;

    public AgregacionesController(IMongoDatabase database)
    {
        _database = database;
    }

    // ========== AGREGACIONES SIMPLES: DISTINCT ==========

    [HttpGet("categorias-restaurantes")]
    public async Task<IActionResult> ObtenerCategoriasRestaurantes()
    {
        var collection = _database.GetCollection<Restaurante>("restaurantes");
        var categorias = await collection.Distinct<string>("categoria", Builders<Restaurante>.Filter.Empty).ToListAsync();

        return Ok(new
        {
            total = categorias.Count,
            categorias = categorias.OrderBy(c => c).ToList()
        });
    }

    [HttpGet("estados-pedidos")]
    public async Task<IActionResult> ObtenerEstadosPedidos()
    {
        var collection = _database.GetCollection<Pedido>("pedidos");
        var estados = await collection.Distinct<string>("estado", Builders<Pedido>.Filter.Empty).ToListAsync();

        return Ok(new
        {
            total = estados.Count,
            estados = estados.OrderBy(e => e).ToList()
        });
    }

    [HttpGet("categorias-articulos")]
    public async Task<IActionResult> ObtenerCategoriasArticulos()
    {
        var collection = _database.GetCollection<Articulo>("articulos");
        var categorias = await collection.Distinct<string>("categorias", Builders<Articulo>.Filter.Empty).ToListAsync();

        return Ok(new
        {
            total = categorias.Count,
            categorias = categorias.OrderBy(c => c).ToList()
        });
    }

    [HttpGet("ingredientes")]
    public async Task<IActionResult> ObtenerIngredientesUnicos()
    {
        var collection = _database.GetCollection<Articulo>("articulos");
        var ingredientes = await collection.Distinct<string>("ingredientes", Builders<Articulo>.Filter.Empty).ToListAsync();

        return Ok(new
        {
            total = ingredientes.Count,
            ingredientes = ingredientes.OrderBy(i => i).ToList()
        });
    }

    // ========== AGREGACIONES SIMPLES: COUNT ==========

    [HttpGet("estadisticas/generales")]
    public async Task<IActionResult> ObtenerEstadisticasGenerales()
    {
        var totalUsuarios = await _database.GetCollection<Usuario>("usuarios")
            .CountDocumentsAsync(Builders<Usuario>.Filter.Empty);

        var totalRestaurantes = await _database.GetCollection<Restaurante>("restaurantes")
            .CountDocumentsAsync(Builders<Restaurante>.Filter.Empty);

        var totalArticulos = await _database.GetCollection<Articulo>("articulos")
            .CountDocumentsAsync(Builders<Articulo>.Filter.Empty);

        var totalPedidos = await _database.GetCollection<Pedido>("pedidos")
            .CountDocumentsAsync(Builders<Pedido>.Filter.Empty);

        var totalResenas = await _database.GetCollection<Resena>("resenas")
            .CountDocumentsAsync(Builders<Resena>.Filter.Empty);

        return Ok(new
        {
            usuarios = totalUsuarios,
            restaurantes = totalRestaurantes,
            articulos = totalArticulos,
            pedidos = totalPedidos,
            resenas = totalResenas
        });
    }

    [HttpGet("estadisticas/pedidos-por-estado")]
    public async Task<IActionResult> ObtenerPedidosPorEstado()
    {
        var collection = _database.GetCollection<Pedido>("pedidos");
        var estados = new[] { "RECIBIDO", "PREPARANDO", "EN_CAMINO", "ENTREGADO", "CANCELADO" };
        var resultados = new Dictionary<string, long>();

        foreach (var estado in estados)
        {
            var count = await collection.CountDocumentsAsync(p => p.Estado == estado);
            resultados[estado] = count;
        }

        return Ok(resultados);
    }

    [HttpGet("estadisticas/restaurantes-activos")]
    public async Task<IActionResult> ObtenerRestaurantesActivos()
    {
        var collection = _database.GetCollection<Restaurante>("restaurantes");

        var activos = await collection.CountDocumentsAsync(r => r.Activo == true);
        var inactivos = await collection.CountDocumentsAsync(r => r.Activo == false);

        return Ok(new
        {
            activos,
            inactivos,
            total = activos + inactivos
        });
    }

    [HttpGet("estadisticas/articulos-disponibles")]
    public async Task<IActionResult> ObtenerArticulosDisponibles()
    {
        var collection = _database.GetCollection<Articulo>("articulos");

        var disponibles = await collection.CountDocumentsAsync(a => a.Disponible == true);
        var noDisponibles = await collection.CountDocumentsAsync(a => a.Disponible == false);

        return Ok(new
        {
            disponibles,
            noDisponibles,
            total = disponibles + noDisponibles
        });
    }

    // ========== AGREGACIONES: SUM Y AVG ==========

    [HttpGet("estadisticas/ventas-totales")]
    public async Task<IActionResult> ObtenerVentasTotales()
    {
        var collection = _database.GetCollection<Pedido>("pedidos");

        var pipeline = new[]
        {
            new MongoDB.Bson.BsonDocument("$match", new MongoDB.Bson.BsonDocument("estado", "ENTREGADO")),
            new MongoDB.Bson.BsonDocument("$group", new MongoDB.Bson.BsonDocument
            {
                { "_id", MongoDB.Bson.BsonNull.Value },
                { "totalVentas", new MongoDB.Bson.BsonDocument("$sum", "$total_pagar") },
                { "promedioVenta", new MongoDB.Bson.BsonDocument("$avg", "$total_pagar") },
                { "totalPedidos", new MongoDB.Bson.BsonDocument("$sum", 1) }
            })
        };

        var resultado = await collection.Aggregate<MongoDB.Bson.BsonDocument>(pipeline).FirstOrDefaultAsync();

        if (resultado == null)
        {
            return Ok(new
            {
                totalVentas = 0,
                promedioVenta = 0,
                totalPedidos = 0
            });
        }

        return Ok(new
        {
            totalVentas = Math.Round(resultado["totalVentas"].ToDouble(), 2),
            promedioVenta = Math.Round(resultado["promedioVenta"].ToDouble(), 2),
            totalPedidos = resultado["totalPedidos"].ToInt32()
        });
    }

    [HttpGet("estadisticas/calificacion-promedio-general")]
    public async Task<IActionResult> ObtenerCalificacionPromedioGeneral()
    {
        var collection = _database.GetCollection<Resena>("resenas");

        var pipeline = new[]
        {
            new MongoDB.Bson.BsonDocument("$group", new MongoDB.Bson.BsonDocument
            {
                { "_id", MongoDB.Bson.BsonNull.Value },
                { "promedioGeneral", new MongoDB.Bson.BsonDocument("$avg", "$calificacion") },
                { "totalResenas", new MongoDB.Bson.BsonDocument("$sum", 1) },
                { "mejorCalificacion", new MongoDB.Bson.BsonDocument("$max", "$calificacion") },
                { "peorCalificacion", new MongoDB.Bson.BsonDocument("$min", "$calificacion") }
            })
        };

        var resultado = await collection.Aggregate<MongoDB.Bson.BsonDocument>(pipeline).FirstOrDefaultAsync();

        if (resultado == null)
        {
            return Ok(new
            {
                promedioGeneral = 0,
                totalResenas = 0,
                mejorCalificacion = 0,
                peorCalificacion = 0
            });
        }

        return Ok(new
        {
            promedioGeneral = Math.Round(resultado["promedioGeneral"].ToDouble(), 2),
            totalResenas = resultado["totalResenas"].ToInt32(),
            mejorCalificacion = resultado["mejorCalificacion"].ToInt32(),
            peorCalificacion = resultado["peorCalificacion"].ToInt32()
        });
    }
}
