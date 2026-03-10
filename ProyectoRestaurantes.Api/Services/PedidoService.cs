using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Services;

public class PedidoService
{
    private readonly IMongoClient _client;
    private readonly IMongoCollection<Pedido> _pedidos;
    private readonly IMongoCollection<Articulo> _articulos;

    // Inyectamos el cliente para poder crear sesiones transaccionales
    public PedidoService(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _pedidos = database.GetCollection<Pedido>("pedidos");
        _articulos = database.GetCollection<Articulo>("articulos");
    }

    public async Task<Pedido> CrearPedidoTransaccionalAsync(Pedido nuevoPedido)
    {
        // Iniciamos la sesión de MongoDB
        using var session = await _client.StartSessionAsync();
        
        // Arrancamos la transacción multidocumento
        session.StartTransaction();

        try
        {
            double totalPagar = 0;

            // Validar inventario y aplicar el Patrón Snapshot
            foreach (var item in nuevoPedido.Items)
            {
                // Buscamos el artículo pasando la 'session' para que sea parte de la transacción
                var articulo = await _articulos
                    .Find(session, a => a.Id == item.ArticuloId)
                    .FirstOrDefaultAsync();

                if (articulo == null)
                    throw new Exception($"El artículo con ID {item.ArticuloId} no existe.");

                if (articulo.Stock < item.Cantidad)
                    throw new Exception($"Inventario insuficiente para '{articulo.Nombre}'. Stock actual: {articulo.Stock}");

                // PATRÓN SNAPSHOT: Congelamos el precio y el nombre en el pedido
                item.PrecioUnitario = articulo.Precio;
                item.NombreCopia = articulo.Nombre;
                item.Subtotal = item.PrecioUnitario * item.Cantidad;
                totalPagar += item.Subtotal;

                // Descontar el inventario del artículo
                var updateStock = Builders<Articulo>.Update.Inc(a => a.Stock, -item.Cantidad);
                await _articulos.UpdateOneAsync(session, a => a.Id == articulo.Id, updateStock);
            }

            // Completar los datos del pedido
            nuevoPedido.TotalPagar = totalPagar;
            nuevoPedido.Estado = "RECIBIDO";
            nuevoPedido.FechaCreacion = DateTime.UtcNow;

            // Guardar el pedido en la base de datos
            await _pedidos.InsertOneAsync(session, nuevoPedido);

            // Si todo salió bien, hacemos el Commit (guardamos todo de golpe)
            await session.CommitTransactionAsync();
            
            return nuevoPedido;
        }
        catch (Exception ex)
        {
            // Si hubo CUALQUIER error (ej. falta de stock), hacemos Rollback
            await session.AbortTransactionAsync();
            throw new Exception($"Error al procesar el pedido: {ex.Message}");
        }
    }

    public async Task<bool> CancelarPedidoTransaccionalAsync(string pedidoId)
    {
        // Iniciamos la sesión y la transacción
        using var session = await _client.StartSessionAsync();
        session.StartTransaction();

        try
        {
            // Buscamos el pedido en la base de datos
            var pedido = await _pedidos.Find(session, p => p.Id == pedidoId).FirstOrDefaultAsync();

            if (pedido == null)
                throw new Exception("El pedido especificado no existe.");

            // Validación de estado: No se puede cancelar si ya se entregó o si ya estaba cancelado 
            if (pedido.Estado == "ENTREGADO" || pedido.Estado == "CANCELADO")
                throw new Exception($"Acción denegada. El pedido se encuentra en estado: {pedido.Estado}");

            // Reposición de inventario: Devolvemos los artículos al stock
            foreach (var item in pedido.Items)
            {
                // Pasamos la cantidad en positivo para hacer el incremento ($inc)
                var updateStock = Builders<Articulo>.Update.Inc(a => a.Stock, item.Cantidad);
                await _articulos.UpdateOneAsync(session, a => a.Id == item.ArticuloId, updateStock);
            }

            // Actualizamos el estado del pedido a CANCELADO
            var updatePedido = Builders<Pedido>.Update.Set(p => p.Estado, "CANCELADO");
            await _pedidos.UpdateOneAsync(session, p => p.Id == pedidoId, updatePedido);

            // Confirmamos la transacción
            await session.CommitTransactionAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            // Si algo falla, deshacemos cualquier cambio en el inventario
            await session.AbortTransactionAsync();
            throw new Exception($"Error al cancelar el pedido: {ex.Message}");
        }
    }
}