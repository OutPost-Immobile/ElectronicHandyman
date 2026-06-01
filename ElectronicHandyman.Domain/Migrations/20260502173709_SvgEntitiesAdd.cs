using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ElectronicHandyman.Domain.Migrations
{
    /// <inheritdoc />
    public partial class SvgEntitiesAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    CenterX = table.Column<double>(type: "double precision", nullable: false),
                    CenterY = table.Column<double>(type: "double precision", nullable: false),
                    Radius = table.Column<double>(type: "double precision", nullable: false),
                    StrokeWidth = table.Column<double>(type: "double precision", nullable: false),
                    StrokeType = table.Column<string>(type: "text", nullable: false),
                    FillType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Circles_Symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "Symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    ElectricalPinType = table.Column<string>(type: "text", nullable: false),
                    GraphicPinShape = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AtX = table.Column<double>(type: "double precision", nullable: false),
                    AtY = table.Column<double>(type: "double precision", nullable: false),
                    AtAngle = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pins_Symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "Symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Polylines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    StrokeWidth = table.Column<double>(type: "double precision", nullable: false),
                    StrokeType = table.Column<string>(type: "text", nullable: false),
                    FillType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polylines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Polylines_Symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "Symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rectangles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SymbolId = table.Column<int>(type: "integer", nullable: false),
                    StartX = table.Column<double>(type: "double precision", nullable: false),
                    StartY = table.Column<double>(type: "double precision", nullable: false),
                    EndX = table.Column<double>(type: "double precision", nullable: false),
                    EndY = table.Column<double>(type: "double precision", nullable: false),
                    StrokeWidth = table.Column<double>(type: "double precision", nullable: false),
                    StrokeType = table.Column<string>(type: "text", nullable: false),
                    FillType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rectangles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rectangles_Symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "Symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolylinePoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolylineId = table.Column<int>(type: "integer", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<double>(type: "double precision", nullable: false),
                    Y = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolylinePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolylinePoints_Polylines_PolylineId",
                        column: x => x.PolylineId,
                        principalTable: "Polylines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Circles_SymbolId",
                table: "Circles",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_Pins_SymbolId",
                table: "Pins",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_PolylinePoints_PolylineId",
                table: "PolylinePoints",
                column: "PolylineId");

            migrationBuilder.CreateIndex(
                name: "IX_Polylines_SymbolId",
                table: "Polylines",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_Rectangles_SymbolId",
                table: "Rectangles",
                column: "SymbolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Circles");

            migrationBuilder.DropTable(
                name: "Pins");

            migrationBuilder.DropTable(
                name: "PolylinePoints");

            migrationBuilder.DropTable(
                name: "Rectangles");

            migrationBuilder.DropTable(
                name: "Polylines");

            migrationBuilder.DropTable(
                name: "Symbols");
        }
    }
}
