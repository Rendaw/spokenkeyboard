using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Speech.Recognition;
using System.Threading.Tasks;
using System.Reflection;
using System.Speech.Recognition.SrgsGrammar;
using System.Xml;
using SpokenKeyboard;

namespace SpokenKeyboard
{
    public class Context
    {

    }

    public abstract class Data
    {
        public string type;
        public Data()
        {
            type = this.GetType().Name;
        }
    }

    public abstract class Response : Data
    {
        public string name;
    }

    public class VoidResponse : Response
    {
    }

    public class SingleStringResponse : Response
    {
        public string data;
    }

    public class SingleIntegerResponse : Response
    {
        public long data;
    }

    public abstract class Request : Data
    {
        public abstract void Invoke();
    }

    public class NewGrammar : Request
    {
        public string name;
        public bool always;
        public Dictionary<string, Rule> rules;

        private Grammar grammar;

        private static NewGrammar loaded;
        private static Dictionary<string, NewGrammar> grammars = new Dictionary<string, NewGrammar>();
        private static Dictionary<Grammar, NewGrammar> grammars2 = new Dictionary<Grammar, NewGrammar>();

        public override void Invoke()
        {
            if (grammars.ContainsKey(name))
            {
                var other = grammars[name];
                if (loaded == other)
                {
                    Program.se.UnloadGrammar(other.grammar);
                    loaded = null;
                }
                else if (other.always)
                    Program.se.UnloadGrammar(other.grammar);
                grammars2.Remove(other.grammar);
                grammars.Remove(other.name);
            }
            var doc = new SrgsDocument(new XmlTextReader(new MemoryStream(Properties.Resources.numbers)));
            var alt = new SrgsOneOf();
            foreach (var rule in rules)
                alt.Add(new SrgsItem(new SrgsElement[] { rule.Value.Build(doc), new SrgsNameValueTag(rule.Key) }));
            var root = new SrgsRule(Program.InternalMarker + "root", alt);
            doc.Rules.Add(root);
            doc.Root = root;
            doc.Debug = true;
            using (var writer = new XmlTextWriter("out.xml", null))
            {
                doc.WriteSrgs(writer);
            }
            grammar = new Grammar(doc);
            grammars.Add(name, this);
            grammars2.Add(grammar, this);
            Console.WriteLine("Created grammar " + name);
            if (always)
            {
                Console.WriteLine("Loading grammar " + name);
                Program.se.LoadGrammarAsync(grammar);
            }
        }

        internal void Load()
        {
            if (always) return;
            Console.WriteLine("Loading grammar " + name);
            Program.se.LoadGrammarAsync(grammar);
            loaded = this;
        }

        internal static void Load(string name)
        {
            if (!grammars.ContainsKey(name)) return;
            if (loaded != null)
                Program.se.UnloadGrammar(loaded.grammar);
            grammars[name].Load();
        }

        internal static void Parse(SpeechRecognizedEventArgs e)
        {
            var name = (string)e.Result.Semantics.Value;
            var grammar = grammars2[e.Result.Grammar];
            Response response = grammar.rules[name].Parse(name, e);
            var json = JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            var writer = Program.writer;
            if (writer != null)
                writer.WriteAsync(json);
        }
    }

    public class SwitchGrammar : Request
    {
        public string name;

        public override void Invoke()
        {
            NewGrammar.Load(name);
        }
    }

    public abstract class Rule : Data
    {
        public string start;
        public abstract SrgsItem Build(SrgsDocument doc);
        public abstract Response Parse(string name, SpeechRecognizedEventArgs e);
    }

    public class VoidRule : Rule
    {
        public override SrgsItem Build(SrgsDocument doc)
        {
            return new SrgsItem(start);
        }

        public override Response Parse(string name, SpeechRecognizedEventArgs e)
        {
            return new VoidResponse
            {
                name = name,
            };
        }
    }

    public class SingleIntegerRule : Rule
    {
        private static Dictionary<string, long> digits;

        static SingleIntegerRule()
        {
            digits = new Dictionary<string, long>();
            Func<string, long, int> token = delegate (string name, long value)
            {
                digits.Add(name, value);
                return 0;
            };
            token("zero", 0);
            token("one", 1);
            token("two", 2);
            token("three", 3);
            token("four", 4);
            token("five", 5);
            token("six", 6);
            token("seven", 7);
            token("eight", 8);
            token("nine", 9);
            token("oh one", 1);
            token("oh two", 2);
            token("oh three", 3);
            token("oh four", 4);
            token("oh five", 5);
            token("oh six", 6);
            token("oh seven", 7);
            token("oh eight", 8);
            token("oh nine", 9);
            token("ten", 10);
            token("eleven", 11);
            token("twelve", 12);
            token("thirteen", 13);
            token("fourteen", 14);
            token("fifteen", 15);
            token("sixteen", 16);
            token("seventeen", 17);
            token("eighteen", 18);
            token("nineteen", 19);
            token("twenty", 20);
            token("thirty", 30);
            token("forty", 40);
            token("fifty", 50);
            token("sixty", 60);
            token("seventy", 70);
            token("eighty", 80);
            token("ninety", 90);
            token("hundred", 100);
            token("thousand", 1000);
            token("million", 1000000);
            token("billion", 1000000000);
            token("trillion", 1000000000000);
            string tmp = System.IO.Packaging.PackUriHelper.UriSchemePack;
        }

        public override SrgsItem Build(SrgsDocument doc)
        {
            return new SrgsItem(
                new SrgsElement[]
                {
                    new SrgsItem(start),
                    //new SrgsRuleRef(new Uri("numbers.grxml", UriKind.Relative)),
                    new SrgsRuleRef(doc.Rules["positiveIntegers"]),
                });
        }

        internal static long Parse(string text)
        {
            long value = 0;
            long minor = 0;
            Console.WriteLine("hey {0}", text);
            bool oh = false;
            foreach (var word in text.Split(' '))
            {
                if (word == "and") continue;

                if (word == "oh")
                {
                    oh = true;
                    continue;
                }

                var number = digits[word];
                if (number >= 1000)
                {
                    value += minor * number;
                    minor = 0;
                }
                else if (number >= 100)
                {
                    minor = minor * number;
                }
                else if (oh || (number >= 10))
                {
                    minor = minor * 100 + number;
                }
                else
                {
                    minor += number;
                }
                Console.WriteLine("word {3} {0}, v {1} m {2}", number, value, minor, word);

                oh = false;
            }
            value += minor;
            return value;
        }

        public override Response Parse(string name, SpeechRecognizedEventArgs e)
        {
            return new SingleIntegerResponse
            {
                name = name,
                data = Parse(e.Result.Text.Substring(start.Length + 1)),
            };
        }
    }

    public class SingleStringRule : Rule
    {
        public override SrgsItem Build(SrgsDocument doc)
        {
            return new SrgsItem(
                new SrgsElement[]
                {
                    new SrgsItem(start),
                    new SrgsRuleRef(new Uri("grammar:dictation")),
                });
        }

        public override Response Parse(string name, SpeechRecognizedEventArgs e)
        {
            return new SingleStringResponse
            {
                name = name,
                data = e.Result.Text.Substring(start.Length + 1),
            };
        }
    }

    public class JsonConverter : Newtonsoft.Json.Converters.CustomCreationConverter<Request>
    {
        public Dictionary<string, System.Type> derived;
        public JsonConverter()
        {
            derived = (
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where assemblyType.IsSubclassOf(typeof(Data))
                select assemblyType
            ).ToDictionary(x => x.Name, x => x);
        }

        public override Request Create(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject source = JObject.Load(reader);
            var target = derived[source.Property("type").ToString()].GetConstructor(Type.EmptyTypes).Invoke(new object[] { });
            serializer.Populate(source.CreateReader(), target);
            return target;
        }
    }

    class Program
    {
        public static SpeechRecognitionEngine se;
        public static StreamWriter writer;
        public const string InternalMarker = "_";

        static void Main(string[] args)
        {
            se = new SpeechRecognitionEngine();
            se.SetInputToDefaultAudioDevice();
            se.SpeechRecognized += delegate (object sender, SpeechRecognizedEventArgs e)
            {
                try
                {
                    NewGrammar.Parse(e);
                }
                catch (Exception x)
                {
                    Console.WriteLine(e);
                }
            };
            new NewGrammar
            {
                name = "test",
                rules = new Dictionary<string, Rule>
                {
                    { "void", new VoidRule { start = "testing void", } },
                    { "string", new SingleStringRule { start = "testing string", } },
                    { "integer", new SingleIntegerRule { start = "testing integer", } },
                },
                always = false,
            }.Invoke();
            new SwitchGrammar
            {
                name = "test",
            }.Invoke();
            se.RecognizeAsync(RecognizeMode.Multiple);

            var server = new TcpListener(IPAddress.Parse("0.0.0.0"), 21147);
            var converter = new JsonConverter();
            server.Start();
            Console.WriteLine("Listening...");
            while (true)
            {
                try
                {
                    using (var client = server.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        Console.WriteLine("Client connected...");
                        var reader = new StreamReader(stream, Encoding.UTF8);
                        Program.writer = new StreamWriter(stream, new UTF8Encoding(false));
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            Console.WriteLine("Request: {0}", line);
                            var request = JsonConvert.DeserializeObject<Request>(line, converter);
                            try
                            {
                                request.Invoke();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
