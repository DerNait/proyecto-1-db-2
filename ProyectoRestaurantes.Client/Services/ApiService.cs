using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProyectoRestaurantes.Client.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AppState _state;
    private const string BaseUrl = "http://localhost:5257/api";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiService(HttpClient http, AppState state)
    {
        _http = http;
        _state = state;
    }

    private void SetAuth()
    {
        if (!string.IsNullOrEmpty(_state.Token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _state.Token);
    }

    // --- Auth ---
    public async Task<string?> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode) return null;
        var data = await res.Content.ReadFromJsonAsync<JsonElement>();
        return data.GetProperty("token").GetString();
    }

    public async Task<bool> RegistrarAsync(object usuario)
    {
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/auth/registro", usuario);
        return res.IsSuccessStatusCode;
    }

    // --- Restaurantes ---
    public async Task<JsonElement?> GetRestaurantesAsync(string? busqueda = null, string? categoria = null,
        bool? activo = null, string sortPor = "nombre", int sortDir = 1, int skip = 0, int limit = 20)
    {
        SetAuth();
        var url = $"{BaseUrl}/restaurantes?sortPor={sortPor}&sortDir={sortDir}&skip={skip}&limit={limit}";
        if (!string.IsNullOrEmpty(busqueda)) url += $"&busqueda={Uri.EscapeDataString(busqueda)}";
        if (!string.IsNullOrEmpty(categoria)) url += $"&categoria={Uri.EscapeDataString(categoria)}";
        if (activo.HasValue) url += $"&activo={activo}";
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<JsonElement?> GetRestaurantesPorIdAsync(string id)
    {
        SetAuth();
        var res = await _http.GetAsync($"{BaseUrl}/restaurantes/{id}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<bool> CrearRestauranteAsync(object restaurante)
    {
        SetAuth();
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/restaurantes", restaurante);
        return res.IsSuccessStatusCode;
    }

    public async Task<JsonElement?> GetRestauranteCreado(object restaurante)
    {
        SetAuth();
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/restaurantes", restaurante);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<bool> ActualizarRestauranteAsync(string id, object restaurante)
    {
        SetAuth();
        var res = await _http.PutAsJsonAsync($"{BaseUrl}/restaurantes/{id}", restaurante);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> EliminarRestauranteAsync(string id)
    {
        SetAuth();
        var res = await _http.DeleteAsync($"{BaseUrl}/restaurantes/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> SubirImagenRestauranteAsync(string id, byte[] bytes, string fileName, string contentType)
    {
        SetAuth();
        using var content = new MultipartFormDataContent();
        var fc = new ByteArrayContent(bytes);
        fc.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fc, "archivo", fileName);
        var res = await _http.PostAsync($"{BaseUrl}/restaurantes/{id}/imagen", content);
        return res.IsSuccessStatusCode;
    }

    public async Task<JsonElement?> GetCategoriasAsync()
    {
        var res = await _http.GetAsync($"{BaseUrl}/restaurantes/categorias");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // --- Artículos ---
    public async Task<JsonElement?> GetArticulosAsync(string? restauranteId = null, string? busqueda = null,
        bool? disponible = null, int skip = 0, int limit = 30)
    {
        SetAuth();
        var url = $"{BaseUrl}/articulos?skip={skip}&limit={limit}";
        if (!string.IsNullOrEmpty(restauranteId)) url += $"&restauranteId={restauranteId}";
        if (!string.IsNullOrEmpty(busqueda)) url += $"&busqueda={Uri.EscapeDataString(busqueda)}";
        if (disponible.HasValue) url += $"&disponible={disponible}";
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<bool> CrearArticuloAsync(object articulo)
    {
        SetAuth();
        return (await _http.PostAsJsonAsync($"{BaseUrl}/articulos", articulo)).IsSuccessStatusCode;
    }

    public async Task<bool> ActualizarArticuloAsync(string id, object articulo)
    {
        SetAuth();
        return (await _http.PutAsJsonAsync($"{BaseUrl}/articulos/{id}", articulo)).IsSuccessStatusCode;
    }

    public async Task<bool> EliminarArticuloAsync(string id)
    {
        SetAuth();
        return (await _http.DeleteAsync($"{BaseUrl}/articulos/{id}")).IsSuccessStatusCode;
    }

    public async Task<bool> AgregarIngredienteAsync(string id, string valor)
    {
        SetAuth();
        return (await _http.PostAsJsonAsync($"{BaseUrl}/articulos/{id}/ingredientes", new { valor })).IsSuccessStatusCode;
    }

    public async Task<bool> QuitarIngredienteAsync(string id, string valor)
    {
        SetAuth();
        var req = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/articulos/{id}/ingredientes")
        { Content = JsonContent.Create(new { valor }) };
        return (await _http.SendAsync(req)).IsSuccessStatusCode;
    }

    public async Task<bool> AgregarCategoriaArticuloAsync(string id, string valor)
    {
        SetAuth();
        return (await _http.PostAsJsonAsync($"{BaseUrl}/articulos/{id}/categorias", new { valor })).IsSuccessStatusCode;
    }

    public async Task<bool> QuitarCategoriaArticuloAsync(string id, string valor)
    {
        SetAuth();
        var req = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/articulos/{id}/categorias")
        { Content = JsonContent.Create(new { valor }) };
        return (await _http.SendAsync(req)).IsSuccessStatusCode;
    }

    // --- Pedidos ---
    public async Task<JsonElement?> GetPedidosAsync(string? usuarioId = null, string? restauranteId = null,
        string? estado = null, int skip = 0, int limit = 20)
    {
        SetAuth();
        var url = $"{BaseUrl}/pedidos?skip={skip}&limit={limit}";
        if (!string.IsNullOrEmpty(usuarioId)) url += $"&usuarioId={usuarioId}";
        if (!string.IsNullOrEmpty(restauranteId)) url += $"&restauranteId={restauranteId}";
        if (!string.IsNullOrEmpty(estado)) url += $"&estado={estado}";
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<(bool Ok, string Msg)> CrearPedidoAsync(object pedido)
    {
        SetAuth();
        var res = await _http.PostAsJsonAsync($"{BaseUrl}/pedidos", pedido);
        var body = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, body);
    }

    public async Task<(bool Ok, string Msg)> CancelarPedidoAsync(string id)
    {
        SetAuth();
        var res = await _http.PutAsync($"{BaseUrl}/pedidos/{id}/cancelar", null);
        var body = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, body);
    }

    public async Task<(bool Ok, string Msg)> ActualizarEstadoPedidoAsync(string id, string estado)
    {
        SetAuth();
        var res = await _http.PutAsJsonAsync($"{BaseUrl}/pedidos/{id}/estado", new { estado });
        var body = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, body);
    }

    public async Task<JsonElement?> GetPedidosEntregadosUsuarioAsync(string usuarioId)
    {
        SetAuth();
        var res = await _http.GetAsync($"{BaseUrl}/pedidos/entregados/usuario/{usuarioId}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<JsonElement?> GetPedidoPorIdAsync(string id)
    {
        SetAuth();
        var res = await _http.GetAsync($"{BaseUrl}/pedidos/{id}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // --- Reseñas ---
    public async Task<JsonElement?> GetResenasAsync(string? restauranteId = null, string? usuarioId = null,
        int? calMin = null, int skip = 0, int limit = 20)
    {
        SetAuth();
        var url = $"{BaseUrl}/resenas?skip={skip}&limit={limit}";
        if (!string.IsNullOrEmpty(restauranteId)) url += $"&restauranteId={restauranteId}";
        if (!string.IsNullOrEmpty(usuarioId)) url += $"&usuarioId={usuarioId}";
        if (calMin.HasValue) url += $"&calificacionMin={calMin}";
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<bool> CrearResenaAsync(object resena)
    {
        SetAuth();
        return (await _http.PostAsJsonAsync($"{BaseUrl}/resenas", resena)).IsSuccessStatusCode;
    }

    public async Task<bool> EliminarResenaAsync(string id)
    {
        SetAuth();
        return (await _http.DeleteAsync($"{BaseUrl}/resenas/{id}")).IsSuccessStatusCode;
    }

    // --- Usuarios ---
    public async Task<JsonElement?> GetUsuariosAsync(string? busqueda = null, int skip = 0, int limit = 20)
    {
        SetAuth();
        var url = $"{BaseUrl}/usuarios?skip={skip}&limit={limit}";
        if (!string.IsNullOrEmpty(busqueda)) url += $"&busqueda={Uri.EscapeDataString(busqueda)}";
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<bool> EliminarUsuarioAsync(string id)
    {
        SetAuth();
        return (await _http.DeleteAsync($"{BaseUrl}/usuarios/{id}")).IsSuccessStatusCode;
    }

    public async Task<bool> AgregarDireccionAsync(string usuarioId, object direccion)
    {
        SetAuth();
        return (await _http.PostAsJsonAsync($"{BaseUrl}/usuarios/{usuarioId}/direcciones", direccion)).IsSuccessStatusCode;
    }

    public async Task<bool> QuitarDireccionAsync(string usuarioId, string alias)
    {
        SetAuth();
        return (await _http.DeleteAsync($"{BaseUrl}/usuarios/{usuarioId}/direcciones/{Uri.EscapeDataString(alias)}")).IsSuccessStatusCode;
    }

    // --- GridFS ---
    public async Task<JsonElement?> GetArchivosAsync(int skip = 0, int limit = 50)
    {
        SetAuth();
        var res = await _http.GetAsync($"{BaseUrl}/gridfs?skip={skip}&limit={limit}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<(bool Ok, string Id, string Msg)> SubirArchivoAsync(byte[] bytes, string fileName, string contentType)
    {
        SetAuth();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "archivo", fileName);
        var res = await _http.PostAsync($"{BaseUrl}/gridfs/upload", content);
        if (!res.IsSuccessStatusCode) return (false, "", await res.Content.ReadAsStringAsync());
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        return (true, json.GetProperty("id").GetString() ?? "", "");
    }

    public async Task<bool> EliminarArchivoAsync(string id)
    {
        SetAuth();
        return (await _http.DeleteAsync($"{BaseUrl}/gridfs/{id}")).IsSuccessStatusCode;
    }

    public string GetDownloadUrl(string id) => $"{BaseUrl}/gridfs/{id}/download";

    // --- Reportes ---
    public async Task<JsonElement?> GetTopRestaurantesAsync(int limite = 5)
    {
        var res = await _http.GetAsync($"{BaseUrl}/reportes/top-restaurantes?limite={limite}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<JsonElement?> GetPlatillosMasVendidosAsync(int limite = 10)
    {
        var res = await _http.GetAsync($"{BaseUrl}/reportes/platillos-mas-vendidos?limite={limite}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<JsonElement?> GetEstadisticasResenasAsync()
    {
        var res = await _http.GetAsync($"{BaseUrl}/resenas/estadisticas");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }
}
