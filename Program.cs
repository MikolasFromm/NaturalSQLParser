using System;

using NaturalSQLParser.Query;

namespace NaturalSQLParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var prc = new Processor();
            prc.CreateQuery();
        }
    }
}