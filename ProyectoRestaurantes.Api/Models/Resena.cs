using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Resena
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

    [BsonElement("pedido_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PedidoId { get; set; } = null!;

    [BsonElement("articulo_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ArticuloId { get; set; }

    [BsonElement("calificacion")]
    public int Calificacion { get; set; }

    [BsonElement("comentario")]
    public string? Comentario { get; set; }

    [BsonElement("fecha")]
    public DateTime Fecha { get; set; }

    [BsonElement("fotos_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> FotosId { get; set; } = new();
}