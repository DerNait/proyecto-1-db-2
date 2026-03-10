using MongoDB.Driver;
using ProyectoRestaurantes.Api.Models;

namespace ProyectoRestaurantes.Api.Services;

public class IndexInitializationService
{
    private readonly IMongoDatabase _database;

    public IndexInitializationService(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task CrearIndicesAsync()
    {
        Console.WriteLine("=== Inicializando índices de MongoDB ===");

        try
        {
            await CrearIndicesUsuariosAsync();
            await CrearIndicesPedidosAsync();
            await CrearIndicesRestaurantesAsync();
            await CrearIndicesArticulosAsync();
            await CrearIndicesResenasAsync();

            Console.WriteLine("=== Índices creados exitosamente ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"! Error al crear índices (es posible que ya existan con distintas opciones): {ex.Message}");
        }
    }

    private async Task CrearIndicesUsuariosAsync()
    {
        var collection = _database.GetCollection<Usuario>("usuarios");

        // Índice simple: email (único)
        var emailIndex = Builders<Usuario>.IndexKeys.Ascending(u => u.Email);
        var emailOptions = new CreateIndexOptions { Unique = true, Name = "email_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Usuario>(emailIndex, emailOptions));
        Console.WriteLine("✓ Índice simple creado: usuarios.email");

        // Índice de texto: nombre
        var nombreIndex = Builders<Usuario>.IndexKeys.Text(u => u.Nombre);
        var nombreOptions = new CreateIndexOptions { Name = "nombre_text" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Usuario>(nombreIndex, nombreOptions));
        Console.WriteLine("✓ Índice de texto creado: usuarios.nombre");
    }

    private async Task CrearIndicesPedidosAsync()
    {
        var collection = _database.GetCollection<Pedido>("pedidos");

        // Índice simple: usuario_id
        var usuarioIndex = Builders<Pedido>.IndexKeys.Ascending(p => p.UsuarioId);
        var usuarioOptions = new CreateIndexOptions { Name = "usuario_id_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Pedido>(usuarioIndex, usuarioOptions));
        Console.WriteLine("✓ Índice simple creado: pedidos.usuario_id");

        // Índice simple: restaurante_id
        var restIndex = Builders<Pedido>.IndexKeys.Ascending(p => p.RestauranteId);
        var restOptions = new CreateIndexOptions { Name = "restaurante_id_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Pedido>(restIndex, restOptions));
        Console.WriteLine("✓ Índice simple creado: pedidos.restaurante_id");

        // Índice compuesto: estado + fecha_creacion (para filtros y ordenamiento)
        var compuestoIndex = Builders<Pedido>.IndexKeys
            .Ascending(p => p.Estado)
            .Descending(p => p.FechaCreacion);
        var compuestoOptions = new CreateIndexOptions { Name = "estado_1_fecha_creacion_-1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Pedido>(compuestoIndex, compuestoOptions));
        Console.WriteLine("✓ Índice compuesto creado: pedidos.estado + fecha_creacion");

        // Índice geoespacial: ubicacion_entrega
        var geoIndex = Builders<Pedido>.IndexKeys.Geo2DSphere(p => p.UbicacionEntrega);
        var geoOptions = new CreateIndexOptions { Name = "ubicacion_entrega_2dsphere" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Pedido>(geoIndex, geoOptions));
        Console.WriteLine("✓ Índice geoespacial creado: pedidos.ubicacion_entrega");
    }

    private async Task CrearIndicesRestaurantesAsync()
    {
        var collection = _database.GetCollection<Restaurante>("restaurantes");

        // Índice simple: dueno_id
        var duenoIndex = Builders<Restaurante>.IndexKeys.Ascending(r => r.DuenoId);
        var duenoOptions = new CreateIndexOptions { Name = "dueno_id_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Restaurante>(duenoIndex, duenoOptions));
        Console.WriteLine("✓ Índice simple creado: restaurantes.dueno_id");

        // Índice simple: categoria
        var catIndex = Builders<Restaurante>.IndexKeys.Ascending(r => r.Categoria);
        var catOptions = new CreateIndexOptions { Name = "categoria_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Restaurante>(catIndex, catOptions));
        Console.WriteLine("✓ Índice simple creado: restaurantes.categoria");

        // Índice de texto: nombre
        var nombreIndex = Builders<Restaurante>.IndexKeys.Text(r => r.Nombre);
        var nombreOptions = new CreateIndexOptions { Name = "nombre_text" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Restaurante>(nombreIndex, nombreOptions));
        Console.WriteLine("✓ Índice de texto creado: restaurantes.nombre");

        // Índice geoespacial: ubicacion
        var geoIndex = Builders<Restaurante>.IndexKeys.Geo2DSphere(r => r.Ubicacion);
        var geoOptions = new CreateIndexOptions { Name = "ubicacion_2dsphere" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Restaurante>(geoIndex, geoOptions));
        Console.WriteLine("✓ Índice geoespacial creado: restaurantes.ubicacion");
    }

    private async Task CrearIndicesArticulosAsync()
    {
        var collection = _database.GetCollection<Articulo>("articulos");

        // Índice simple: restaurante_id
        var restIndex = Builders<Articulo>.IndexKeys.Ascending(a => a.RestauranteId);
        var restOptions = new CreateIndexOptions { Name = "restaurante_id_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Articulo>(restIndex, restOptions));
        Console.WriteLine("✓ Índice simple creado: articulos.restaurante_id");

        // Índice de texto: nombre + descripcion
        var textoIndex = Builders<Articulo>.IndexKeys
            .Text(a => a.Nombre)
            .Text(a => a.Descripcion);
        var textoOptions = new CreateIndexOptions { Name = "nombre_descripcion_text" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Articulo>(textoIndex, textoOptions));
        Console.WriteLine("✓ Índice de texto creado: articulos.nombre + descripcion");

        // Índice multikey: categorias (array)
        var categoriasIndex = Builders<Articulo>.IndexKeys.Ascending(a => a.Categorias);
        var categoriasOptions = new CreateIndexOptions { Name = "categorias_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Articulo>(categoriasIndex, categoriasOptions));
        Console.WriteLine("✓ Índice multikey creado: articulos.categorias");

        // Índice multikey: ingredientes (array)
        var ingredientesIndex = Builders<Articulo>.IndexKeys.Ascending(a => a.Ingredientes);
        var ingredientesOptions = new CreateIndexOptions { Name = "ingredientes_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Articulo>(ingredientesIndex, ingredientesOptions));
        Console.WriteLine("✓ Índice multikey creado: articulos.ingredientes");
    }

    private async Task CrearIndicesResenasAsync()
    {
        var collection = _database.GetCollection<Resena>("resenas");

        // Índice simple: restaurante_id
        var restIndex = Builders<Resena>.IndexKeys.Ascending(r => r.RestauranteId);
        var restOptions = new CreateIndexOptions { Name = "restaurante_id_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Resena>(restIndex, restOptions));
        Console.WriteLine("✓ Índice simple creado: resenas.restaurante_id");

        // Índice simple: usuario_id
        var usuarioIndex = Builders<Resena>.IndexKeys.Ascending(r => r.UsuarioId);
        var usuarioOptions = new CreateIndexOptions { Name = "usuario_id_1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Resena>(usuarioIndex, usuarioOptions));
        Console.WriteLine("✓ Índice simple creado: resenas.usuario_id");

        // Índice compuesto: restaurante_id + calificacion
        var compuestoIndex = Builders<Resena>.IndexKeys
            .Ascending(r => r.RestauranteId)
            .Descending(r => r.Calificacion);
        var compuestoOptions = new CreateIndexOptions { Name = "restaurante_id_1_calificacion_-1" };
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Resena>(compuestoIndex, compuestoOptions));
        Console.WriteLine("✓ Índice compuesto creado: resenas.restaurante_id + calificacion");
    }
}
