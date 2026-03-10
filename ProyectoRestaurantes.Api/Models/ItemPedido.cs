using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProyectoRestaurantes.Api.Models;

public class ItemPedido
{
    [BsonElement("articulo_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ArticuloId { get; set; } = null!;

    [BsonElement("nombre_copia")]
    public string NombreCopia { get; set; } = null!;

    [BsonElement("precio_unitario")]
    public double PrecioUnitario { get; set; }

    [BsonElement("cantidad")]
    public int Cantidad { get; set; }

    [BsonElement("subtotal")]
    public double Subtotal { get; set; }
}