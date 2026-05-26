using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micros.Api.Migrations
{
    /// <inheritdoc />
    public partial class UserTelegramIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "telegram_id",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_telegram_id",
                table: "users",
                column: "telegram_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_telegram_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "telegram_id",
                table: "users");
        }
    }
}
