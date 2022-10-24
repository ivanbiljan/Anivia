using System.Text.Json.Serialization;

namespace Victoria.Payloads; 

internal abstract class AbstractPayload {
    protected AbstractPayload(string op) {
        Op = op;
    }

    [JsonPropertyName("op")]
    public string Op { get; }
}