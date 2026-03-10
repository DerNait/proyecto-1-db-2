using MongoDB.Bson;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Services;

public class ResenaService
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<Resena> _resenas;
    private readonly IMongoCollection<Pedido> _pedidos;
    private readonly IMongoCollection<Restaurante> _restaurantes;

    public ResenaService(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _resenas = database.GetCollection<Resena>("resenas");
        _pedidos = database.GetCollection<Pedido>("pedidos");
        _restaurantes = database.GetCollection<Restaurante>("restaurantes");
    }

    public async Task<Resena> CrearResenaTransaccionalAsync(Resena nuevaResena)
    {
        using var session = await _client.StartSessionAsync();
        session.StartTransaction();

        try
        {
            // 1. Validar que el pedido exista
            var pedido = await _pedidos
                .Find(session, p => p.Id == nuevaResena.PedidoId)
                .FirstOrDefaultAsync();

            if (pedido == null)
                throw new Exception("El pedido especificado no existe.");

            // 2. Validar que el pedido esté en estado ENTREGADO
            if (pedido.Estado != "ENTREGADO")
                throw new Exception($"No se puede reseñar un pedido que no ha sido entregado. Estado actual: {pedido.Estado}");

            // 3. Validar que el usuario del pedido coincida con el de la reseña
            if (pedido.UsuarioId != nuevaResena.UsuarioId)
                throw new Exception("El usuario no puede reseñar un pedido que no le pertenece.");

            // 4. Validar que el restaurante del pedido coincida con el de la reseña
            if (pedido.RestauranteId != nuevaResena.RestauranteId)
                throw new Exception("El restaurante de la reseña no coincide con el del pedido.");

            // 5. Validar que el artículo pertenezca al pedido (si se especificó)
            if (!string.IsNullOrEmpty(nuevaResena.ArticuloId))
            {
                var articuloEnPedido = pedido.Items.Any(item => item.ArticuloId == nuevaResena.ArticuloId);
                if (!articuloEnPedido)
                    throw new Exception("El artículo especificado no pertenece a este pedido.");
            }

            // 6. Guardar la reseña
            nuevaResena.Fecha = DateTime.UtcNow;
            await _resenas.InsertOneAsync(session, nuevaResena);

            // 7. Recalcular y actualizar el rating promedio del restaurante
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("restaurante_id", new BsonObjectId(ObjectId.Parse(nuevaResena.RestauranteId)))),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$restaurante_id" },
                    { "promedio", new BsonDocument("$avg", "$calificacion") }
                })
            };

            var resultado = await _resenas.Aggregate(session, new AggregateFluent<Resena, BsonDocument>(
                _resenas,
                PipelineDefinition<Resena, BsonDocument>.Create(pipeline),
                new AggregateOptions()
            )).FirstOrDefaultAsync();

            var promedio = resultado != null ? resultado["promedio"].AsDouble : 0;

            var update = Builders<Restaurante>.Update.Set(r => r.RatingPromedio, Math.Round(promedio, 2));
            await _restaurantes.UpdateOneAsync(session, r => r.Id == nuevaResena.RestauranteId, update);

            // 8. Confirmar la transacción
            await session.CommitTransactionAsync();

            return nuevaResena;
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            throw new Exception($"Error al crear la reseña: {ex.Message}");
        }
    }
}
