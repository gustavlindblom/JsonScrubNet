using System.Text.Json;

if (args.Length == 0)
{
    Console.Error.WriteLine("ERROR: No input file specified");
    Console.WriteLine("USAGE: jsscrub <input file> <output file>");
    return;
}

if (!File.Exists(args[0]))
{
    Console.Error.WriteLine("ERROR: file {0} does not exist.", args[0]);
    return;
}

if (args.Length == 1)
{
    Console.Error.WriteLine("ERROR: No output file specified");
    Console.WriteLine("USAGE: jsscrub <input file> <output file>");
    return;
}

const string replacementStringCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
using var input = File.Open(args[0], FileMode.Open, FileAccess.Read);
using var output = File.Open(args[1], FileMode.OpenOrCreate, FileAccess.Write);

var buffer = new byte[input.Length];
var bytes = input.Read(buffer, 0, (int)input.Length);

if (bytes > 0)
{
    Console.WriteLine("INFO: Scrubbing json values in {0}", input.Name);
    var readOptions = new JsonReaderOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };
    var writeOptions = new JsonWriterOptions()
    {
        Indented = true,
    };

    var reader = new Utf8JsonReader(buffer.AsSpan<byte>(), readOptions);
    using var stream = new MemoryStream();
    using var writer = new Utf8JsonWriter(stream, writeOptions);
    while (reader.Read())
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.GetString() ?? "NN");
                    break;
                case JsonTokenType.String:
                    int valueLength = reader.HasValueSequence
                        ? checked((int)reader.ValueSequence.Length)
                        : reader.ValueSpan.Length;
                    string replacement = string.Join("", Enumerable.Range(0, valueLength)
                        .Select(i => replacementStringCharacters[Random.Shared.Next(replacementStringCharacters.Length)]));
                    writer.WriteStringValue(replacement);
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out int intValue))
                    {
                        writer.WriteNumberValue(Random.Shared.Next(intValue));
                    }
                    else if (reader.TryGetDouble(out double doubleValue))
                    {
                        writer.WriteNumberValue(Random.Shared.NextDouble() * doubleValue);
                    }
                    break;
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;
                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;
                case JsonTokenType.True:
                case JsonTokenType.False:
                    writer.WriteBooleanValue(reader.GetBoolean());
                    break;
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;
                default:
                    break;
            }
        }
        catch (InvalidOperationException exception)
        {

            Console.Error.WriteLine(
                "ERROR: Unable to parse JSON in input file at position {0}, depth {1}\n{2}",
                reader.TokenStartIndex,
                reader.CurrentDepth,
                exception.Message);
            return;
        }
    }
    writer.Flush();
    stream.WriteTo(output);
    Console.WriteLine("INFO: Wrote {0} bytes to {1}", output.Length, output.Name);
}
