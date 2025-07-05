using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class JustClimb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    gold = table.Column<int>(type: "int", nullable: false),
                    gems = table.Column<int>(type: "int", nullable: false),
                    selected_character = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tutorial_displayed = table.Column<bool>(type: "bit", nullable: false),
                    stage_clears_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    stage_flag_positions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    best_gem_rewards_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    best_clear_times_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    best_death_counts_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_gem_rewards_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_clear_times_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_death_counts_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    current_play_times_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    current_death_counts_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_items",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_items", x => new { x.user_id, x.item_id });
                    table.ForeignKey(
                        name: "FK_user_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_items");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
