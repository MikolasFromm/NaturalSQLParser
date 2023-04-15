using System;
using System.Data;
using NaturalSQLParser.Query;
using OpenAI_API;

namespace NaturalSQLParser
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var prc = new Processor();
            await prc.AIRequestTest();
            //prc.CreateQuery();
        }
    }
}