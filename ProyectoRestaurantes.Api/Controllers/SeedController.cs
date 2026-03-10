using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly IMongoDatabase _database;

    public SeedController(IMongoDatabase database)
    {
        _database = database;
    }

    [HttpPost("generar-datos")]
    public async Task<IActionResult> GenerarDatos([FromQuery] int cantidadPedidos = 50000)
    {
        try
        {
            Console.WriteLine($"=== Generando {cantidadPedidos} pedidos de prueba ===");

            var usuariosCol = _database.GetCollection<Usuario>("usuarios");
            var restaurantesCol = _database.GetCollection<Restaurante>("restaurantes");
            var articulosCol = _database.GetCollection<Articulo>("articulos");
            var pedidosCol = _database.GetCollection<Pedido>("pedidos");

            // Verificar si ya hay usuarios
            var countUsuarios = await usuariosCol.CountDocumentsAsync(Builders<Usuario>.Filter.Empty);
            if (countUsuarios == 0)
            {
                await GenerarUsuariosAsync(usuariosCol, 100);
            }

            // Verificar si ya hay restaurantes
            var countRestaurantes = await restaurantesCol.CountDocumentsAsync(Builders<Restaurante>.Filter.Empty);
            if (countRestaurantes == 0)
            {
                await GenerarRestaurantesAsync(restaurantesCol, 50);
            }

            // Verificar si ya hay artículos
            var countArticulos = await articulosCol.CountDocumentsAsync(Builders<Articulo>.Filter.Empty);
            if (countArticulos == 0)
            {
                await GenerarArticulosAsync(articulosCol, restaurantesCol, 500);
            }

            // Generar pedidos
            await GenerarPedidosAsync(pedidosCol, usuariosCol, restaurantesCol, articulosCol, cantidadPedidos);

            return Ok(new
            {
                mensaje = $"Datos generados exitosamente",
                pedidos = cantidadPedidos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task GenerarUsuariosAsync(IMongoCollection<Usuario> collection, int cantidad)
    {
        Console.WriteLine($"Generando {cantidad} usuarios...");
        var usuarios = new List<Usuario>();
        var random = new Random();
        var nombres = new[] { "Juan", "María", "Carlos", "Ana", "Luis", "Laura", "Pedro", "Sofia", "Miguel", "Elena" };
        var apellidos = new[] { "García", "Rodríguez", "Martínez", "López", "González", "Pérez", "Sánchez", "Ramírez", "Torres", "Flores" };

        for (int i = 0; i < cantidad; i++)
        {
            var nombre = $"{nombres[random.Next(nombres.Length)]} {apellidos[random.Next(apellidos.Length)]}";
            usuarios.Add(new Usuario
            {
                Nombre = nombre,
                Email = $"usuario{i}@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("password123"),
                Roles = new List<string> { "cliente" },
                Direcciones = new List<Direccion>
                {
                    new Direccion
                    {
                        Alias = "Principal",
                        Nombre = "Casa",
                        DetalleDireccion = $"Zona {random.Next(1, 22)}, Guatemala",
                        Ubicacion = new Ubicacion
                        {
                            Type = "Point",
                            Coordinates = new[] { -90.5069 + (random.NextDouble() - 0.5) * 0.1, 14.6349 + (random.NextDouble() - 0.5) * 0.1 }
                        }
                    }
                },
                FechaRegistro = DateTime.UtcNow.AddDays(-random.Next(1, 365))
            });
        }

        await collection.InsertManyAsync(usuarios);
        Console.WriteLine($"✓ {cantidad} usuarios creados");
    }

    private async Task GenerarRestaurantesAsync(IMongoCollection<Restaurante> collection, int cantidad)
    {
        Console.WriteLine($"Generando {cantidad} restaurantes...");
        var restaurantes = new List<Restaurante>();
        var random = new Random();
        var categorias = new[] { "Mexicana", "Italiana", "China", "Japonesa", "Americana", "Guatemalteca", "Vegetariana", "Pizza", "Hamburguesas", "Mariscos" };
        var nombres = new[] { "El Sabor", "La Casa", "Don", "Restaurante", "Comedor", "Bistro", "Cocina", "Rincón", "Delicias", "Tradición" };

        for (int i = 0; i < cantidad; i++)
        {
            var categoria = categorias[random.Next(categorias.Length)];
            restaurantes.Add(new Restaurante
            {
                Nombre = $"{nombres[random.Next(nombres.Length)]} {categoria} #{i + 1}",
                Categoria = categoria,
                DuenoId = "000000000000000000000001",
                Ubicacion = new Ubicacion
                {
                    Type = "Point",
                    Coordinates = new[] { -90.5069 + (random.NextDouble() - 0.5) * 0.1, 14.6349 + (random.NextDouble() - 0.5) * 0.1 }
                },
                RatingPromedio = Math.Round(3.0 + random.NextDouble() * 2.0, 1),
                Activo = true
            });
        }

        await collection.InsertManyAsync(restaurantes);
        Console.WriteLine($"✓ {cantidad} restaurantes creados");
    }

    private async Task GenerarArticulosAsync(IMongoCollection<Articulo> collection, IMongoCollection<Restaurante> restaurantesCol, int cantidad)
    {
        Console.WriteLine($"Generando {cantidad} artículos...");
        var restaurantes = await restaurantesCol.Find(Builders<Restaurante>.Filter.Empty).Limit(50).ToListAsync();
        var articulos = new List<Articulo>();
        var random = new Random();
        var platillos = new[] { "Tacos", "Pizza", "Hamburguesa", "Sushi", "Pasta", "Ensalada", "Burrito", "Sopa", "Pollo", "Carne" };
        var adjetivos = new[] { "Especial", "Deluxe", "Suprema", "Clásica", "Premium", "Tradicional", "Gourmet", "Casera" };

        for (int i = 0; i < cantidad; i++)
        {
            var restaurante = restaurantes[random.Next(restaurantes.Count)];
            articulos.Add(new Articulo
            {
                RestauranteId = restaurante.Id!,
                Nombre = $"{platillos[random.Next(platillos.Length)]} {adjetivos[random.Next(adjetivos.Length)]}",
                Descripcion = "Delicioso platillo preparado con ingredientes frescos",
                Precio = Math.Round(25.0 + random.NextDouble() * 100.0, 2),
                Stock = random.Next(10, 100),
                Categorias = new List<string> { restaurante.Categoria },
                Disponible = true,
                Ingredientes = new List<string> { "Ingrediente1", "Ingrediente2", "Ingrediente3" }
            });
        }

        await collection.InsertManyAsync(articulos);
        Console.WriteLine($"✓ {cantidad} artículos creados");
    }

    private async Task GenerarPedidosAsync(
        IMongoCollection<Pedido> pedidosCol,
        IMongoCollection<Usuario> usuariosCol,
        IMongoCollection<Restaurante> restaurantesCol,
        IMongoCollection<Articulo> articulosCol,
        int cantidad)
    {
        Console.WriteLine($"Generando {cantidad} pedidos...");
        var usuarios = await usuariosCol.Find(Builders<Usuario>.Filter.Empty).ToListAsync();
        var restaurantes = await restaurantesCol.Find(Builders<Restaurante>.Filter.Empty).ToListAsync();
        var random = new Random();
        var estados = new[] { "RECIBIDO", "PREPARANDO", "EN_CAMINO", "ENTREGADO", "CANCELADO" };

        var batchSize = 1000;
        var totalBatches = (int)Math.Ceiling((double)cantidad / batchSize);

        for (int batch = 0; batch < totalBatches; batch++)
        {
            var pedidosBatch = new List<Pedido>();
            var currentBatchSize = Math.Min(batchSize, cantidad - (batch * batchSize));

            for (int i = 0; i < currentBatchSize; i++)
            {
                var usuario = usuarios[random.Next(usuarios.Count)];
                var restaurante = restaurantes[random.Next(restaurantes.Count)];

                var articulos = await articulosCol
                    .Find(a => a.RestauranteId == restaurante.Id)
                    .Limit(random.Next(1, 5))
                    .ToListAsync();

                if (!articulos.Any()) continue;

                var items = articulos.Select(a => new ItemPedido
                {
                    ArticuloId = a.Id!,
                    NombreCopia = a.Nombre,
                    PrecioUnitario = a.Precio,
                    Cantidad = random.Next(1, 4),
                    Subtotal = 0
                }).ToList();

                items.ForEach(item => item.Subtotal = item.PrecioUnitario * item.Cantidad);

                pedidosBatch.Add(new Pedido
                {
                    UsuarioId = usuario.Id!,
                    RestauranteId = restaurante.Id!,
                    Items = items,
                    TotalPagar = items.Sum(item => item.Subtotal),
                    Estado = estados[random.Next(estados.Length)],
                    UbicacionEntrega = usuario.Direcciones.First().Ubicacion,
                    FechaCreacion = DateTime.UtcNow.AddDays(-random.Next(1, 180))
                });
            }

            if (pedidosBatch.Any())
            {
                await pedidosCol.InsertManyAsync(pedidosBatch);
                Console.WriteLine($"✓ Batch {batch + 1}/{totalBatches} - {pedidosBatch.Count} pedidos creados");
            }
        }

        Console.WriteLine($"✓ Total: {cantidad} pedidos creados");
    }

    [HttpDelete("limpiar-datos")]
    public async Task<IActionResult> LimpiarDatos()
    {
        try
        {
            await _database.GetCollection<Pedido>("pedidos").DeleteManyAsync(Builders<Pedido>.Filter.Empty);
            await _database.GetCollection<Resena>("resenas").DeleteManyAsync(Builders<Resena>.Filter.Empty);
            await _database.GetCollection<Articulo>("articulos").DeleteManyAsync(Builders<Articulo>.Filter.Empty);
            await _database.GetCollection<Restaurante>("restaurantes").DeleteManyAsync(Builders<Restaurante>.Filter.Empty);
            await _database.GetCollection<Usuario>("usuarios").DeleteManyAsync(Builders<Usuario>.Filter.Empty);

            return Ok(new { mensaje = "Todos los datos han sido eliminados" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
