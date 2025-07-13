using System;
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
            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    achievement_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    reward_amount = table.Column<int>(type: "int", nullable: false),
                    reward_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.achievement_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    gold = table.Column<int>(type: "int", nullable: false),
                    gems = table.Column<int>(type: "int", nullable: false),
                    selected_character = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    tutorial_displayed = table.Column<bool>(type: "bit", nullable: false),
                    steam_display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    steam_avatar_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_achievement_progress",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    stages_completed = table.Column<int>(type: "int", nullable: false),
                    perfect_clears = table.Column<int>(type: "int", nullable: false),
                    speed_clears = table.Column<int>(type: "int", nullable: false),
                    chapter1_perfect_stages = table.Column<int>(type: "int", nullable: false),
                    items_purchased = table.Column<int>(type: "int", nullable: false),
                    unlocked_characters_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    item_types_used_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    deaths_in_current_stage = table.Column<int>(type: "int", nullable: false),
                    used_item_in_current_stage = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievement_progress", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_achievement_progress_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    achievement_id = table.Column<int>(type: "int", nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    claimed_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => new { x.user_id, x.achievement_id });
                    table.CheckConstraint("CK_UserAchievements_ClaimedAfterUnlock", "claimed_at IS NULL OR claimed_at >= unlocked_at");
                    table.ForeignKey(
                        name: "FK_user_achievements_achievements_achievement_id",
                        column: x => x.achievement_id,
                        principalTable: "achievements",
                        principalColumn: "achievement_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_achievements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_items",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "user_stage_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    stage_number = table.Column<int>(type: "int", nullable: false),
                    is_cleared = table.Column<bool>(type: "bit", nullable: false),
                    best_gem_count = table.Column<int>(type: "int", nullable: false),
                    best_clear_time = table.Column<float>(type: "real", nullable: false),
                    best_death_count = table.Column<int>(type: "int", nullable: false),
                    current_play_time = table.Column<float>(type: "real", nullable: false),
                    current_death_count = table.Column<int>(type: "int", nullable: false),
                    flag_x = table.Column<float>(type: "real", nullable: true),
                    flag_y = table.Column<float>(type: "real", nullable: true),
                    flag_z = table.Column<float>(type: "real", nullable: true),
                    display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_stage_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_stage_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievements_category",
                table: "achievements",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "IX_achievements_code",
                table: "achievements",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievements_sort_order",
                table: "achievements",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievement_progress_user_id",
                table: "user_achievement_progress",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_achievement_id",
                table: "user_achievements",
                column: "achievement_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_unlocked_at",
                table: "user_achievements",
                column: "unlocked_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_achievements_user_id",
                table: "user_achievements",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserAchievements_Unclaimed",
                table: "user_achievements",
                columns: new[] { "user_id", "claimed_at" },
                filter: "claimed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_stage_records_stage_number_best_clear_time",
                table: "user_stage_records",
                columns: new[] { "stage_number", "best_clear_time" });

            migrationBuilder.CreateIndex(
                name: "IX_user_stage_records_stage_number_best_death_count",
                table: "user_stage_records",
                columns: new[] { "stage_number", "best_death_count" });

            migrationBuilder.CreateIndex(
                name: "IX_user_stage_records_user_id_stage_number",
                table: "user_stage_records",
                columns: new[] { "user_id", "stage_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_achievement_progress");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "user_items");

            migrationBuilder.DropTable(
                name: "user_stage_records");

            migrationBuilder.DropTable(
                name: "achievements");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
