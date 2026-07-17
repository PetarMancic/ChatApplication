using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication1.Models;

public static class ChannelTypes
{
    public const string Public = "public";
    public const string Private = "private";
    public const string Dm = "dm";
}

public record Channel(
    string Name,
    string Type,
    string OwnerId,
    List<string> Members,
    string? DmKey,
    DateTime CreatedAt)
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = ObjectId.GenerateNewId().ToString();
}
