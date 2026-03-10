using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Repositories;

public class PedidoRepository
{
    private readonly IMongoCollection<Pedido> _pedidos;

    public PedidoRepository(IMongoDatabase database)
    {
        _pedidos = database.GetCollection<Pedido>("pedidos");
    }

    public async Task<(List<BsonDocument> Items, long Total)> ObtenerAsync(
        string? usuarioId = null,
        string? restauranteId = null,
        string? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        int skip = 0,
        int limit = 20)
    {
        var matchConditions = new BsonDocument();

        if (!string.IsNullOrWhiteSpace(usuarioId))
            matchConditions["usuario_id"] = new BsonObjectId(ObjectId.Parse(usuarioId));

        if (!string.IsNullOrWhiteSpace(restauranteId))
            matchConditions["restaurante_id"] = new BsonObjectId(ObjectId.Parse(restauranteId));

        if (!string.IsNullOrWhiteSpace(estado))
            matchConditions["estado"] = estado;

        if (desde.HasValue || hasta.HasValue)
        {
            var dateFilter = new BsonDocument();
            if (desde.HasValue) dateFilter["$gte"] = desde.Value;
            if (hasta.HasValue) dateFilter["$lte"] = hasta.Value;
            matchConditions["fecha_creacion"] = dateFilter;
        }

        var pipeline = new List<BsonDocument>
        {
            new("$match", matchConditions),
            new("$sort", new BsonDocument("fecha_creacion", -1)),
            new("$lookup", new BsonDocument
            {
                { "from", "usuarios" },
                { "localField", "usuario_id" },
                { "foreignField", "_id" },
                { "as", "usuario" },
                { "pipeline", new BsonArray {
                    new BsonDocument("$project", new BsonDocument { { "nombre", 1 }, { "email", 1 } })
                }}
            }),
            new("$lookup", new BsonDocument
            {
                { "from", "restaurantes" },
                { "localField", "restaurante_id" },
                { "foreignField", "_id" },
                { "as", "restaurante" },
                { "pipeline", new BsonArray {
                    new BsonDocument("$project", new BsonDocument { { "nombre", 1 } })
                }}
            }),
            new("$unwind", new BsonDocument { { "path", "$usuario" }, { "preserveNullAndEmptyArrays", true } }),
            new("$unwind", new BsonDocument { { "path", "$restaurante" }, { "preserveNullAndEmptyArrays", true } }),
        };

        var filterForCount = matchConditions.ElementCount == 0
            ? Builders<Pedido>.Filter.Empty
            : new MongoDB.Driver.BsonDocumentFilterDefinition<Pedido>(matchConditions);

        var total = await _pedidos.CountDocumentsAsync(filterForCount);

        var paginatedPipeline = new List<BsonDocument>(pipeline)
        {
            new("$skip", skip),
            new("$limit", limit)
        };

        var items = await _pedidos.Aggregate<BsonDocument>(paginatedPipeline).ToListAsync();
        return (items, total);
    }

    public async Task<Pedido?> ObtenerPorIdAsync(string id) =>
        await _pedidos.Find(p => p.Id == id).FirstOrDefaultAsync();

    // Actualizar estado de muchos pedidos a la vez (UpdateMany)
    public async Task<long> ActualizarEstadoMultiplesAsync(List<string> ids, string nuevoEstado)
    {
        var filtro = Builders<Pedido>.Filter.In(p => p.Id, ids);
        var update = Builders<Pedido>.Update.Set(p => p.Estado, nuevoEstado);
        var result = await _pedidos.UpdateManyAsync(filtro, update);
        return result.ModifiedCount;
    }
}
