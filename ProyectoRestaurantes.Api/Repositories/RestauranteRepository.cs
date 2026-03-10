using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Repositories;

public class RestauranteRepository
{
    private readonly IMongoCollection<Restaurante> _restaurantes;
    private readonly IMongoCollection<Resena> _resenas;

    public RestauranteRepository(IMongoDatabase database)
    {
        _restaurantes = database.GetCollection<Restaurante>("restaurantes");
        _resenas = database.GetCollection<Resena>("resenas");
    }

    // Listar con filtro, proyección, sort y paginación
    public async Task<(List<Restaurante> Items, long Total)> ObtenerAsync(
        string? busqueda = null,
        string? categoria = null,
        bool? activo = null,
        string sortPor = "nombre",
        int sortDir = 1,
        int skip = 0,
        int limit = 20)
    {
        var builder = Builders<Restaurante>.Filter;
        var filtro = builder.Empty;

        if (!string.IsNullOrWhiteSpace(busqueda))
            filtro &= builder.Regex(r => r.Nombre, new BsonRegularExpression(busqueda, "i"));

        if (!string.IsNullOrWhiteSpace(categoria))
            filtro &= builder.Eq(r => r.Categoria, categoria);

        if (activo.HasValue)
            filtro &= builder.Eq(r => r.Activo, activo.Value);

        var sortDef = sortDir >= 0
            ? Builders<Restaurante>.Sort.Ascending(sortPor)
            : Builders<Restaurante>.Sort.Descending(sortPor);

        var total = await _restaurantes.CountDocumentsAsync(filtro);
        var items = await _restaurantes.Find(filtro).Sort(sortDef).Skip(skip).Limit(limit).ToListAsync();

        return (items, total);
    }

    public async Task<Restaurante?> ObtenerPorIdAsync(string id) =>
        await _restaurantes.Find(r => r.Id == id).FirstOrDefaultAsync();

    public async Task<Restaurante> CrearAsync(Restaurante restaurante)
    {
        restaurante.RatingPromedio = 0;
        await _restaurantes.InsertOneAsync(restaurante);
        return restaurante;
    }

    public async Task<bool> ActualizarAsync(string id, Restaurante restaurante)
    {
        restaurante.Id = id;
        var result = await _restaurantes.ReplaceOneAsync(r => r.Id == id, restaurante);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> EliminarAsync(string id)
    {
        var result = await _restaurantes.DeleteOneAsync(r => r.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> ActualizarImagenAsync(string id, string gridfsId)
    {
        var update = Builders<Restaurante>.Update.Set(r => r.ImagenPortadaId, gridfsId);
        var result = await _restaurantes.UpdateOneAsync(r => r.Id == id, update);
        return result.ModifiedCount > 0;
    }

    // Desactivar/activar varios restaurantes a la vez ($set en múltiples documentos)
    public async Task<long> ActualizarActivosBulkAsync(List<string> ids, bool activo)
    {
        var filtro = Builders<Restaurante>.Filter.In(r => r.Id, ids);
        var update = Builders<Restaurante>.Update.Set(r => r.Activo, activo);
        var result = await _restaurantes.UpdateManyAsync(filtro, update);
        return result.ModifiedCount;
    }

    // Consulta multi-colección: restaurante con su rating calculado desde reseñas
    public async Task<List<BsonDocument>> ObtenerConRatingAsync(int limite = 10)
    {
        var pipeline = new[]
        {
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "resenas" },
                { "localField", "_id" },
                { "foreignField", "restaurante_id" },
                { "as", "resenas" }
            }),
            new BsonDocument("$addFields", new BsonDocument
            {
                { "totalResenas", new BsonDocument("$size", "$resenas") },
                { "ratingCalculado", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", "$resenas"), 0 }),
                        new BsonDocument("$avg", "$resenas.calificacion"),
                        0
                    })
                }
            }),
            new BsonDocument("$project", new BsonDocument { { "resenas", 0 } }),
            new BsonDocument("$sort", new BsonDocument("ratingCalculado", -1)),
            new BsonDocument("$limit", limite)
        };

        return await _restaurantes.Aggregate<BsonDocument>(pipeline).ToListAsync();
    }

    // Distinct: obtener categorías únicas disponibles
    public async Task<List<string>> ObtenerCategoriasAsync() =>
        await _restaurantes.Distinct<string>("categoria", Builders<Restaurante>.Filter.Empty).ToListAsync();
}
