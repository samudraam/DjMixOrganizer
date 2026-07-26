using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DjMixOrganizer.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCamelotKeyToMusicalKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve existing letter values (e.g. "C") instead of drop+add.
            migrationBuilder.RenameColumn(
                name: "CamelotKey",
                table: "Tracks",
                newName: "MusicalKey");

            migrationBuilder.AlterColumn<string>(
                name: "MusicalKey",
                table: "Tracks",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Null out Camelot / Open Key / junk so only canonical letter keys remain.
            migrationBuilder.Sql(
                """
                UPDATE Tracks
                SET MusicalKey = NULL
                WHERE MusicalKey IS NOT NULL
                  AND MusicalKey NOT IN (
                    'C','C#','Db','D','Eb','E','F','F#','G','Ab','A','Bb','B',
                    'Cm','C#m','Dbm','Dm','Ebm','Em','Fm','F#m','Gm','Abm','Am','Bbm','Bm'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MusicalKey",
                table: "Tracks",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: "MusicalKey",
                table: "Tracks",
                newName: "CamelotKey");
        }
    }
}
