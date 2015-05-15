using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace LrNet.Lucene.Fluent
{
    internal static class QueryParserExtensions
    {
        /// <summary>
        /// This method uses the analyzer to return a safe string of tokens that should 
        /// be safe to use in a query parser. This is the ideal string to pass to a query parser
        /// if you assume that the user is not using any query syntax (like fields, operators, fuzziness, etc)
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Query FromUserInput(this QueryParser parser, string text)
        {
            var analyzer = parser.GetAnalyzer();
            var parseable = string.Join(" ", analyzer.TokenListFromString(text));
            return parser.Parse(parseable);
        }

        public static Query FromUserInput(string field, string text, Version version)
        {
            var parser = new QueryParser(version, field, QueryBuilder.DefaultAnalyzer);
            return parser.FromUserInput(text);
        }

        public static string Tokenize(this Analyzer analyzer, string input)
        {
            return string.Join(" ", analyzer.TokenListFromString(input));
        }

        public static IEnumerable<string> TokenListFromString(this Analyzer analyzer, string text)
        {
            var stream = analyzer.TokenStream(null, new StringReader(text));
            var termAttr = (TokenWrapper)stream.GetAttribute(typeof(TermAttribute));
            while (stream.IncrementToken())
            {
                yield return termAttr.Term();
            }

            //NOTE: When we upgrate to version 3, we will want to use this instead:
            //var termAttr = tokenStream.GetAttribute<Lucene.Net.Analysis.Tokenattributes.ITermAttribute>();
            //while (tokenStream.IncrementToken())
            //{
            //    string term = termAttr.Term;
            //}
        }

    }
}
