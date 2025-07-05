using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRankingSystem : Migration
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
                name: "user_stage_records");
        }
    }
}
