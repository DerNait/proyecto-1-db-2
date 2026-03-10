using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Repositories;

public class UsuarioRepository
{
    private readonly IMongoCollection<Usuario> _usuarios;

    public UsuarioRepository(IMongoDatabase database)
    {
        _usuarios = database.GetCollection<Usuario>("usuarios");
    }

    public async Task<(List<Usuario> Items, long Total)> ObtenerAsync(
        string? busqueda = null,
        int skip = 0,
        int limit = 20)
    {
        var builder = Builders<Usuario>.Filter;
        var filtro = builder.Empty;

        if (!string.IsNullOrWhiteSpace(busqueda))
            filtro &= builder.Or(
                builder.Regex(u => u.Nombre, new MongoDB.Bson.BsonRegularExpression(busqueda, "i")),
                builder.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(busqueda, "i"))
            );

        // Proyectar para NO retornar el campo password
        var proyeccion = Builders<Usuario>.Projection.Exclude(u => u.Password);
        var total = await _usuarios.CountDocumentsAsync(filtro);
        var items = await _usuarios.Find(filtro)
            .Project<Usuario>(proyeccion)
            .Skip(skip).Limit(limit).ToListAsync();

        return (items, total);
    }

    public async Task<Usuario?> ObtenerPorIdAsync(string id)
    {
        var proyeccion = Builders<Usuario>.Projection.Exclude(u => u.Password);
        return await _usuarios.Find(u => u.Id == id).Project<Usuario>(proyeccion).FirstOrDefaultAsync();
    }

    public async Task<bool> ActualizarAsync(string id, string nombre, List<string> roles)
    {
        var update = Builders<Usuario>.Update
            .Set(u => u.Nombre, nombre)
            .Set(u => u.Roles, roles);
        var result = await _usuarios.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> EliminarAsync(string id)
    {
        var result = await _usuarios.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    // Documento embebido: agregar una dirección al array del usuario ($push)
    public async Task<bool> AgregarDireccionAsync(string usuarioId, Direccion direccion)
    {
        var update = Builders<Usuario>.Update.Push(u => u.Direcciones, direccion);
        var result = await _usuarios.UpdateOneAsync(u => u.Id == usuarioId, update);
        return result.ModifiedCount > 0;
    }

    // Documento embebido: quitar una dirección por su alias ($pull con filtro anidado)
    public async Task<bool> QuitarDireccionAsync(string usuarioId, string alias)
    {
        var update = Builders<Usuario>.Update.PullFilter(
            u => u.Direcciones,
            d => d.Alias == alias
        );
        var result = await _usuarios.UpdateOneAsync(u => u.Id == usuarioId, update);
        return result.ModifiedCount > 0;
    }
}
