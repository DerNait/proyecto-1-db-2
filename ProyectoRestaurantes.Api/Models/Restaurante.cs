using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Restaurante
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("categoria")]
    public string Categoria { get; set; } = null!;

    [BsonElement("dueno_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DuenoId { get; set; } = null!;

    [BsonElement("ubicacion")]
    public Ubicacion Ubicacion { get; set; } = null!;

    [BsonElement("rating_promedio")]
    public double RatingPromedio { get; set; }

    [BsonElement("activo")]
    public bool Activo { get; set; }

    [BsonElement("imagen_portada")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ImagenPortadaId { get; set; }
}