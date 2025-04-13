using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

public class TenderApplicationEditConverter : JsonConverter<IFormFile>
{
    public override IFormFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Return null since we don't need to deserialize the file
        return null;
    }

    public override void Write(Utf8JsonWriter writer, IFormFile value, JsonSerializerOptions options)
    {
        // Skip serialization of the file
        writer.WriteNullValue();
    }
}