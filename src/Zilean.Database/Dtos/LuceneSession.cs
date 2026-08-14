using J2N.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Zilean.Database.Dtos;

/// <summary>
/// Encapsulates an in-memory Lucene index session with a <see cref="RAMDirectory"/>, <see cref="StandardAnalyzer"/>,
/// and <see cref="IndexWriter"/>. Implements <see cref="IDisposable"/> for resource cleanup.
/// </summary>
public sealed class LuceneSession : IDisposable
{
    /// <summary>
    /// Gets the in-memory RAM directory backing the Lucene index.
    /// </summary>
    public RAMDirectory? Directory { get; } = new();
    /// <summary>
    /// Gets the standard Lucene analyzer used for tokenizing text fields.
    /// </summary>
    public StandardAnalyzer? Analyzer { get; } = new(LuceneVersion.LUCENE_48);
    /// <summary>
    /// Gets or sets the <see cref="IndexWriterConfig"/> controlling index write behavior.
    /// </summary>
    public IndexWriterConfig? Config { get; private set; }
    /// <summary>
    /// Gets or sets the <see cref="IndexWriter"/> used to add documents to the index.
    /// </summary>
    public IndexWriter? Writer { get; private set; }

    /// <summary>
    /// Creates a new <see cref="LuceneSession"/> with a configured <see cref="IndexWriter"/>.
    /// </summary>
    /// <returns>A fully initialized <see cref="LuceneSession"/> instance.</returns>
    public static LuceneSession NewInstance()
    {
        var instance = new LuceneSession();

        instance.Config = new(LuceneVersion.LUCENE_48, instance.Analyzer);
        instance.Writer = new(instance.Directory, instance.Config);

        return instance;
    }

    /// <summary>
    /// Disposes the directory, analyzer, and writer resources held by this session.
    /// </summary>
    public void Dispose()
    {
        Directory?.Dispose();
        Analyzer?.Dispose();
        Writer?.Dispose();
    }
}
