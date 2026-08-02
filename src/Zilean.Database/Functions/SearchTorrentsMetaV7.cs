namespace Zilean.Database.Functions;

public class SearchTorrentsMetaV7
{
    internal const string Create =
        """
        CREATE OR REPLACE FUNCTION search_torrents_meta(
            query TEXT DEFAULT NULL,
            season INT DEFAULT NULL,
            episode INT DEFAULT NULL,
            year INT DEFAULT NULL,
            language TEXT DEFAULT NULL,
            resolution TEXT DEFAULT NULL,
            imdbId TEXT DEFAULT NULL,
            limit_param INT DEFAULT 20,
            category TEXT DEFAULT NULL,
            similarity_threshold REAL DEFAULT 0.85
        )
        RETURNS TABLE(
            "InfoHash" TEXT,
            "Resolution" TEXT,
            "Year" INT,
            "Remastered" BOOLEAN,
            "Codec" TEXT,
            "Audio" TEXT[],
            "Quality" TEXT,
            "Episodes" INT[],
            "Seasons" INT[],
            "Languages" TEXT[],
            "ParsedTitle" TEXT,
            "NormalizedTitle" TEXT,
            "RawTitle" TEXT,
            "Size" TEXT,
            "Category" TEXT,
            "Complete" BOOLEAN,
            "Volumes" INT[],
            "Hdr" TEXT[],
            "Channels" TEXT[],
            "Dubbed" BOOLEAN,
            "Subbed" BOOLEAN,
            "Edition" TEXT,
            "BitDepth" TEXT,
            "Bitrate" TEXT,
            "Network" TEXT,
            "Extended" BOOLEAN,
            "Converted" BOOLEAN,
            "Hardcoded" BOOLEAN,
            "Region" TEXT,
            "Ppv" BOOLEAN,
            "Is3d" BOOLEAN,
            "Site" TEXT,
            "Proper" BOOLEAN,
            "Repack" BOOLEAN,
            "Retail" BOOLEAN,
            "Upscaled" BOOLEAN,
            "Unrated" BOOLEAN,
            "Documentary" BOOLEAN,
            "EpisodeCode" TEXT,
            "Country" TEXT,
            "Container" TEXT,
            "Extension" TEXT,
            "Torrent" BOOLEAN,
            "Score" REAL,
            "ImdbId" TEXT,
            "ImdbCategory" TEXT,
            "ImdbTitle" TEXT,
            "ImdbYear" INT,
            "ImdbAdult" BOOLEAN,
            "IngestedAt" TIMESTAMPTZ
        ) AS $$
        DECLARE
            effective_threshold REAL;
            has_filters BOOLEAN;
        BEGIN
            -- When structured filters are provided (season, episode, year, imdbId),
            -- lower the similarity threshold since the filters themselves provide precision.
            -- This fixes short query strings (e.g. "1923") returning 0 results when combined
            -- with season/episode filters, because trigram similarity is unreliable for short strings.
            --
            -- Book/audiobook searches also get a lower threshold because their titles include
            -- author names and format tags, reducing trigram similarity for keyword searches.
            has_filters := (season IS NOT NULL OR episode IS NOT NULL OR year IS NOT NULL OR imdbId IS NOT NULL);

            IF category IN ('book', 'audiobook') THEN
                effective_threshold := similarity_threshold * 0.35;
            ELSIF has_filters AND query IS NOT NULL AND length(query) <= 6 THEN
                effective_threshold := similarity_threshold * 0.3;
            ELSIF has_filters THEN
                effective_threshold := similarity_threshold * 0.5;
            ELSE
                effective_threshold := similarity_threshold;
            END IF;

            EXECUTE format('SET pg_trgm.similarity_threshold = %L', effective_threshold);

            IF query IS NULL THEN
                -- IMDb-ID-only / filtered-only search: no trigram distance, sort by recency
                RETURN QUERY
                SELECT
                    t."InfoHash",
                    t."Resolution",
                    t."Year",
                    t."Remastered",
                    t."Codec",
                    t."Audio",
                    t."Quality",
                    t."Episodes",
                    t."Seasons",
                    t."Languages",
                    t."ParsedTitle",
                    t."NormalizedTitle",
                    t."RawTitle",
                    t."Size",
                    t."Category",
                    t."Complete",
                    t."Volumes",
                    t."Hdr",
                    t."Channels",
                    t."Dubbed",
                    t."Subbed",
                    t."Edition",
                    t."BitDepth",
                    t."Bitrate",
                    t."Network",
                    t."Extended",
                    t."Converted",
                    t."Hardcoded",
                    t."Region",
                    t."Ppv",
                    t."Is3d",
                    t."Site",
                    t."Proper",
                    t."Repack",
                    t."Retail",
                    t."Upscaled",
                    t."Unrated",
                    t."Documentary",
                    t."EpisodeCode",
                    t."Country",
                    t."Container",
                    t."Extension",
                    t."Torrent",
                    similarity(t."CleanedParsedTitle", query) AS "Score",
                    t."ImdbId",
                    i."Category" AS "ImdbCategory",
                    i."Title" AS "ImdbTitle",
                    i."Year" AS "ImdbYear",
                    i."Adult" AS "ImdbAdult",
                    t."IngestedAt"
                FROM
                    public."Torrents" t
                LEFT JOIN
                    public."ImdbFiles" i ON t."ImdbId" = i."ImdbId"
                WHERE
                    Length(t."InfoHash") = 40
                AND
                    (category IS NULL OR t."Category" = category)
                AND (imdbId IS NULL OR t."ImdbId" = imdbId)
                AND (season IS NULL OR season = ANY(t."Seasons"))
                AND (
                    (episode IS NULL AND season IS NOT NULL)
                    OR
                    (
                        episode IS NOT NULL AND
                        season IS NOT NULL AND
                        (episode = ANY(t."Episodes") OR t."Episodes" IS NULL OR t."Episodes" = '{}')
                    )
                    OR (season IS NULL AND episode IS NULL)
                )
                AND (year IS NULL OR t."Year" BETWEEN year - 1 AND year + 1)
                AND (language IS NULL OR language = ANY(t."Languages"))
                AND (resolution IS NULL OR resolution = t."Resolution")
                ORDER BY
                    "IngestedAt" DESC
                LIMIT
                    limit_param;
            ELSE
                -- Trigram search: two-stage query to get GiST KNN index scan.
                -- Inner: ORDER BY <-> LIMIT (index-accelerated KNN, no full-table sort).
                -- Outer: re-sort the limited rows by distance then recency for stable ordering.
                RETURN QUERY
                SELECT
                    k."InfoHash",
                    k."Resolution",
                    k."Year",
                    k."Remastered",
                    k."Codec",
                    k."Audio",
                    k."Quality",
                    k."Episodes",
                    k."Seasons",
                    k."Languages",
                    k."ParsedTitle",
                    k."NormalizedTitle",
                    k."RawTitle",
                    k."Size",
                    k."Category",
                    k."Complete",
                    k."Volumes",
                    k."Hdr",
                    k."Channels",
                    k."Dubbed",
                    k."Subbed",
                    k."Edition",
                    k."BitDepth",
                    k."Bitrate",
                    k."Network",
                    k."Extended",
                    k."Converted",
                    k."Hardcoded",
                    k."Region",
                    k."Ppv",
                    k."Is3d",
                    k."Site",
                    k."Proper",
                    k."Repack",
                    k."Retail",
                    k."Upscaled",
                    k."Unrated",
                    k."Documentary",
                    k."EpisodeCode",
                    k."Country",
                    k."Container",
                    k."Extension",
                    k."Torrent",
                    k."Score",
                    k."ImdbId",
                    k."ImdbCategory",
                    k."ImdbTitle",
                    k."ImdbYear",
                    k."ImdbAdult",
                    k."IngestedAt"
                FROM (
                    SELECT
                        t."InfoHash",
                        t."Resolution",
                        t."Year",
                        t."Remastered",
                        t."Codec",
                        t."Audio",
                        t."Quality",
                        t."Episodes",
                        t."Seasons",
                        t."Languages",
                        t."ParsedTitle",
                        t."NormalizedTitle",
                        t."RawTitle",
                        t."Size",
                        t."Category",
                        t."Complete",
                        t."Volumes",
                        t."Hdr",
                        t."Channels",
                        t."Dubbed",
                        t."Subbed",
                        t."Edition",
                        t."BitDepth",
                        t."Bitrate",
                        t."Network",
                        t."Extended",
                        t."Converted",
                        t."Hardcoded",
                        t."Region",
                        t."Ppv",
                        t."Is3d",
                        t."Site",
                        t."Proper",
                        t."Repack",
                        t."Retail",
                        t."Upscaled",
                        t."Unrated",
                        t."Documentary",
                        t."EpisodeCode",
                        t."Country",
                        t."Container",
                        t."Extension",
                        t."Torrent",
                        similarity(t."CleanedParsedTitle", query) AS "Score",
                        t."CleanedParsedTitle" <-> query AS "Dist",
                        t."ImdbId",
                        i."Category" AS "ImdbCategory",
                        i."Title" AS "ImdbTitle",
                        i."Year" AS "ImdbYear",
                        i."Adult" AS "ImdbAdult",
                        t."IngestedAt"
                    FROM
                        public."Torrents" t
                    LEFT JOIN
                        public."ImdbFiles" i ON t."ImdbId" = i."ImdbId"
                    WHERE
                        Length(t."InfoHash") = 40
                    AND
                        (category IS NULL OR t."Category" = category)
                    AND
                        t."CleanedParsedTitle" % query
                    AND (imdbId IS NULL OR t."ImdbId" = imdbId)
                    AND (season IS NULL OR season = ANY(t."Seasons"))
                    AND (
                        (episode IS NULL AND season IS NOT NULL)
                        OR
                        (
                            episode IS NOT NULL AND
                            season IS NOT NULL AND
                            (episode = ANY(t."Episodes") OR t."Episodes" IS NULL OR t."Episodes" = '{}')
                        )
                        OR (season IS NULL AND episode IS NULL)
                    )
                    AND (year IS NULL OR t."Year" BETWEEN year - 1 AND year + 1)
                    AND (language IS NULL OR language = ANY(t."Languages"))
                    AND (resolution IS NULL OR resolution = t."Resolution")
                    ORDER BY
                        t."CleanedParsedTitle" <-> query
                    FETCH FIRST limit_param ROWS WITH TIES
                ) k
                ORDER BY
                    k."Dist",
                    k."IngestedAt" DESC
                LIMIT
                    limit_param;
            END IF;
        END;
        $$ LANGUAGE plpgsql;
        """;

    internal const string Remove = "DROP FUNCTION IF EXISTS search_torrents_meta(TEXT, INT, INT, INT, TEXT, TEXT, TEXT, INT, TEXT, REAL);";
}