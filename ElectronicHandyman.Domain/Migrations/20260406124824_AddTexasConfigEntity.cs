using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicHandyman.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddTexasConfigEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardDocumentEntity_Boards_BoardId",
                table: "BoardDocumentEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardDocumentEntity",
                table: "BoardDocumentEntity");

            migrationBuilder.RenameTable(
                name: "BoardDocumentEntity",
                newName: "BoardDocuments");

            migrationBuilder.RenameIndex(
                name: "IX_BoardDocumentEntity_BoardId",
                table: "BoardDocuments",
                newName: "IX_BoardDocuments_BoardId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardDocuments",
                table: "BoardDocuments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TexasApiConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TexasApiConfig", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_BoardDocuments_Boards_BoardId",
                table: "BoardDocuments",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardDocuments_Boards_BoardId",
                table: "BoardDocuments");

            migrationBuilder.DropTable(
                name: "TexasApiConfig");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardDocuments",
                table: "BoardDocuments");

            migrationBuilder.RenameTable(
                name: "BoardDocuments",
                newName: "BoardDocumentEntity");

            migrationBuilder.RenameIndex(
                name: "IX_BoardDocuments_BoardId",
                table: "BoardDocumentEntity",
                newName: "IX_BoardDocumentEntity_BoardId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardDocumentEntity",
                table: "BoardDocumentEntity",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardDocumentEntity_Boards_BoardId",
                table: "BoardDocumentEntity",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
