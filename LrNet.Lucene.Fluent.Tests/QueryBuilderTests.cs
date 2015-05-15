using LrNet.Lucene.Fluent;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using NUnit.Framework;

namespace UnitTestProject1
{
    [TestFixture]
    class QueryBuilderTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            QueryBuilder.Version = Version.LUCENE_29;
            QueryBuilder.DefaultAnalyzer = new StandardAnalyzer(QueryBuilder.Version);
        }
        private void Expect(QueryBuilder builder, string expected)
        {
            Assert.AreEqual(expected, builder.Query.ToString());
        }

        [Test]
        public void fluent_interface_basic()
        {
            Expect(
                new QueryBuilder()
                    .MatchTerm("foo", "bar")
                    .MatchTerm("baz", "boo"),
                "+foo:bar +baz:boo"
            );

            Expect(
                new QueryBuilder()
                    .MatchTerm("foo", "bar")
                    .Should
                    .MatchTerm("baz", "boo"),
                "+foo:bar baz:boo"
            );

            Expect(
                new QueryBuilder()
                    .MatchTerm("foo", "bar")
                    .MustNot
                    .MatchTerm("baz", "boo"),
                "+foo:bar -baz:boo"
            );

            Expect(
                new QueryBuilder()
                    .MatchTerm("foo", "bar")
                    .MustNot
                    .MatchTerm("baz", "boo")
                    .MatchTerm("fee", "fum"),
                "+foo:bar -baz:boo -fee:fum"
            );
        }

        [TestCase("foo", "bar", Result = "+foo:bar")]
        public string match_term(string field, string value)
        {
            return
                new QueryBuilder()
                    .MatchTerm(field, value)
                    .Query
                    .ToString();
        }


        [TestCase("foo", "bar,baz", Result = "+(+foo:bar +foo:baz)")]
        [TestCase("foo", "bar,baz,boo", Result = "+(+foo:bar +foo:baz +foo:boo)")]
        public string match_all_terms(string field, string values)
        {
            return
                new QueryBuilder()
                    .MatchAllTerms(field, values.Split(','))
                    .Query
                    .ToString();
        }

        [TestCase("foo", "bar,baz", Result = "+(foo:bar foo:baz)")]
        [TestCase("foo", "bar,baz,boo", Result = "+(foo:bar foo:baz foo:boo)")]
        public string match_any_terms(string field, string values)
        {
            return
                new QueryBuilder()
                    .MatchAtLeastOneTerm(field, values.Split(','))
                    .Query
                    .ToString();
        }

        [TestCase("foo", "bar,baz", Result = "+(-foo:bar -foo:baz)")]
        [TestCase("foo", "bar,baz,boo", Result = "+(-foo:bar -foo:baz -foo:boo)")]
        public string match_no_terms(string field, string values)
        {
            return
                new QueryBuilder()
                    .MatchNoneOfTheTerms(field, values.Split(','))
                    .Query
                    .ToString();
        }


        [Test]
        public void match_sub_query()
        {
            Expect(
                new QueryBuilder()
                    .MatchTerm("foo", "bar")
                    .MatchSubQuery(b => b
                        .Should
                        .MatchTerm("fee", "fum")
                        .MatchTerm("lee", "da")),
                "+foo:bar +(fee:fum lee:da)"
            );
        }


        [Test]
        public void for_each()
        {
            Expect(
                new QueryBuilder()
                    .ForEach(new[] { "bar", "baz", "blip" }, (v, b) => b.MatchTerm("foo", v)),
                "+foo:bar +foo:baz +foo:blip"
            );
        }

        [TestCase("foo", "bar", Result = "+foo:bar")]
        [TestCase("foo", "bar:bap", Result = "+bar:bap")]
        [TestCase("foo", "AND what", Result = "+(foo:what)")]
        [TestCase("foo", "wh^addaya(mean", Result = "+foo:\"wh addaya mean\"")]
        public string match_parsed_input(string field, string value)
        {
            return new QueryBuilder()
                    .MatchParsedInput(field, value)
                    .Query
                    .ToString();
        }

    }
}
