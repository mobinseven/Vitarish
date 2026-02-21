namespace Vitarish.Modules.Elasticsearch.Application.Abstractions;

public interface ISearchDocumentMapper<in TSource, out TDocument>
{
    TDocument Map(TSource source);
}
