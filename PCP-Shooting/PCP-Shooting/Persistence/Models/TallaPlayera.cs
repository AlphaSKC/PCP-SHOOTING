using System.Text.Json.Serialization;

namespace PCP_Shooting.Persistence.Models;

[JsonConverter(typeof(JsonStringEnumConverter<TallaPlayera>))]
public enum TallaPlayera { S = 0, M = 1, L = 2, XL = 3 }
