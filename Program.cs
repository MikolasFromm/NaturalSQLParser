using System;
using System.Data;
using NaturalSQLParser.Types.Enums;
using NaturalSQLParser.Query;
using NaturalSQLParser.Types;

namespace NaturalSQLParser
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // MOCK DATA
            List<Field> dataSet = new List<Field>
            {
                new Field() { Header = new Header("FirstName", FieldDataType.String, 0), Data = new List<Cell>() { new Cell() { Content = "John", Index = 0 }, new Cell() { Content = "Jane", Index = 1 }, new Cell() { Content = "Tomas", Index = 2 } } },
                new Field() { Header = new Header("LastName", FieldDataType.String, 1), Data = new List<Cell>() { new Cell() { Content = "Lennon", Index = 0 }, new Cell() { Content = "Petrová", Index = 1 }, new Cell() { Content = "Fromm", Index = 2 } } },
                new Field() { Header = new Header("Age", FieldDataType.Number, 2), Data = new List<Cell>() { new Cell() { Content = "35", Index = 0 }, new Cell() { Content = "25", Index = 1 }, new Cell() { Content = "45", Index = 2 } } },
                new Field() { Header = new Header("Department", FieldDataType.String,3), Data = new List<Cell>() { new Cell() { Content = "IT", Index = 0 }, new Cell() { Content = "HR", Index = 1 }, new Cell() { Content = "CEO", Index = 2 } } }
            };

            var prc = new Processor();
            prc.Response = new List<EmptyField>(dataSet);
            //await prc.AIRequestTest();

            var transformations = prc.CreateQuery();
            var result = prc.MakeTransformations(transformations, dataSet);
            return;
        }
    }
}