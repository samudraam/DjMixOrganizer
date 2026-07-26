using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DjMixOrganizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class CanonicalizeDbMusicalKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Tracks
                SET MusicalKey = CASE MusicalKey
                    WHEN 'C#' THEN 'Db'
                    WHEN 'C#m' THEN 'Dbm'
                    ELSE MusicalKey
                END
                WHERE MusicalKey IN ('C#', 'C#m');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Tracks
                SET MusicalKey = CASE MusicalKey
                    WHEN 'Db' THEN 'C#'
                    WHEN 'Dbm' THEN 'C#m'
                    ELSE MusicalKey
                END
                WHERE MusicalKey IN ('Db', 'Dbm');
                """);
        }
    }
}
