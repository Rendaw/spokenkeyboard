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
using System.Collections;

namespace SpokenKeyboard
{
    public abstract class Data
    {
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
            var json = Program.toJson(response).ToString(Newtonsoft.Json.Formatting.None);
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
            //Console.WriteLine("hey {0}", text);
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
                //Console.WriteLine("word {3} {0}, v {1} m {2}", number, value, minor, word);

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

    class Program
    {
        public static SpeechRecognitionEngine se;
        public static StreamWriter writer;
        public const string InternalMarker = "_";

        private static Dictionary<string, System.Type> derived;
        static Program()
        {
            derived = (
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where assemblyType.IsSubclassOf(typeof(Data))
                select assemblyType
            ).ToDictionary(x => x.Name, x => x);
        }

        public static object fromJsonProperty(Type type, JToken val)
        {
            if (type == typeof(string)) return (string)val;
            else if (type == typeof(long)) return (Int64)val;
            else if (type == typeof(bool)) return (bool)val;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (IList)Activator.CreateInstance(type);
                foreach (var element in (JContainer)val)
                    list.Add(fromJsonProperty(type.GenericTypeArguments[0], element));
                return list;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dict = (IDictionary)Activator.CreateInstance(type);
                foreach (var element in (JObject)val)
                    dict.Add(element.Key, fromJsonProperty(type.GenericTypeArguments[1], element.Value));
                return dict;
            }
            else if (val is JObject) return fromJson((JObject)val);
            else throw new NotImplementedException("Cannot deserialize " + type.Name);
        }

        public static object fromJson(JObject source)
        {
            var typeProp = source.Property("type");
            if (typeProp == null)
                throw new ArgumentException(String.Format("[type] missing at {0}", source.Path));
            var typeName = (string)typeProp.Value;
            if (!derived.ContainsKey(typeName))
                throw new ArgumentException("Unknown message type [" + typeName + "]");
            source.Remove("type");
            var type = derived[typeName];
            var target = Activator.CreateInstance(type);
            foreach (var prop in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (source.Property(prop.Name) == null)
                    throw new ArgumentNullException(String.Format("Missing [{0}] in [{1}] at [{2}]", prop.Name, type, source.Path));
                var val = source.Property(prop.Name).Value;
                source.Remove(prop.Name);
                prop.SetValue(target, fromJsonProperty(prop.FieldType, val));
            }
            var remaining = source.Properties().Select(p => p.Name).ToList();
            if (remaining.Count > 0)
                Console.WriteLine(String.Format("WARNING: Unknown properties ({0}) in message", String.Join(", ", remaining)));
            return target;
        }

        public static JToken toJsonProperty(Type type, object val)
        {
            if (type == typeof(string)) return new JValue((string)val);
            else if (type == typeof(long)) return new JValue((Int64)val);
            else if (type == typeof(bool)) return new JValue((bool)val);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return new JArray(
                    (
                        from object element in (IList)val
                        select toJsonProperty(type.GetGenericArguments()[0], element)
                    ).ToArray());
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var obj = new JObject();
                var elements = ((IDictionary)val).GetEnumerator();
                while (elements.MoveNext())
                    obj[(string)elements.Key] = toJsonProperty(type.GetGenericArguments()[1], elements.Value);
                return obj;
            }
            else return toJson(val);
        }

        public static JObject toJson(object source)
        {
            var dest = new JObject();
            var type = source.GetType();
            foreach (var prop in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                dest[prop.Name] = toJsonProperty(prop.FieldType, prop.GetValue(source));
            dest["type"] = type.Name;
            return dest;
        }

        static void Main(string[] args)
        {
            Uri address = new Uri("tcp://0.0.0.0:21147", UriKind.Absolute);
            if (args.Length >= 1)
            {
                address = new Uri("tcp://" + args[0], UriKind.Absolute);
            }
            else
            {
                Console.WriteLine("Using default address (Specify address as first argument)");
            }

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
                    Console.WriteLine(x);
                }
            };

            Console.WriteLine("Examples:");
            Process(toJson(new NewGrammar
            {
                name = "test",
                rules = new Dictionary<string, Rule>
                {
                    { "void", new VoidRule { start = "testing void", } },
                    { "string", new SingleStringRule { start = "testing string", } },
                    { "integer", new SingleIntegerRule { start = "testing integer", } },
                },
                always = false,
            }).ToString(Newtonsoft.Json.Formatting.None));
            Process(toJson(new SwitchGrammar
            {
                name = "test",
            }).ToString(Newtonsoft.Json.Formatting.None));

            se.RecognizeAsync(RecognizeMode.Multiple);

            var server = new TcpListener(IPAddress.Parse(address.Host), address.Port);
            server.Start();
            Console.WriteLine("Listening on " + address + "...");
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
                            Process(line);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static void Process(string line)
        {
            Console.WriteLine("Request: {0}", line);
            var request = (Request)fromJson(JObject.Parse(line));
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
