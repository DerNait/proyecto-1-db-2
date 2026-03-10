using System.Text.Json;

namespace ProyectoRestaurantes.Client.Services;

public class AppState
{
    public string Token { get; private set; } = "";
    public string UsuarioId { get; private set; } = "";
    public string Nombre { get; private set; } = "";
    public string Email { get; private set; } = "";
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action? OnChange;

    public void Login(string token)
    {
        Token = token;
        try
        {
            // Decodificar el payload del JWT (parte 2 en Base64Url)
            var parts = token.Split('.');
            if (parts.Length >= 2)
            {
                var payload = parts[1];
                // Agregar padding si es necesario
                var padded = payload.Length % 4 == 0 ? payload : payload + new string('=', 4 - payload.Length % 4);
                padded = padded.Replace('-', '+').Replace('_', '/');
                var bytes = Convert.FromBase64String(padded);
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (claims != null)
                {
                    UsuarioId = claims.TryGetValue("sub", out var sub) ? sub.GetString() ?? "" : "";
                    Email = claims.TryGetValue("email", out var emailEl) ? emailEl.GetString() ?? "" : "";
                    Nombre = claims.TryGetValue("nombre", out var n) ? n.GetString() ?? Email :
                             (claims.TryGetValue("name", out var name) ? name.GetString() ?? Email : Email);
                }
            }
        }
        catch { /* Token invalido, ignorar */ }
        NotifyStateChanged();
    }

    public void Logout()
    {
        Token = "";
        UsuarioId = "";
        Nombre = "";
        Email = "";
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
