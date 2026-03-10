using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Direccion
{
    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("direccion")]
    public string DetalleDireccion { get; set; } = null!;

    [BsonElement("ubicacion")]
    public Ubicacion Ubicacion { get; set; } = null!;
}