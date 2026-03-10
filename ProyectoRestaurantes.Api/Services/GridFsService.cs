using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace ProyectoRestaurantes.Api.Services;

public class GridFsService
{
    private readonly IGridFSBucket _bucket;

    public GridFsService(IMongoDatabase database)
    {
        _bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = "archivos_restaurante"
        });
    }

    // Subir archivo y retornar el ObjectId generado por GridFS
    public async Task<string> SubirArchivoAsync(Stream stream, string nombreArchivo, string contentType)
    {
        var opciones = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "contentType", contentType },
                { "uploadedAt", DateTime.UtcNow }
            }
        };

        var objectId = await _bucket.UploadFromStreamAsync(nombreArchivo, stream, opciones);
        return objectId.ToString();
    }

    // Descargar archivo por su ID
    public async Task<(Stream Stream, string FileName, string ContentType)> DescargarArchivoAsync(string id)
    {
        var objectId = ObjectId.Parse(id);
        var info = await _bucket.Find(Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, objectId))
                                .FirstOrDefaultAsync()
            ?? throw new Exception($"Archivo con ID '{id}' no encontrado.");

        var contentType = info.Metadata?.GetValue("contentType", "application/octet-stream").AsString
                          ?? "application/octet-stream";

        var stream = new MemoryStream();
        await _bucket.DownloadToStreamAsync(objectId, stream);
        stream.Position = 0;

        return (stream, info.Filename, contentType);
    }

    // Listar los archivos disponibles en el bucket
    public async Task<List<object>> ListarArchivosAsync(int skip = 0, int limit = 50)
    {
        var filtro = Builders<GridFSFileInfo>.Filter.Empty;
        var sort = Builders<GridFSFileInfo>.Sort.Descending(f => f.UploadDateTime);
        var opciones = new GridFSFindOptions { Sort = sort, Skip = skip, Limit = limit };
        var cursor = await _bucket.FindAsync(filtro, opciones);
        var archivos = await cursor.ToListAsync();

        return archivos.Select(f => (object)new
        {
            Id = f.Id.ToString(),
            Nombre = f.Filename,
            TamanioBytes = f.Length,
            FechaSubida = f.UploadDateTime,
            ContentType = f.Metadata?.GetValue("contentType", "application/octet-stream").AsString ?? "application/octet-stream"
        }).ToList();
    }

    // Eliminar un archivo por su ID
    public async Task EliminarArchivoAsync(string id)
    {
        var objectId = ObjectId.Parse(id);
        await _bucket.DeleteAsync(objectId);
    }

    // Verificar si un archivo existe
    public async Task<bool> ExisteAsync(string id)
    {
        var objectId = ObjectId.Parse(id);
        var cursor = await _bucket.FindAsync(
            Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, objectId),
            new GridFSFindOptions { Limit = 1 });
        return await cursor.AnyAsync();
    }
}
