using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micros.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAtColumnTriggerFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 CREATE OR REPLACE FUNCTION set_updated_at()
                                 RETURNS TRIGGER AS $$
                                 BEGIN
                                     NEW.updated_at = NOW();
                                     RETURN NEW;
                                 END;
                                 $$ LANGUAGE plpgsql;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_updated_at();");
        }
    }
}
