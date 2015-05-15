using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace LrNet.Lucene.Fluent
{
    public class QueryBuilder
    {
        public static Version Version { get; set; }
        public static Analyzer DefaultAnalyzer { get; set; }

        private readonly BooleanQuery _query;
        private Analyzer _analyzer;
        private BooleanClause.Occur _defaultOccur;

        public QueryBuilder()
        {
            _query = new BooleanQuery();
            _analyzer = DefaultAnalyzer;
            _defaultOccur = BooleanClause.Occur.MUST;
        }

        private QueryBuilder(BooleanQuery query, Analyzer analyzer, BooleanClause.Occur defaultOccur)
        {
            _query = query;
            _analyzer = analyzer;
            _defaultOccur = defaultOccur;
        }

        private QueryBuilder AddSubQuery(Query query, BooleanClause.Occur occur = null)
        {
            _query.Add(query, occur ?? _defaultOccur);
            return this;
        }

        private QueryBuilder AddSubQuery(QueryBuilder query, BooleanClause.Occur occur = null)
        {
            _query.Add(query.Query, occur ?? _defaultOccur);
            return this;
        }

        /// <summary>
        /// Returns this QueryBuilder with all following queries added to it using 
        /// BooleanClause.Occur.MUST. (this is effectively the "AND" operator)
        /// </summary>
        public QueryBuilder Must
        {
            get
            {
                _defaultOccur = BooleanClause.Occur.MUST;
                return this;
            }
        }

        /// <summary>
        /// Returns this QueryBuilder with all following queries added to it using 
        /// BooleanClause.Occur.MUST_NOT. (this is effectively the "AND NOT" operator)
        /// </summary>
        public QueryBuilder MustNot
        {
            get
            {
                _defaultOccur = BooleanClause.Occur.MUST_NOT;
                return this;
            }
        }

        /// <summary>
        /// Returns this QueryBuilder with all following queries added to it using 
        /// BooleanClause.Occur.SHOULD. (this is effectively the "OR" operator, but scored)
        /// </summary>
        public QueryBuilder Should
        {
            get
            {
                _defaultOccur = BooleanClause.Occur.SHOULD;
                return this;
            }
        }

        /// <summary>
        /// Returns the underlying built query.
        /// </summary>
        public BooleanQuery Query
        {
            get { return _query; }
        }

        #region Fluent Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="analyzer"></param>
        /// <returns></returns>
        public QueryBuilder WithAnalyzer(Analyzer analyzer)
        {
            _analyzer = analyzer;
            return this;
        }

        /// <summary>
        /// Loop through an enumerable and do a specific action on each one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public QueryBuilder ForEach<T>(IEnumerable<T> enumerable, Action<T, QueryBuilder> action)
        {
            foreach (var item in enumerable)
            {
                action(item, this);
            }
            return this;
        }

        /// <summary>
        /// Provides an interface for you to nest sub-queries with another QueryBuilder.
        /// </summary>
        /// <param name="fn">
        ///     A function which is passed in the sub QueryBuilder, expecting to return a QueryBuilder instance
        ///     whose Query will be added as a subquery to this one's
        /// </param>
        /// <returns></returns>
        public QueryBuilder MatchSubQuery(Func<QueryBuilder, QueryBuilder> fn)
        {
            var subBuilder = fn(new QueryBuilder(new BooleanQuery(), _analyzer, BooleanClause.Occur.MUST));
            return AddSubQuery(subBuilder.Query);
        }

        /// <summary>
        /// Add query to match a term/value for a certain field
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public QueryBuilder MatchTerm<T>(string field, T value)
        {
            if (value == null) return this;

            return AddSubQuery(new TermQuery(new Term(field, value.ToString())));
        }

        /// <summary>
        /// Require document to have a field match all of the passed in terms
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public QueryBuilder MatchAllTerms<T>(string field, IEnumerable<T> values)
        {

            if (values == null || !values.Any())
            {
                return this;
            }

            var query = new BooleanQuery();

            foreach (var keyword in values)
            {
                query.Add(new TermQuery(new Term(field, keyword.ToString())), BooleanClause.Occur.MUST);
            }

            return AddSubQuery(query);
        }

        /// <summary>
        /// Require document to have a field match at least one of the passed in terms
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public QueryBuilder MatchAtLeastOneTerm<T>(string field, IEnumerable<T> values)
        {

            if (values == null || !values.Any())
            {
                return this;
            }

            var query = new BooleanQuery();

            foreach (var keyword in values)
            {
                query.Add(new TermQuery(new Term(field, keyword.ToString())), BooleanClause.Occur.SHOULD);
            }

            return AddSubQuery(query);
        }

        public QueryBuilder MatchAtLeastOneParsedInput<T>(string field, IEnumerable<T> values)
        {

            if (values == null || !values.Any())
            {
                return this;
            }

            var subquery = new QueryBuilder().Should;

            foreach (var keyword in values)
            {
                subquery.MatchParsedInput(field, keyword.ToString(), QueryParser.Operator.AND);
            }

            return AddSubQuery(subquery, BooleanClause.Occur.MUST);
        }



        public QueryBuilder MatchSomeTokens(string field, string stringToTokenize)
        {
            return MatchAtLeastOneTerm(field, _analyzer.TokenListFromString(stringToTokenize));
        }

        /// <summary>
        /// Require document to have a field match none of the passed in terms
        /// </summary>
        /// <param name="field"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public QueryBuilder MatchNoneOfTheTerms<T>(string field, IEnumerable<T> values)
        {

            if (values == null || !values.Any())
            {
                return this;
            }

            var query = new BooleanQuery();

            foreach (var keyword in values)
            {
                query.Add(new TermQuery(new Term(field, keyword.ToString())), BooleanClause.Occur.MUST_NOT);
            }

            return AddSubQuery(query);
        }

        /// <summary>
        /// Require document to match the provided query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryBuilder MatchQuery(Query query)
        {
            return query != null ? AddSubQuery(query) : this;
        }

        /// <summary>
        /// Require document to have a date between the two provided dates. Requires that the date
        /// be saved as an OADate double representation.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="minInclusive"></param>
        /// <param name="maxInclusive"></param>
        /// <returns></returns>
        public QueryBuilder MatchDateRange(string field, DateTime? start, DateTime? end, bool minInclusive = true, bool maxInclusive = true)
        {
            if (!start.HasValue && !end.HasValue) return this;

            return AddSubQuery(NumericRangeQuery.NewIntRange(field,
                                    start != null ? (int?)start.Value.ToOADate() : null,
                                    end != null ? (int?)end.Value.ToOADate() : null,
                                    minInclusive,
                                    maxInclusive));
        }

        public QueryBuilder MatchOADateRange(string field, DateTime start, DateTime end, bool minInclusive = true, bool maxInclusive = true)
        {
            return AddSubQuery(NumericRangeQuery.NewDoubleRange(field,
                                    start.ToOADate(),
                                    end.ToOADate(),
                                    minInclusive,
                                    maxInclusive));
        }

        /// <summary>
        /// Require document to have a number between the two provided ints. 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="minInclusive"></param>
        /// <param name="maxInclusive"></param>
        /// <returns></returns>
        public QueryBuilder MatchRange(string field, int? start, int? end, bool minInclusive = true, bool maxInclusive = true)
        {
            if (!start.HasValue && !end.HasValue) return this;

            return AddSubQuery(NumericRangeQuery.NewIntRange(field,
                                    start,
                                    end,
                                    minInclusive,
                                    maxInclusive));
        }

        public QueryBuilder MatchDoubleRange(string field, double start, double end, bool minInclusive = true, bool maxInclusive = true)
        {
            return AddSubQuery(NumericRangeQuery.NewDoubleRange(field,
                                    start,
                                    end,
                                    minInclusive,
                                    maxInclusive));
        }

        public QueryBuilder MatchRange(string field, string start, string end)
        {
            if (start == null && end == null) return this;

            return AddSubQuery(new TermRangeQuery(field,
                                    start,
                                    end,
                                    true,
                                    true));
        }

        /// <summary>
        /// Require document to match a query from user input passed through a query parser
        /// </summary>
        /// <param name="defaultField"></param>
        /// <param name="stringQuery"></param>
        /// <param name="defaultOp"></param>
        /// <returns></returns>
        public QueryBuilder MatchParsedInput(string defaultField, string stringQuery, QueryParser.Operator defaultOp = null)
        {
            if (string.IsNullOrWhiteSpace(stringQuery))
            {
                return this;
            }

            var parser = new QueryParser(Version, defaultField, _analyzer);

            if (defaultOp != null)
            {
                parser.SetDefaultOperator(defaultOp);
            }

            Query query;
            try
            {
                query = parser.Parse(stringQuery);
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(stringQuery.ToLower()));
            }

            return query != null ? AddSubQuery(query) : this;
        }

        /// <summary>
        /// Use a multifield query parser with field-specific boosts.
        /// </summary>
        /// <param name="boosts"></param>
        /// <param name="stringQuery"></param>
        /// <param name="defaultOp"></param>
        /// <returns></returns>
        public QueryBuilder MatchParsedInput(Dictionary<string, float> boosts, string stringQuery, QueryParser.Operator defaultOp = null)
        {
            if (string.IsNullOrWhiteSpace(stringQuery))
            {
                return this;
            }

            var parser = new MultiFieldQueryParser(Version, boosts.Keys.ToArray(), _analyzer, boosts);

            if (defaultOp != null)
            {
                parser.SetDefaultOperator(defaultOp);
            }

            Query query;
            try
            {
                query = parser.Parse(stringQuery);
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(stringQuery.ToLower()));
            }

            return query != null ? AddSubQuery(query) : this;
        }

        /// <summary>
        /// Use a multifield query parser
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="stringQuery"></param>
        /// <param name="defaultOp"></param>
        /// <returns></returns>
        public QueryBuilder MatchParsedInput(IEnumerable<string> fields, string stringQuery, QueryParser.Operator defaultOp = null)
        {
            if (string.IsNullOrWhiteSpace(stringQuery))
            {
                return this;
            }

            var parser = new MultiFieldQueryParser(Version, fields.ToArray(), _analyzer);

            if (defaultOp != null)
            {
                parser.SetDefaultOperator(defaultOp);
            }

            Query query;
            try
            {
                query = parser.Parse(stringQuery);
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(stringQuery.ToLower()));
            }

            return query != null ? AddSubQuery(query) : this;
        }

        /// <summary>
        /// Match a multi-word phrase exactly. (This is like how QueryParser handles quoted phrases)
        /// </summary>
        /// <param name="field"></param>
        /// <param name="phrase"></param>
        /// <param name="slop"></param>
        /// <returns></returns>
        public QueryBuilder MatchPhrase(string field, string phrase, int slop = 0)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                return this;
            }

            var query = new PhraseQuery();
            foreach (var token in _analyzer.TokenListFromString(phrase))
            {
                query.Add(new Term(field, token));
            }

            query.SetSlop(slop);

            return AddSubQuery(query);
        }

        public QueryBuilder MatchPrefix(string field, string phrase, MultiTermQuery.RewriteMethod rewriteMethod = null)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                return this;
            }
            var query = new PrefixQuery(new Term(field, phrase));
            if (rewriteMethod != null)
            {
                query.SetRewriteMethod(rewriteMethod);
            }
            return AddSubQuery(query);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="ifTrue"></param>
        /// <param name="ifFalse"></param>
        /// <returns></returns>
        public QueryBuilder If(bool criteria, Action<QueryBuilder> ifTrue, Action<QueryBuilder> ifFalse = null)
        {
            if (criteria)
            {
                ifTrue(this);
            }
            else if (ifFalse != null)
            {
                ifFalse(this);
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="ifFalse"></param>
        /// <returns></returns>
        public QueryBuilder IfNot(bool criteria, Action<QueryBuilder> ifFalse)
        {
            return If(!criteria, ifFalse);
        }

        public QueryBuilder IfQueryIsEmpty(Action<QueryBuilder> ifEmpty)
        {
            if (_query.Clauses().Count == 0) ifEmpty(this);
            return this;
        }

        public QueryBuilder IfQueryIsNonEmpty(Action<QueryBuilder> ifNonEmpty)
        {
            if (_query.Clauses().Count > 0) ifNonEmpty(this);
            return this;
        }

        #endregion

    }
}
