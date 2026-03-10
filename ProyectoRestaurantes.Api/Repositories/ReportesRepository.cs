using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.DTOs.Responses;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Repositories;

public class ReportesRepository
{
    private readonly IMongoCollection<Resena> _resenas;
    private readonly IMongoCollection<Pedido> _pedidos;

    public ReportesRepository(IMongoDatabase database)
    {
        _resenas = database.GetCollection<Resena>("resenas");
        _pedidos = database.GetCollection<Pedido>("pedidos");
    }

    public async Task<List<TopRestauranteResponse>> ObtenerTopRestaurantesAsync(int limite = 5)
    {
        // Definimos el pipeline etapa por etapa, tal como pide el documento
        var pipeline = new[]
        {
            // Agrupar por restaurante_id para calcular promedio y contar reseñas
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$restaurante_id" },
                { "Promedio", new BsonDocument("$avg", "$calificacion") },
                { "TotalResenas", new BsonDocument("$sum", 1) }
            }),
            
            // Ordenar de mayor a menor según el promedio
            new BsonDocument("$sort", new BsonDocument("Promedio", -1)),
            
            // Limitar a los mejores N resultados
            new BsonDocument("$limit", limite),
            
            // Lookup (JOIN) hacia la colección 'restaurantes' para traer nombre y categoría
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "restaurantes" },
                { "localField", "_id" },
                { "foreignField", "_id" },
                { "as", "DatosRestaurante" }
            }),
            
            // Unwind para aplanar el arreglo generado por el lookup
            new BsonDocument("$unwind", "$DatosRestaurante"),
            
            // Proyectar la salida para que coincida exactamente con nuestro DTO
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "RestauranteId", new BsonDocument("$toString", "$_id") },
                { "Nombre", "$DatosRestaurante.nombre" },
                { "Categoria", "$DatosRestaurante.categoria" },
                { "Promedio", 1 },
                { "TotalResenas", 1 }
            })
        };

        // Ejecutamos el pipeline y deserializamos el resultado BSON a nuestra clase C#
        var resultadosBson = await _resenas.Aggregate<BsonDocument>(pipeline).ToListAsync();
        
        return resultadosBson.Select(bson => BsonSerializer.Deserialize<TopRestauranteResponse>(bson)).ToList();
    }

    public async Task<List<PlatilloMasVendidoResponse>> ObtenerPlatillosMasVendidosAsync(int limite = 5)
    {
        var pipeline = new[]
        {
            // Unwind: Separa el array de "items" en documentos individuales
            new BsonDocument("$unwind", "$items"),
            
            // Group: Agrupamos por el ID del artículo dentro del item
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$items.articulo_id" },
                { "CantidadVendida", new BsonDocument("$sum", "$items.cantidad") }, // Sumamos las cantidades
                // Tomamos el nombre directamente del snapshot del pedido, ¡sin hacer Lookups costosos!
                { "Nombre", new BsonDocument("$first", "$items.nombre_copia") } 
            }),
            
            // Sort: Ordenamos por los más vendidos de mayor a menor
            new BsonDocument("$sort", new BsonDocument("CantidadVendida", -1)),
            
            // Limit: Traemos solo el Top N solicitados
            new BsonDocument("$limit", limite),
            
            // Project: Estructuramos para que haga match con nuestro DTO
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "ArticuloId", new BsonDocument("$toString", "$_id") },
                { "Nombre", 1 },
                { "CantidadVendida", 1 }
            })
        };

        var resultadosBson = await _pedidos.Aggregate<BsonDocument>(pipeline).ToListAsync();
        
        return resultadosBson.Select(bson => BsonSerializer.Deserialize<PlatilloMasVendidoResponse>(bson)).ToList();
    }
}