namespace ProyectoRestaurantes.Api.DTOs.Responses;

public class TopRestauranteResponse
{
    public string RestauranteId { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public double Promedio { get; set; }
    public int TotalResenas { get; set; }
}