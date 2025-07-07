using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_stage_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    stage_number = table.Column<int>(type: "int", nullable: false),
                    best_clear_time = table.Column<float>(type: "real", nullable: false),
                    best_death_count = table.Column<int>(type: "int", nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stage_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    gold = table.Column<int>(type: "int", nullable: false),
                    gems = table.Column<int>(type: "int", nullable: false),
                    selected_character = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tutorial_displayed = table.Column<bool>(type: "bit", nullable: false),
                    steam_display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    steam_avatar_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    stage_clears_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    stage_flag_positions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    best_gem_rewards_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    best_clear_times_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    best_death_counts_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    user_id = table.Column<string>(type: "nvarchar(50)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_UserStageRecords_StageNumber_BestClearTime",
                table: "user_stage_records",
                columns: new[] { "stage_number", "best_clear_time" });

            migrationBuilder.CreateIndex(
                name: "IX_UserStageRecords_StageNumber_BestDeathCount",
                table: "user_stage_records",
                columns: new[] { "stage_number", "best_death_count" });

            migrationBuilder.CreateIndex(
                name: "IX_UserStageRecords_UserId_StageNumber",
                table: "user_stage_records",
                columns: new[] { "user_id", "stage_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_items");

            migrationBuilder.DropTable(
                name: "user_stage_records");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
