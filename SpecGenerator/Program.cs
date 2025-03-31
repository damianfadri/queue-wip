// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

var schemaRaw = await File.ReadAllTextAsync("C:\\Users\\dfadri\\source\\repos\\queue-wip\\schema.json");
var schema = JsonConvert.DeserializeObject<JObject>(schemaRaw);
var definitions = schema["definitions"];

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var specRaw = await File.ReadAllTextAsync("C:\\Users\\dfadri\\source\\repos\\queue-wip\\crd-eventjob.yaml");
var specYaml = deserializer.Deserialize(specRaw);
var spec = JObject.FromObject(specYaml);

for (var refs = spec.SelectTokens("$..$ref").ToList(); 
    refs.Count > 0; 
    refs = spec.SelectTokens("$..$ref").ToList())
{
    foreach (var refToken in refs)
    {
        var parent = refToken.Parent as JProperty;
        var grandparent = parent.Parent as JObject;

        if (parent != null && grandparent != null)
        {
            var refKey = refToken.Value<string>().Replace("#/definitions/", "");
            var replacement = definitions[refKey] as JObject;

            if (replacement != null)
            {
                parent.Remove();
                foreach (var prop in replacement.Properties())
                {
                    if (prop.Name.Contains("x-kubernetes"))
                    {
                        continue;
                    }
                    grandparent.TryAdd(prop.Name, prop.Value);
                }
            }
        }
    }
}

var xKubernetesKeys = spec.SelectTokens("$..*")
    .Select(value => value.Parent as JProperty)
    .Where(prop => prop != null)
    .Where(prop => prop.Name.Contains("x-kubernetes"))
    .ToList();

foreach (var key in xKubernetesKeys)
{
    key.Remove();
}

var jsonRaw = JsonConvert.SerializeObject(spec);
var complete = ConvertJTokenToObject(JsonConvert.DeserializeObject<JToken>(jsonRaw));


var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();

var yamlRaw = serializer.Serialize(complete);

//var specComplete = JsonConvert.SerializeObject(spec);
File.WriteAllText("C:\\Users\\dfadri\\source\\repos\\queue-wip\\crd-output.yaml", yamlRaw);

static object ConvertJTokenToObject(JToken token)
{
    if (token is JValue)
        return ((JValue)token).Value;
    if (token is JArray)
        return token.AsEnumerable().Select(ConvertJTokenToObject).ToList();
    if (token is JObject)
        return token.AsEnumerable().Cast<JProperty>().ToDictionary(x => x.Name, x => ConvertJTokenToObject(x.Value));
    throw new InvalidOperationException("Unexpected token: " + token);
}
// store all #/definitions

// input the spec with $ref

// check all instances of $ref
// for each $ref
// replace $ref from #/definitions