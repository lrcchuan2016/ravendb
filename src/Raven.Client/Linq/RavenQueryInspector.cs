//-----------------------------------------------------------------------
// <copyright file="RavenQueryInspector.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Commands;
using Raven.Client.Connection;
using Raven.Client.Data;
using Raven.Client.Data.Queries;
using Raven.Client.Document;
using Raven.Client.Document.Async;
using Raven.Client.Extensions;
using Raven.Client.Indexes;
using Raven.Client.Indexing;
using Raven.Client.PublicExtensions;
using Raven.Client.Spatial;

namespace Raven.Client.Linq
{
    /// <summary>
    /// Implements <see cref="IRavenQueryable{T}"/>
    /// </summary>
    public class RavenQueryInspector<T> : IRavenQueryable<T>, IRavenQueryInspector
    {
        private Expression _expression;
        private IRavenQueryProvider _provider;
        private RavenQueryStatistics _queryStats;
        private RavenQueryHighlightings _highlightings;
        private string _indexName;
        private InMemoryDocumentSessionOperations _session;
        private bool _isMapReduce;


        /// <summary>
        /// Initializes a new instance of the <see cref="RavenQueryInspector{T}"/> class.
        /// </summary>
        public void Init(
            IRavenQueryProvider provider,
            RavenQueryStatistics queryStats,
            RavenQueryHighlightings highlightings,
            string indexName,
            Expression expression,
            InMemoryDocumentSessionOperations session,
            bool isMapReduce)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            _provider = provider.For<T>();
            _queryStats = queryStats;
            _highlightings = highlightings;
            _indexName = indexName;
            _session = session;
            _isMapReduce = isMapReduce;
            _provider.AfterQueryExecuted(AfterQueryExecuted);
            _expression = expression ?? Expression.Constant(this);
        }

        private void AfterQueryExecuted(QueryResult queryResult)
        {
            _queryStats.UpdateQueryStats(queryResult);
            _highlightings.Update(queryResult);
        }

        #region IOrderedQueryable<T> Members

        Expression IQueryable.Expression => _expression;

        Type IQueryable.ElementType => typeof(T);

        IQueryProvider IQueryable.Provider => _provider;

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            var execute = _provider.Execute(_expression);
            return ((IEnumerable<T>)execute).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Provide statistics about the query, such as total count of matching records
        /// </summary>
        public IRavenQueryable<T> Statistics(out RavenQueryStatistics stats)
        {
            stats = _queryStats;
            return this;
        }

        /// <summary>
        /// Customizes the query using the specified action
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public IRavenQueryable<T> Customize(Action<IDocumentQueryCustomization> action)
        {
            _provider.Customize(action);
            return this;
        }

        public IRavenQueryable<TResult> TransformWith<TTransformer, TResult>() where TTransformer : AbstractTransformerCreationTask, new()
        {
            var transformer = new TTransformer();
            _provider.TransformWith(transformer.TransformerName);
            var res = (IRavenQueryable<TResult>)this.As<TResult>();
            res.OriginalQueryType = res.OriginalQueryType ?? typeof(T);
            var p = res.Provider as IRavenQueryProvider;
            if (null != p)
                p.OriginalQueryType = res.OriginalQueryType;
            return res;
        }

        public IRavenQueryable<TResult> TransformWith<TResult>(string transformerName)
        {
            _provider.TransformWith(transformerName);
            var res = (IRavenQueryable<TResult>)this.As<TResult>();
            res.OriginalQueryType = res.OriginalQueryType ?? typeof(T);
            _provider.OriginalQueryType = res.OriginalQueryType;
            var p = res.Provider as IRavenQueryProvider;
            if (null != p)
                p.OriginalQueryType = res.OriginalQueryType;
            return res;
        }

        public IRavenQueryable<T> AddQueryInput(string input, object value)
        {
            return AddTransformerParameter(input, value);
        }

        public IRavenQueryable<T> AddTransformerParameter(string input, object value)
        {
            _provider.AddTransformerParameter(input, value);
            return this;
        }

        public IRavenQueryable<T> Spatial(Expression<Func<T, object>> path, Func<SpatialCriteriaFactory, SpatialCriteria> clause)
        {
            return Customize(x => x.Spatial(path.ToPropertyPath(), clause));
        }

        public IRavenQueryable<T> OrderByDistance(SpatialSort sortParamsClause)
        {
            if (string.IsNullOrEmpty(sortParamsClause.FieldName))
                return Customize(x => x.SortByDistance(sortParamsClause.Latitude, sortParamsClause.Longitude));

            return Customize(x => x.SortByDistance(sortParamsClause.Latitude, sortParamsClause.Longitude, sortParamsClause.FieldName));
        }

        public Type OriginalQueryType { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            RavenQueryProviderProcessor<T> ravenQueryProvider = GetRavenQueryProvider();
            string query;
            if ((_session as AsyncDocumentSession) != null)
            {
                var asyncDocumentQuery = ravenQueryProvider.GetAsyncDocumentQueryFor(_expression);
                query = asyncDocumentQuery.GetIndexQuery(true).ToString();
            }
            else
            {
                var documentQuery = ravenQueryProvider.GetDocumentQueryFor(_expression);
                query = documentQuery.ToString();
            }

            string fields = "";
            if (ravenQueryProvider.FieldsToFetch.Count > 0)
                fields = "<" + string.Join(", ", ravenQueryProvider.FieldsToFetch.ToArray()) + ">: ";
            return fields + query;
        }

        public IndexQuery GetIndexQuery(bool isAsync = true)
        {
            RavenQueryProviderProcessor<T> ravenQueryProvider = GetRavenQueryProvider();
            if (isAsync == false)
            {
                var documentQuery = ravenQueryProvider.GetDocumentQueryFor(_expression);
                return documentQuery.GetIndexQuery(false);
            }
            var asyncDocumentQuery = ravenQueryProvider.GetAsyncDocumentQueryFor(_expression);
            return asyncDocumentQuery.GetIndexQuery(true);
        }

        public virtual FacetedQueryResult GetFacets(string facetSetupDoc, int start, int? pageSize)
        {
            var q = GetIndexQuery(false);
            var query = FacetQuery.Create(_indexName, q, facetSetupDoc, null, start, pageSize, q.Conventions);

            var command = new GetFacetsCommand(_session.Context, query);
            _session.RequestExecuter.Execute(command, _session.Context);
            return command.Result;
        }

        public virtual FacetedQueryResult GetFacets(List<Facet> facets, int start, int? pageSize)
        {
            var q = GetIndexQuery(false);
            var query = FacetQuery.Create(_indexName, q, null, facets, start, pageSize, q.Conventions);
            var command = new GetFacetsCommand(_session.Context, query);
            _session.RequestExecuter.Execute(command, _session.Context);
            return command.Result;
        }

        public virtual async Task<FacetedQueryResult> GetFacetsAsync(string facetSetupDoc, int start, int? pageSize, CancellationToken token = default(CancellationToken))
        {
            var q = GetIndexQuery();
            var query = FacetQuery.Create(_indexName, q, facetSetupDoc, null, start, pageSize, q.Conventions);

            var command = new GetFacetsCommand(_session.Context, query);
            await _session.RequestExecuter.ExecuteAsync(command, _session.Context, token).ConfigureAwait(false);

            return command.Result;
        }

        public virtual async Task<FacetedQueryResult> GetFacetsAsync(List<Facet> facets, int start, int? pageSize, CancellationToken token = default(CancellationToken))
        {
            var q = GetIndexQuery();
            var query = FacetQuery.Create(_indexName, q, null, facets, start, pageSize, q.Conventions);

            var command = new GetFacetsCommand(_session.Context, query);
            await _session.RequestExecuter.ExecuteAsync(command, _session.Context, token).ConfigureAwait(false);

            return command.Result;
        }

        private RavenQueryProviderProcessor<T> GetRavenQueryProvider()
        {
            return new RavenQueryProviderProcessor<T>(_provider.QueryGenerator, _provider.CustomizeQuery, null, null, _indexName,
                                                      new HashSet<string>(), new List<RenamedField>(), _isMapReduce,
                                                      _provider.ResultTransformer, _provider.TransformerParameters, OriginalQueryType);
        }

        /// <summary>
        /// Get the name of the index being queried
        /// </summary>
        public string IndexQueried
        {
            get
            {
                var ravenQueryProvider = new RavenQueryProviderProcessor<T>(_provider.QueryGenerator, null, null, null, _indexName, new HashSet<string>(), new List<RenamedField>(), _isMapReduce,
                    _provider.ResultTransformer, _provider.TransformerParameters, OriginalQueryType);
                var documentQuery = ravenQueryProvider.GetDocumentQueryFor(_expression);
                return ((IRavenQueryInspector)documentQuery).IndexQueried;
            }
        }

        /// <summary>
        /// Get the name of the index being queried asynchronously
        /// </summary>
        public string AsyncIndexQueried
        {
            get
            {
                var ravenQueryProvider = new RavenQueryProviderProcessor<T>(_provider.QueryGenerator, null, null, null, _indexName, new HashSet<string>(), new List<RenamedField>(), _isMapReduce,
                    _provider.ResultTransformer, _provider.TransformerParameters, OriginalQueryType);
                var documentQuery = ravenQueryProvider.GetAsyncDocumentQueryFor(_expression);
                return ((IRavenQueryInspector)documentQuery).IndexQueried;
            }
        }

        public InMemoryDocumentSessionOperations Session => _session;

        ///<summary>
        /// Get the last equality term for the query
        ///</summary>
        public KeyValuePair<string, string> GetLastEqualityTerm(bool isAsync = false)
        {
            var ravenQueryProvider = new RavenQueryProviderProcessor<T>(_provider.QueryGenerator, null, null, null, _indexName, new HashSet<string>(),
                new List<RenamedField>(), _isMapReduce, _provider.ResultTransformer, _provider.TransformerParameters, OriginalQueryType);

            if (isAsync)
            {
                var asyncDocumentQuery = ravenQueryProvider.GetAsyncDocumentQueryFor(_expression);
                return ((IRavenQueryInspector)asyncDocumentQuery).GetLastEqualityTerm(true);
            }

            var documentQuery = ravenQueryProvider.GetDocumentQueryFor(_expression);
            return ((IRavenQueryInspector)documentQuery).GetLastEqualityTerm();
        }

        /// <summary>
        /// Set the fields to fetch
        /// </summary>
        public void FieldsToFetch(IEnumerable<string> fields)
        {
            foreach (var field in fields)
            {
                _provider.FieldsToFetch.Add(field);
            }
        }
    }
}
