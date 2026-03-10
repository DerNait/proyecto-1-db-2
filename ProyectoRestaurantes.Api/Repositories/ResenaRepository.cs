using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Repositories;

public class ResenaRepository
{
    private readonly IMongoCollection<Resena> _resenas;
    private readonly IMongoCollection<Restaurante> _restaurantes;

    public ResenaRepository(IMongoDatabase database)
    {
        _resenas = database.GetCollection<Resena>("resenas");
        _restaurantes = database.GetCollection<Restaurante>("restaurantes");
    }

    public async Task<(List<BsonDocument> Items, long Total)> ObtenerAsync(
        string? restauranteId = null,
        string? usuarioId = null,
        int? calificacionMin = null,
        string sortPor = "fecha",
        int sortDir = -1,
        int skip = 0,
        int limit = 20)
    {
        var matchConditions = new BsonDocument();

        if (!string.IsNullOrWhiteSpace(restauranteId))
            matchConditions["restaurante_id"] = new BsonObjectId(ObjectId.Parse(restauranteId));

        if (!string.IsNullOrWhiteSpace(usuarioId))
            matchConditions["usuario_id"] = new BsonObjectId(ObjectId.Parse(usuarioId));

        if (calificacionMin.HasValue && calificacionMin.Value > 0)
            matchConditions["calificacion"] = new BsonDocument("$gte", calificacionMin.Value);

        // Pipeline con $lookup multi-colección hacia usuarios y restaurantes
        var pipeline = new List<BsonDocument>
        {
            new("$match", matchConditions),
            new("$sort", new BsonDocument(sortPor, sortDir)),
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
                    new BsonDocument("$project", new BsonDocument { { "nombre", 1 }, { "categoria", 1 } })
                }}
            }),
            new("$unwind", new BsonDocument { { "path", "$usuario" }, { "preserveNullAndEmptyArrays", true } }),
            new("$unwind", new BsonDocument { { "path", "$restaurante" }, { "preserveNullAndEmptyArrays", true } }),
        };

        var totalFilter = matchConditions.ElementCount == 0
            ? Builders<Resena>.Filter.Empty
            : (FilterDefinition<Resena>)new BsonDocumentFilterDefinition<Resena>(matchConditions);

        var total = await _resenas.CountDocumentsAsync(totalFilter);

        var paginatedPipeline = new List<BsonDocument>(pipeline)
        {
            new("$skip", skip),
            new("$limit", limit)
        };

        var items = await _resenas.Aggregate<BsonDocument>(paginatedPipeline).ToListAsync();
        return (items, total);
    }

    public async Task<Resena?> ObtenerPorIdAsync(string id) =>
        await _resenas.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<Resena> CrearAsync(Resena resena)
    {
        resena.Fecha = DateTime.UtcNow;
        await _resenas.InsertOneAsync(resena);

        // Actualizar el rating promedio del restaurante
        await RecalcularRatingAsync(resena.RestauranteId);
        return resena;
    }

    public async Task<bool> EliminarAsync(string id)
    {
        var resena = await ObtenerPorIdAsync(id);
        if (resena == null) return false;

        var result = await _resenas.DeleteOneAsync(r => r.Id == id);
        if (result.DeletedCount > 0)
            await RecalcularRatingAsync(resena.RestauranteId);

        return result.DeletedCount > 0;
    }

    // Agregación: recalcular y persistir el rating promedio del restaurante
    private async Task RecalcularRatingAsync(string restauranteId)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("restaurante_id", new BsonObjectId(ObjectId.Parse(restauranteId)))),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$restaurante_id" },
                { "promedio", new BsonDocument("$avg", "$calificacion") }
            })
        };

        var resultado = await _resenas.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        var promedio = resultado != null ? resultado["promedio"].AsDouble : 0;

        var update = Builders<Restaurante>.Update.Set(r => r.RatingPromedio, Math.Round(promedio, 2));
        var restaurantes = _resenas.Database.GetCollection<Restaurante>("restaurantes");
        await restaurantes.UpdateOneAsync(r => r.Id == restauranteId, update);
    }

    // Agregación simple: estadísticas de reseñas por restaurante
    public async Task<List<BsonDocument>> ObtenerEstadisticasPorRestauranteAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$restaurante_id" },
                { "totalResenas", new BsonDocument("$sum", 1) },
                { "promedioCalificacion", new BsonDocument("$avg", "$calificacion") },
                { "minCalificacion", new BsonDocument("$min", "$calificacion") },
                { "maxCalificacion", new BsonDocument("$max", "$calificacion") }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "restaurantes" },
                { "localField", "_id" },
                { "foreignField", "_id" },
                { "as", "restaurante" }
            }),
            new BsonDocument("$unwind", "$restaurante"),
            new BsonDocument("$sort", new BsonDocument("totalResenas", -1)),
            new BsonDocument("$limit", 20)
        };

        return await _resenas.Aggregate<BsonDocument>(pipeline).ToListAsync();
    }
}
