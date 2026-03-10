using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Repositories;

public class ArticuloRepository
{
    private readonly IMongoCollection<Articulo> _articulos;

    public ArticuloRepository(IMongoDatabase database)
    {
        _articulos = database.GetCollection<Articulo>("articulos");
    }

    public async Task<(List<Articulo> Items, long Total)> ObtenerAsync(
        string? restauranteId = null,
        string? busqueda = null,
        bool? disponible = null,
        string sortPor = "nombre",
        int sortDir = 1,
        int skip = 0,
        int limit = 30)
    {
        var builder = Builders<Articulo>.Filter;
        var filtro = builder.Empty;

        if (!string.IsNullOrWhiteSpace(restauranteId))
            filtro &= builder.Eq(a => a.RestauranteId, restauranteId);

        if (!string.IsNullOrWhiteSpace(busqueda))
            filtro &= builder.Regex(a => a.Nombre, new MongoDB.Bson.BsonRegularExpression(busqueda, "i"));

        if (disponible.HasValue)
            filtro &= builder.Eq(a => a.Disponible, disponible.Value);

        var sortDef = sortDir >= 0
            ? Builders<Articulo>.Sort.Ascending(sortPor)
            : Builders<Articulo>.Sort.Descending(sortPor);

        var total = await _articulos.CountDocumentsAsync(filtro);
        var items = await _articulos.Find(filtro).Sort(sortDef).Skip(skip).Limit(limit).ToListAsync();
        return (items, total);
    }

    public async Task<Articulo?> ObtenerPorIdAsync(string id) =>
        await _articulos.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<Articulo> CrearAsync(Articulo articulo)
    {
        await _articulos.InsertOneAsync(articulo);
        return articulo;
    }

    public async Task<bool> ActualizarAsync(string id, Articulo articulo)
    {
        articulo.Id = id;
        var result = await _articulos.ReplaceOneAsync(a => a.Id == id, articulo);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> EliminarAsync(string id)
    {
        var result = await _articulos.DeleteOneAsync(a => a.Id == id);
        return result.DeletedCount > 0;
    }

    // Manejo de Arrays: $push - Agregar ingrediente
    public async Task<bool> AgregarIngredienteAsync(string id, string ingrediente)
    {
        var update = Builders<Articulo>.Update.AddToSet(a => a.Ingredientes, ingrediente);
        var result = await _articulos.UpdateOneAsync(a => a.Id == id, update);
        return result.ModifiedCount > 0;
    }

    // Manejo de Arrays: $pull - Quitar ingrediente
    public async Task<bool> QuitarIngredienteAsync(string id, string ingrediente)
    {
        var update = Builders<Articulo>.Update.Pull(a => a.Ingredientes, ingrediente);
        var result = await _articulos.UpdateOneAsync(a => a.Id == id, update);
        return result.ModifiedCount > 0;
    }

    // Manejo de Arrays: $addToSet - Agregar categoría sin duplicados
    public async Task<bool> AgregarCategoriaAsync(string id, string categoria)
    {
        var update = Builders<Articulo>.Update.AddToSet(a => a.Categorias, categoria);
        var result = await _articulos.UpdateOneAsync(a => a.Id == id, update);
        return result.ModifiedCount > 0;
    }

    // Manejo de Arrays: $pull - Quitar categoría
    public async Task<bool> QuitarCategoriaAsync(string id, string categoria)
    {
        var update = Builders<Articulo>.Update.Pull(a => a.Categorias, categoria);
        var result = await _articulos.UpdateOneAsync(a => a.Id == id, update);
        return result.ModifiedCount > 0;
    }

    // Bulk Write: actualizar precios masivamente
    public async Task<BulkWriteResult<Articulo>> ActualizarPreciosBulkAsync(List<(string Id, double NuevoPrecio)> cambios)
    {
        var operaciones = cambios.Select(c =>
            new UpdateOneModel<Articulo>(
                Builders<Articulo>.Filter.Eq(a => a.Id, c.Id),
                Builders<Articulo>.Update.Set(a => a.Precio, c.NuevoPrecio)
            )
        ).Cast<WriteModel<Articulo>>().ToList();

        return await _articulos.BulkWriteAsync(operaciones);
    }

    // Eliminar varios artículos de un restaurante (DeleteMany)
    public async Task<long> EliminarPorRestauranteAsync(string restauranteId)
    {
        var result = await _articulos.DeleteManyAsync(a => a.RestauranteId == restauranteId);
        return result.DeletedCount;
    }
}
