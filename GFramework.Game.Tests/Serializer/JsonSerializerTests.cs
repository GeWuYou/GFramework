using System.Globalization;
using Newtonsoft.Json;
using GameJsonSerializer = GFramework.Game.Serializer.JsonSerializer;

namespace GFramework.Game.Tests.Serializer;

[TestFixture]
public sealed class JsonSerializerTests
{
    [Test]
    public void Serialize_And_Deserialize_Should_RoundTrip_Object()
    {
        var serializer = new GameJsonSerializer();
        var original = new PlayerStateStub
        {
            Name = "Player1",
            Level = 7
        };

        var json = serializer.Serialize(original);
        var restored = serializer.Deserialize<PlayerStateStub>(json);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Name, Is.EqualTo("Player1"));
            Assert.That(restored.Level, Is.EqualTo(7));
        });
    }

    [Test]
    public void Serialize_Should_Honor_Injected_Settings()
    {
        var serializer = new GameJsonSerializer(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        });

        var json = serializer.Serialize(new OptionalStateStub
        {
            Name = "Configured",
            Description = null
        });

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain(Environment.NewLine));
            Assert.That(json, Does.Contain("\"Name\": \"Configured\""));
            Assert.That(json, Does.Not.Contain("Description"));
        });
    }

    [Test]
    public void Settings_And_Converters_Should_Expose_Live_Configuration_Instance()
    {
        var settings = new JsonSerializerSettings();
        var serializer = new GameJsonSerializer(settings);

        Assert.Multiple(() =>
        {
            Assert.That(serializer.Settings, Is.SameAs(settings));
            Assert.That(serializer.Converters, Is.SameAs(settings.Converters));
        });
    }

    [Test]
    public void Converters_Should_Be_Used_For_Serialization_And_Deserialization()
    {
        var serializer = new GameJsonSerializer();
        serializer.Converters.Add(new CoordinateStubConverter());

        var json = serializer.Serialize(new CoordinateStub { X = 3, Y = 9 });
        var restored = serializer.Deserialize<CoordinateStub>(json);

        Assert.Multiple(() =>
        {
            Assert.That(json, Is.EqualTo("\"3:9\""));
            Assert.That(restored.X, Is.EqualTo(3));
            Assert.That(restored.Y, Is.EqualTo(9));
        });
    }

    [Test]
    public void Deserialize_Should_Throw_With_Target_Type_Context_When_Json_Is_Invalid()
    {
        var serializer = new GameJsonSerializer();

        var exception =
            Assert.Throws<InvalidOperationException>(() => serializer.Deserialize<PlayerStateStub>("{invalid json}"));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain(typeof(PlayerStateStub).FullName));
            Assert.That(exception.InnerException, Is.Not.Null);
        });
    }

    [Test]
    public void Deserialize_With_Runtime_Type_Should_Throw_With_Target_Type_Context_When_Json_Is_Invalid()
    {
        var serializer = new GameJsonSerializer();

        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                serializer.Deserialize("{invalid json}", typeof(PlayerStateStub)));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain(typeof(PlayerStateStub).FullName));
            Assert.That(exception.InnerException, Is.Not.Null);
        });
    }

    [Test]
    public void Deserialize_With_Runtime_Type_Should_Return_Target_Runtime_Type()
    {
        var serializer = new GameJsonSerializer();

        var restored = serializer.Deserialize("{\"Name\":\"Runtime\",\"Level\":11}", typeof(PlayerStateStub));

        Assert.That(restored, Is.TypeOf<PlayerStateStub>());
        Assert.That(((PlayerStateStub)restored).Level, Is.EqualTo(11));
    }

    [Test]
    public void Serialize_With_Runtime_Type_Should_Allow_Null_Object()
    {
        var serializer = new GameJsonSerializer();

        var json = serializer.Serialize(null!, typeof(PlayerStateStub));

        Assert.That(json, Is.EqualTo("null"));
    }

    [Test]
    public void Deserialize_Should_Preserve_ArgumentException_For_Invalid_Input_Arguments()
    {
        var serializer = new GameJsonSerializer();

        var exception = Assert.Throws<ArgumentException>(() => serializer.Deserialize<PlayerStateStub>(string.Empty));

        Assert.That(exception, Is.Not.Null);
    }

    private sealed class PlayerStateStub
    {
        public string Name { get; set; } = string.Empty;

        public int Level { get; set; }
    }

    private sealed class OptionalStateStub
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    private sealed class CoordinateStub
    {
        public int X { get; set; }

        public int Y { get; set; }
    }

    private sealed class CoordinateStubConverter : JsonConverter<CoordinateStub>
    {
        public override void WriteJson(JsonWriter writer, CoordinateStub? value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value?.X}:{value?.Y}");
        }

        public override CoordinateStub ReadJson(
            JsonReader reader,
            Type objectType,
            CoordinateStub? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var raw = (string?)reader.Value ?? throw new JsonSerializationException("Coordinate value cannot be null.");
            var parts = raw.Split(':');
            return new CoordinateStub
            {
                X = int.Parse(parts[0], CultureInfo.InvariantCulture),
                Y = int.Parse(parts[1], CultureInfo.InvariantCulture)
            };
        }
    }
}
