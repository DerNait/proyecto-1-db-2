# Proyecto Restaurantes

Sistema de gestion de restaurantes desarrollado con ASP.NET Core 8 (Web API) y Blazor WebAssembly como frontend. Utiliza MongoDB Atlas como base de datos y GridFS para almacenamiento de archivos.

---

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Acceso a una instancia de MongoDB Atlas (o local)
- Git

---

## Estructura del proyecto

```
ProyectoRestaurantes/
├── ProyectoRestaurantes.sln
├── ProyectoRestaurantes.Api/       # Backend (ASP.NET Core Web API)
└── ProyectoRestaurantes.Client/    # Frontend (Blazor WebAssembly)
```

---

## Configuracion

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd ProyectoRestaurantes
```

### 2. Configurar la API

El archivo `ProyectoRestaurantes.Api/appsettings.json` contiene la configuracion de conexion. Existe un archivo de ejemplo en `appsettings.example.json` con la estructura esperada:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb+srv://<usuario>:<contraseña>@<cluster>.mongodb.net/",
    "DatabaseName": "proyecto_restaurantes"
  },
  "Jwt": {
    "Key": "<clave-secreta-larga-minimo-32-caracteres>",
    "Issuer": "ProyectoRestaurantesApi",
    "Audience": "UsuariosApp"
  }
}
```

Edita `appsettings.json` con tus credenciales reales antes de ejecutar el proyecto.

---

## Ejecucion

El proyecto requiere que tanto la API como el cliente corran de forma simultanea, en terminales separadas.

### Terminal 1 - API (Backend)

```bash
cd ProyectoRestaurantes.Api
dotnet run
```

La API quedara disponible en:
- `https://localhost:7XXX` (HTTPS)
- `http://localhost:5XXX` (HTTP)

Swagger UI estara disponible en: `https://localhost:<puerto>/swagger`

### Terminal 2 - Cliente (Frontend)

```bash
cd ProyectoRestaurantes.Client
dotnet run
```

El cliente Blazor quedara disponible en:
- `http://localhost:5249`
- `https://localhost:7214`

Abre el navegador en `http://localhost:5249` para acceder a la aplicacion.

---

## Primeros pasos: cargar datos de prueba

La API incluye un endpoint de seeding para generar datos de prueba en la base de datos.

### Desde Swagger

1. Abre `https://localhost:<puerto-api>/swagger`
2. Busca el endpoint **POST** `/api/Seed/generar-datos`
3. Ejecutalo (opcionalmente indica la cantidad de pedidos con el parametro `cantidadPedidos`, por defecto 50,000)

Esto generara automaticamente:
- 100 usuarios de prueba
- 50 restaurantes
- 500 articulos del menu
- Los pedidos indicados (distribuidos en lotes de 1,000)

### Limpiar datos

Para eliminar todos los datos generados, usa el endpoint **DELETE** `/api/Seed/limpiar-datos` desde Swagger.

---

## Autenticacion

La API usa JWT Bearer para proteger sus endpoints. El flujo es el siguiente:

### Registrar un usuario

**POST** `/api/Auth/registro`

```json
{
  "nombre": "Tu Nombre",
  "email": "tu@email.com",
  "password": "tu_password"
}
```

### Iniciar sesion

**POST** `/api/Auth/login`

```json
{
  "email": "tu@email.com",
  "password": "tu_password"
}
```

La respuesta contendra un `token` JWT. Para usar endpoints protegidos en Swagger, haz clic en el boton **Authorize** e ingresa el token con el formato:

```
Bearer <tu_token>
```

Desde el cliente Blazor, el login se realiza directamente desde la interfaz de la aplicacion.

---

## Funcionalidades del sistema

### Modulo de Restaurantes

- Listar, crear, editar y eliminar restaurantes
- Asignar categoria, ubicacion geografica y rating
- Subir imagen de portada del restaurante
- Ver detalle completo del restaurante en un modal

### Modulo de Articulos

- Listar, crear, editar y eliminar articulos del menu
- Asociar articulos a un restaurante
- Gestionar precio, stock, ingredientes y disponibilidad

### Modulo de Pedidos

- Crear pedidos con multiples articulos
- Seguimiento de estado: `RECIBIDO`, `PREPARANDO`, `EN_CAMINO`, `ENTREGADO`, `CANCELADO`
- Calculo automatico de subtotales y total

### Modulo de Resenas

- Crear resenas asociadas a un restaurante y pedido
- Calificacion del 1 al 5
- Actualizacion automatica del rating promedio del restaurante mediante transaccion

### Modulo de Usuarios

- Ver y gestionar usuarios registrados
- Gestion de direcciones con coordenadas geograficas

### Modulo de Archivos

- Subida y visualizacion de archivos mediante GridFS (MongoDB)

### Reportes y Estadisticas (pagina Home)

- Estadisticas generales del sistema (total usuarios, restaurantes, articulos, pedidos, resenas)
- Ventas totales y promedio
- Pedidos por estado
- Calificacion promedio general
- Top restaurantes por pedidos
- Platillos mas vendidos
- Categorias unicas de restaurantes y articulos

---

## Endpoints principales de la API

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| POST | `/api/Auth/registro` | Registrar nuevo usuario |
| POST | `/api/Auth/login` | Iniciar sesion y obtener token |
| GET | `/api/Restaurantes` | Listar restaurantes |
| POST | `/api/Restaurantes` | Crear restaurante |
| PUT | `/api/Restaurantes/{id}` | Editar restaurante |
| DELETE | `/api/Restaurantes/{id}` | Eliminar restaurante |
| GET | `/api/Articulos` | Listar articulos |
| GET | `/api/Pedidos` | Listar pedidos |
| POST | `/api/Pedidos` | Crear pedido |
| GET | `/api/Resenas` | Listar resenas |
| POST | `/api/Resenas` | Crear resena (transaccional) |
| GET | `/api/Reportes/top-restaurantes` | Top restaurantes |
| GET | `/api/Reportes/platillos-mas-vendidos` | Platillos mas vendidos |
| GET | `/api/Agregaciones/estadisticas/generales` | Estadisticas generales |
| POST | `/api/Seed/generar-datos` | Generar datos de prueba |
| DELETE | `/api/Seed/limpiar-datos` | Eliminar todos los datos |

La documentacion interactiva completa esta disponible en Swagger al ejecutar la API en modo desarrollo.
