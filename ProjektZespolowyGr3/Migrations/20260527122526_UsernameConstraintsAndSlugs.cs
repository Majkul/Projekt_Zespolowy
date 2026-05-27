using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjektZespolowyGr3.Migrations
{
    /// <inheritdoc />
    public partial class UsernameConstraintsAndSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    user_record RECORD;
                    base_username TEXT;
                    candidate_username TEXT;
                    suffix_number INTEGER;
                    suffix_text TEXT;
                BEGIN
                    FOR user_record IN SELECT "Id", "Username", "Email" FROM "Users" ORDER BY "Id" LOOP
                        base_username := COALESCE(NULLIF(user_record."Username", ''), split_part(user_record."Email", '@', 1), 'user');
                        base_username := regexp_replace(base_username, '[^A-Za-z0-9_]', '_', 'g');
                        base_username := regexp_replace(base_username, '_+', '_', 'g');
                        base_username := trim(both '_' from base_username);

                        IF base_username = '' THEN
                            base_username := 'user';
                        END IF;

                        candidate_username := left(base_username, 32);
                        suffix_number := 2;

                        WHILE EXISTS (
                            SELECT 1
                            FROM "Users"
                            WHERE "Username" = candidate_username
                              AND "Id" <> user_record."Id"
                        ) LOOP
                            suffix_text := '_' || suffix_number::TEXT;
                            candidate_username := left(base_username, 32 - length(suffix_text)) || suffix_text;
                            suffix_number := suffix_number + 1;
                        END LOOP;

                        UPDATE "Users"
                        SET "Username" = candidate_username
                        WHERE "Id" = user_record."Id";
                    END LOOP;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }
    }
}
