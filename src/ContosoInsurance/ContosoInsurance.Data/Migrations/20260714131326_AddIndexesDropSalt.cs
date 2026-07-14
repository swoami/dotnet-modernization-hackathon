using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoInsurance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesDropSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Guarded SQL: databases baselined from the legacy db/*.sql scripts have no Salt
            // column and already contain IX_Claims_FiledOn / unique constraints.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Users', N'Salt') IS NOT NULL
                    ALTER TABLE [dbo].[Users] DROP COLUMN [Salt];
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Users_Username' AND [object_id] = OBJECT_ID(N'dbo.Users'))
                    CREATE UNIQUE INDEX [IX_Users_Username] ON [dbo].[Users] ([Username]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Policies_PolicyNumber' AND [object_id] = OBJECT_ID(N'dbo.Policies'))
                    CREATE UNIQUE INDEX [IX_Policies_PolicyNumber] ON [dbo].[Policies] ([PolicyNumber]);
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Claims_FiledOn' AND [object_id] = OBJECT_ID(N'dbo.Claims'))
                    CREATE INDEX [IX_Claims_FiledOn] ON [dbo].[Claims] ([FiledOn] DESC);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                schema: "dbo",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Policies_PolicyNumber",
                schema: "dbo",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Claims_FiledOn",
                schema: "dbo",
                table: "Claims");

            migrationBuilder.AddColumn<string>(
                name: "Salt",
                schema: "dbo",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
