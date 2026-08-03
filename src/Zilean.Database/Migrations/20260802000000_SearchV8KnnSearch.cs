using Microsoft.EntityFrameworkCore.Migrations;
using Zilean.Database.Functions;

#nullable disable

namespace Zilean.Database.Migrations;

/// <inheritdoc />
public partial class SearchV8KnnSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop GIN trigram index, create GiST trigram index for KNN distance support
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "idx_cleaned_parsed_title_trgm";
            """);
        migrationBuilder.Sql(
            """
            CREATE INDEX "idx_cleaned_parsed_title_trgm"
            ON "Torrents" USING GIST ("CleanedParsedTitle" gist_trgm_ops);
            """);

        // Deploy V7 function (ORDER BY <-> KNN instead of ORDER BY similarity)
        migrationBuilder.Sql(SearchTorrentsMetaV6.Remove);
        migrationBuilder.Sql(SearchTorrentsMetaV7.Create);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert function to V6
        migrationBuilder.Sql(SearchTorrentsMetaV7.Remove);
        migrationBuilder.Sql(SearchTorrentsMetaV6.Create);

        // Revert index to GIN
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "idx_cleaned_parsed_title_trgm";
            """);
        migrationBuilder.Sql(
            """
            CREATE INDEX "idx_cleaned_parsed_title_trgm"
            ON "Torrents" USING GIN ("CleanedParsedTitle" gin_trgm_ops);
            """);
    }
}