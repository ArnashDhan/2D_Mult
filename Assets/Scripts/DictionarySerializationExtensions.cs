using System.Collections.Generic;
using Unity.Netcode;

public static class DictionarySerializationExtensions
{
    // Serialize Dictionary<int, float>
    public static void WriteValueSafe(this FastBufferWriter writer, Dictionary<int, float> dictionary)
    {
        writer.WriteValueSafe(dictionary.Count);
        foreach (var kvp in dictionary)
        {
            writer.WriteValueSafe(kvp.Key);
            writer.WriteValueSafe(kvp.Value);
        }
    }

    // Deserialize Dictionary<int, float>
    public static void ReadValueSafe(this FastBufferReader reader, out Dictionary<int, float> dictionary)
    {
        dictionary = new Dictionary<int, float>();
        reader.ReadValueSafe(out int count);
        for (int i = 0; i < count; i++)
        {
            reader.ReadValueSafe(out int key);
            reader.ReadValueSafe(out float value);
            dictionary.Add(key, value);
        }
    }
}
