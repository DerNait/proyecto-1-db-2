using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Direccion
{
    [BsonElement("alias")]
    public string Alias { get; set; } = null!;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("direccion")]
    public string DetalleDireccion { get; set; } = null!;

    [BsonElement("ubicacion")]
    public Ubicacion? Ubicacion { get; set; }
}