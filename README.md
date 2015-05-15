# lucene-fluent-query-builder

Building queries with Lucene's API can be a bit of a pain, if not at least verbose. This class tries to make writing queries as simple as possible using the Fluent interface pattern.


## Installation

```bash
PM> Install-Package LrNet.Lucene.Fluent
```


To get started, before using the query builder you will want to specify somewhere in your project:

```csharp
QueryBuilder.Version = /* Lucene Version you want to use */
QueryBuilder.DefaultAnalyzer = /* Default analyzer you want to use */
```



## Building Queries

The `QueryBuilder` class follows a fluent interface pattern. there are many methods which allow you to add predicates to a boolean query.

When calling `.Must`, `.MustNot`, and `.Should`, you end up changing what operator future conditions are added to the query with.

For example: 

```csharp
var query = new QueryBuilder()
	.WithAnalyzer(...) // optional. defaults to QueryBuilder.DefaultAnalyzer
	.Must
	.MatchParsedInput(q, "searchField")
	.MatchQuery(...)
	.MatchTerm(field, value)
	.MatchAllTerms(field, values)
	.MatchDateRange(startDate, endDate)
	.MatchDateRange()

	.MustNot
	.MatchDateRange(...)
	
	.Should
	.MatchDateRange(...)
	.MatchSubQuery(b => b
		.Must
		.MatchTerm(...)
		.MustNot
		.MatchTerm(...))
	.Query;
```

