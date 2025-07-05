using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_clear_times_json",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_death_counts_json",
                table: "users");

            migrationBuilder.DropColumn(
                name: "last_gem_rewards_json",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_clear_times_json",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_death_counts_json",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_gem_rewards_json",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
