namespace Raven.Client.PublicExtensions
{
    public enum EscapeQueryOptions
    {
        EscapeAll,
        AllowPostfixWildcard,
        /// <summary>
        /// This allows queries such as Name:*term*, which tend to be much
        /// more expensive and less performant than any other queries. 
        /// Consider carefully whatever you really need this, as there are other
        /// alternative for searching without doing extremely expensive leading 
        /// wildcard matches.
        /// </summary>
        AllowAllWildcards,
        RawQuery
    }
}