using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ooc_color = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    ghost_color = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true)
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
