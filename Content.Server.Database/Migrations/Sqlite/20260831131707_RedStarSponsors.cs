using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RedStarSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "redstar_sponsors",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    tier = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ooc_color = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    ghost_color = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redstar_sponsors", x => x.player_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redstar_sponsors");
        }
    }
}
