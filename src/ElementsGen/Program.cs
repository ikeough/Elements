using Hypar.Elements;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ElementsGen
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine("The first argument must be the path to the elements schema.");
            }

            if(!File.Exists(args[0]))
            {
                Console.WriteLine("The specified path to the elements schema does not exist.");
            }

            var schema = await JsonSchema.FromFileAsync(args[0]);
            var csGenerator = new CSharpGenerator(schema, new CSharpGeneratorSettings(){
                Namespace = "Hypar.Elements"
            });
            var csFile = csGenerator.GenerateFile();
            File.WriteAllText("../../../Elements.g.cs", csFile);

            var element = new Element(){
                Id = Guid.NewGuid().ToString()
            };
            
            var pset = new PropertySet();
            pset.Add("prop1", new NumericProperty(){
                ValueType = NumericPropertyValueType.Force,
                Value = 5.0
            });
            pset.Add("prop2", new StringProperty(){
                ValueType = StringPropertyValueType.Date,
                Value = DateTime.Now.ToString()
            });
            element.Components.Add("PropertySet", pset);
            var json = JsonConvert.SerializeObject(element);
            Console.WriteLine(json);
        }
    }
}
