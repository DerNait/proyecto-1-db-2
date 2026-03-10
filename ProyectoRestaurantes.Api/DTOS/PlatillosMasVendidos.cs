namespace ProyectoRestaurantes.Api.DTOs.Responses;

public class PlatilloMasVendidoResponse
{
    public string ArticuloId { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public int CantidadVendida { get; set; }
}