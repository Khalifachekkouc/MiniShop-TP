using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniShop.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        // Products.Category was an enum (0 iPhone, 1 Mac, 2 Desktop, 3 Accessoires). The column is renamed
        // CategoryId and its values become the ids of the new Categories rows. Desktop is not a category
        // any more: its products (iMac, Mac mini, Mac Studio, Mac Pro) move to Mac.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Produits",
                newName: "CategoryId");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.Sql(
                "INSERT INTO Categories (Name, Slug, SortOrder) VALUES (N'iPhone', N'iphone', 1), (N'Mac', N'mac', 2), (N'Accessoires', N'accessoires', 3);");

            migrationBuilder.Sql("""
                UPDATE p SET p.CategoryId = c.Id
                FROM Produits p
                JOIN Categories c ON c.Slug = CASE p.CategoryId WHEN 0 THEN N'iphone' WHEN 3 THEN N'accessoires' ELSE N'mac' END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Produits_CategoryId",
                table: "Produits",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Produits_Categories_CategoryId",
                table: "Produits",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        // Back to the enum: iPhone and Mac by slug; every other category becomes Accessoires.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Produits_Categories_CategoryId",
                table: "Produits");

            migrationBuilder.DropIndex(
                name: "IX_Produits_CategoryId",
                table: "Produits");

            migrationBuilder.Sql("""
                UPDATE p SET p.CategoryId = CASE c.Slug WHEN N'iphone' THEN 0 WHEN N'mac' THEN 1 ELSE 3 END
                FROM Produits p
                JOIN Categories c ON c.Id = p.CategoryId;
                """);

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Produits",
                newName: "Category");
        }
    }
}
