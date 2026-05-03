using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicHandyman.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddNameIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Symbols_Name",
                table: "Symbols",
                column: "Name",
                unique: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Symbols_Name",
                table: "Symbols");
        }
    }
}
