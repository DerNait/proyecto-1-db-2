using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Pedido
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("usuario_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = null!;

    [BsonElement("restaurante_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RestauranteId { get; set; } = null!;

    [BsonElement("repartidor_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RepartidorId { get; set; }

    [BsonElement("items")]
    public List<ItemPedido> Items { get; set; } = new();

    [BsonElement("total_pagar")]
    public double TotalPagar { get; set; }

    [BsonElement("estado")]
    public string Estado { get; set; } = null!;

    [BsonElement("ubicacion_entrega")]
    public Ubicacion UbicacionEntrega { get; set; } = null!;

    [BsonElement("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }
}