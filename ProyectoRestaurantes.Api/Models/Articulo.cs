using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class Articulo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("restaurante_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RestauranteId { get; set; } = null!;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = null!;

    [BsonElement("descripcion")]
    public string Descripcion { get; set; } = null!;

    [BsonElement("precio")]
    public double Precio { get; set; }

    [BsonElement("stock")]
    public int Stock { get; set; }

    [BsonElement("categorias")]
    public List<string> Categorias { get; set; } = new();

    [BsonElement("disponible")]
    public bool Disponible { get; set; }

    [BsonElement("ingredientes")]
    public List<string> Ingredientes { get; set; } = new();
}