using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MoneyManager.Domain.Entities;

/// <summary>
/// Arquivo (já normalizado) de uma importação de fatura: JPEG, PDF original ou texto da planilha.
/// Serve só de insumo para a IA: é apagado fisicamente ao fim do processamento e, como garantia,
/// expira em 24 horas pelo índice TTL sobre <see cref="CreatedAt"/>.
/// </summary>
[BsonIgnoreExtraElements]
public class CreditCardInvoiceImportFile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("importId")]
    public string ImportId { get; set; } = string.Empty;

    // Ordem do arquivo no upload (0 = primeiro).
    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [BsonElement("sizeBytes")]
    public long SizeBytes { get; set; }

    [BsonElement("content")]
    public byte[] Content { get; set; } = [];

    // Base do índice TTL de 24 horas.
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
