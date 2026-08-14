namespace Zilean.Shared.Features.Python;

/// <summary>
/// Represents the result of an RTN <c>parse()</c> invocation for a single torrent title.
/// </summary>
/// <param name="Success">Whether the RTN parse completed without error.</param>
/// <param name="Response">The populated <see cref="TorrentInfo"/> on success, or <see langword="null"/> on failure.</param>
public record ParseTorrentTitleResponse(bool Success, TorrentInfo? Response);