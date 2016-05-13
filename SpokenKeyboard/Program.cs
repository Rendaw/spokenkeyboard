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
        public abstract void invoke();
    }

    public class NewGrammar : Request
    {
        public string name;
        public bool always;
        public List<Rule> rules;

        private Grammar grammar;

        public override void invoke()
        {
            if (Program.grammars.ContainsKey(name))
            {
                Program.se.UnloadGrammar(Program.grammars[name].grammar);
                Program.grammars.Remove(name);
            }
            grammar = new Grammar(new GrammarBuilder(new Choices(
                (from rule in rules select rule.build()).ToArray()
            )));
            Program.grammars.Add(name, this);
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
            Program.loaded = grammar;
        }
    }

    public class SwitchGrammar : Request
    {
        public string name;

        public override void invoke()
        {
            if (!Program.grammars.ContainsKey(name)) return;
            if (Program.loaded != null)
                Program.se.UnloadGrammar(Program.loaded);
            Program.grammars[name].Load();
        }
    }

    public abstract class Rule : Data
    {
        public string name;
        public string start;
        public abstract GrammarBuilder build();
    }

    public class VoidRule : Rule
    {
        public override GrammarBuilder build()
        {
            return new SemanticResultKey(name, new GrammarBuilder(start));
        }
    }

    public class SingleIntegerRule : Rule
    {
        private static GrammarBuilder positiveIntegers;
        private static Dictionary<string, long> digits;

        static SingleIntegerRule()
        {
            Dictionary<Func<GrammarBuilder>, GrammarBuilder> cache = new Dictionary<Func<GrammarBuilder>, GrammarBuilder>();
            Func<Func<GrammarBuilder>, Func<GrammarBuilder>> wrap = delegate (Func<GrammarBuilder> source)
            {
                return delegate ()
                {
                    if (cache.ContainsKey(source)) return cache[source];
                    var prod = source();
                    cache.Add(source, prod);
                    return prod;
                };
            };

            digits = new Dictionary<string, long>();
            Func<string, long, Func<GrammarBuilder>> token = delegate (string name, long value)
            {
                digits.Add(name, value);
                return wrap(delegate () { return new GrammarBuilder(name); });
            };
            var zero = token("zero", 0);
            var one = token("one", 1);
            var two = token("two", 2);
            var three = token("three", 3);
            var four = token("four", 4);
            var five = token("five", 5);
            var six = token("six", 6);
            var seven = token("seven", 7);
            var eight = token("eight", 8);
            var nine = token("nine", 9);
            var ohOne = token("oh one", 1);
            var ohTwo = token("oh two", 2);
            var ohThree = token("oh three", 3);
            var ohFour = token("oh four", 4);
            var ohFive = token("oh five", 5);
            var ohSix = token("oh six", 6);
            var ohSeven = token("oh seven", 7);
            var ohEight = token("oh eight", 8);
            var ohNine = token("oh nine", 9);
            var ten = token("ten", 10);
            var eleven = token("eleven", 11);
            var twelve = token("twelve", 12);
            var thirteen = token("thirteen", 13);
            var fourteen = token("fourteen", 14);
            var fifteen = token("fifteen", 15);
            var sixteen = token("sixteen", 16);
            var seventeen = token("seventeen", 17);
            var eighteen = token("eighteen", 18);
            var nineteen = token("nineteen", 19);
            var twenty = token("twenty", 20);
            var thirty = token("thirty", 30);
            var forty = token("forty", 40);
            var fifty = token("fifty", 50);
            var sixty = token("sixty", 60);
            var seventy = token("seventy", 70);
            var eighty = token("eighty", 80);
            var ninety = token("ninety", 90);
            var hundred = token("hundred", 100);
            var thousand = token("thousand", 1000);
            var million = token("million", 1000000);
            var billion = token("billion", 1000000000);
            var trillion = token("trillion", 1000000000000);

            Func<GrammarBuilder> ones = delegate ()
            {
                return new Choices(new GrammarBuilder[]
                {
                    one(),
                    two(),
                    three(),
                    four(),
                    five(),
                    six(),
                    seven(),
                    eight(),
                    nine(),
                });
            };
            Func<GrammarBuilder> optOnes = wrap(delegate ()
            {
                return new GrammarBuilder(ones(), 0, 1);
            });

            Func<GrammarBuilder> ohOnes = wrap(delegate ()
            {
                return new Choices(new GrammarBuilder[]
                {
                    ohOne(),
                    ohTwo(),
                    ohThree(),
                    ohFour(),
                    ohFive(),
                    ohSix(),
                    ohSeven(),
                    ohEight(),
                    ohNine(),
                });
            });
            Func<GrammarBuilder> optOhOnes = wrap(delegate ()
            {
                return new GrammarBuilder(ohOnes(), 0, 1);
            });

            Func<GrammarBuilder> lowTens = wrap(delegate ()
            {
                return new Choices(new GrammarBuilder[]
                {
                    ten(),
                    eleven(),
                    twelve(),
                    thirteen(),
                    fourteen(),
                    fifteen(),
                    sixteen(),
                    seventeen(),
                    eighteen(),
                    nineteen(),
                });
            });

            Func<GrammarBuilder> highTens = wrap(delegate ()
            {
                return new Choices(new GrammarBuilder[]
                {
                    twenty(),
                    thirty(),
                    forty(),
                    fifty(),
                    sixty(),
                    seventy(),
                    eighty(),
                    ninety(),
                });
            });

            /*Func<GrammarBuilder> tens = wrap(delegate ()
            {
                return new GrammarBuilder(new Choices(new GrammarBuilder[] {
                    lowTens(),
                    highTens()
                }));
            });
            Func<GrammarBuilder> optTens = wrap(delegate ()
            {
                return new GrammarBuilder(tens(), 0, 1);
            });*/

            Func<GrammarBuilder> twoDigits = wrap(delegate ()
            {
                return new GrammarBuilder(new Choices(new GrammarBuilder[] {
                    ohOnes(),
                    lowTens(),
                    highTens() + optOnes()
                }));
            });
            Func<GrammarBuilder> optTwoDigits = wrap(delegate ()
            {
                return new GrammarBuilder(twoDigits(), 0, 1);
            });

            Func<GrammarBuilder> hundredsDown = wrap(delegate ()
            {
                return hundred() +
                    new GrammarBuilder("and", 0, 1) +
                    new GrammarBuilder(new Choices(new GrammarBuilder[] {
                        ones(),
                        lowTens(),
                        highTens() + optOnes()
                    }), 0, 1);
            });
            Func<GrammarBuilder> optHundredsDown = wrap(delegate ()
            {
                return new GrammarBuilder(hundredsDown(), 0, 1);
            });

            /*
            Func<GrammarBuilder> minor = wrap(delegate ()
            {
                return ones() + optHundredsDown();
            });

            Func<GrammarBuilder> thousandsDown = wrap(delegate ()
            {
                return thousand() + new GrammarBuilder(minor(), 0, 1);
            });
            Func<GrammarBuilder> optThousandsDown = wrap(delegate ()
            {
                return new GrammarBuilder(thousandsDown(), 0, 1);
            });

            Func<GrammarBuilder> millionsDown = wrap(delegate ()
            {
                return new GrammarBuilder(new Choices(new GrammarBuilder[]
                {
                    million() + new GrammarBuilder(minor() + optThousandsDown(), 0, 1),
                    thousandsDown(),
                }));
            });
            Func<GrammarBuilder> optMillionsDown = wrap(delegate ()
            {
                return new GrammarBuilder(millionsDown(), 0, 1);
            });

            Func<GrammarBuilder> billionsDown = wrap(delegate ()
            {
                return new GrammarBuilder(new Choices(new GrammarBuilder[]
                {
                    billion() + new GrammarBuilder(minor() + optMillionsDown(), 0, 1),
                    millionsDown(),
                }));
            });
            Func<GrammarBuilder> optBillionsDown = wrap(delegate ()
            {
                return new GrammarBuilder(billionsDown(), 0, 1);
            });

            Func<GrammarBuilder> trillionsDown = wrap(delegate ()
            {
                return new GrammarBuilder(new Choices(new GrammarBuilder[]
                {
                    trillion() + new GrammarBuilder(minor() + optBillionsDown(), 0, 1),
                    billionsDown(),
                }));
            });
            Func<GrammarBuilder> optTrillionsDown = wrap(delegate ()
            {
                return new GrammarBuilder(trillionsDown(), 0, 1);
            });
            */

            /*positiveIntegers = new GrammarBuilder(new Choices(new GrammarBuilder[] {
                zero(),
                new GrammarBuilder(new Choices(new GrammarBuilder[]
                {
                    ones(),
                    lowTens(),
                    highTens() + optOnes()
                })) +
                new GrammarBuilder(new Choices(new GrammarBuilder[]
                {
                    //optTrillionsDown(),
                    optTwoDigits()
                })),
            }));*/
            positiveIntegers = new GrammarBuilder();
            string tmp = System.IO.Packaging.PackUriHelper.UriSchemePack;
            positiveIntegers.AppendRuleReference(
                "pack://application:,,,/numbers.grxml",
                "positiveIntegers"
            );
        }

        public override GrammarBuilder build()
        {
            return new SemanticResultKey(
                name, 
                new GrammarBuilder(start) +
                new SemanticResultKey(
                    "singleinteger", 
                    positiveIntegers
                )
            );
        }

        internal static long Parse(string text)
        {
            long value = 0;
            long minor = 0;
            Console.WriteLine("hey {0}", text);
            bool oh = false;
            foreach (var word in text.Split(' '))
            {
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
    }

    public class SingleStringRule : Rule
    {
        public string prompt;
        public override GrammarBuilder build()
        {
            var data = new GrammarBuilder();
            data.AppendDictation();
            return new SemanticResultKey(
                name, 
                new GrammarBuilder(start) + new SemanticResultKey("singleword", data)
            );
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
        public static Dictionary<string, NewGrammar> grammars = new Dictionary<string, NewGrammar>();
        GrammarBuilder integerGrammar;
        public static SpeechRecognitionEngine se;
        public static StreamWriter writer;
        internal static Grammar loaded;

        static Program()
        {

        }

        static void Main(string[] args)
        {
            Program.se = new SpeechRecognitionEngine();
            Program.se.SetInputToDefaultAudioDevice();
            Program.se.SpeechRecognized += SpeechRecognized;
            new NewGrammar
            {
                name = "test",
                rules = new List<Rule>
                {
                    new VoidRule { name = "void", start = "testing void", },
                    new SingleStringRule { name = "string", start = "testing string", },
                    new SingleIntegerRule { name = "integer", start = "testing integer", },
                },
                always = false,
            }.invoke();
            new SwitchGrammar
            {
                name = "test",
            }.invoke();
            Program.se.RecognizeAsync(RecognizeMode.Multiple);

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
                            request.invoke();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            var outer = e.Result.Semantics.First();
            var name = outer.Key;
            Response response;
            if (outer.Value.ContainsKey("singlestring"))
            {
                var data = outer.Value["singlestring"].Value;
                response = new SingleStringResponse
                {
                    name = name,
                    data = (string)data,
                };
            }
            else if (outer.Value.ContainsKey("singleinteger"))
            {
                Console.WriteLine(outer.Value["singleinteger"]);
                response = new SingleIntegerResponse
                {
                    name = name,
                    data = SingleIntegerRule.Parse((string)outer.Value["singleinteger"].Value),
                };
            }
            else
            {
                response = new VoidResponse
                {
                    name = name,
                };
            }
            var json = JsonConvert.SerializeObject(response);
            Console.WriteLine(json);
            var writer = Program.writer;
            if (writer != null)
                writer.WriteAsync(json);
        }
    }
}
