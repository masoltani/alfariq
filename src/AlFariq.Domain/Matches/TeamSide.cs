using System.Text.Json.Serialization;

namespace AlFariq.Domain;

/// <summary>Les deux côtés d'un match (RG-COM-01), stockés en texte.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TeamSide>))]
public enum TeamSide
{
    A,
    B,
}
