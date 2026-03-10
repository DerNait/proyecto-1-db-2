using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("password")]
    public string Password { get; set; } = null!;

    [BsonElement("roles")]
    public List<string> Roles { get; set; } = new();

    [BsonElement("direcciones")]
    public List<Direccion> Direcciones { get; set; } = new();

    [BsonElement("fecha_registro")]
    public DateTime FechaRegistro { get; set; }
}