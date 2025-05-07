using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BRK0Y5_HSZF_2024252.Persistence.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fares_TaxiCars_TaxiCarId",
                table: "Fares");

            migrationBuilder.RenameColumn(
                name: "TaxiCarId",
                table: "Fares",
                newName: "CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_Fares_TaxiCarId",
                table: "Fares",
                newName: "IX_Fares_CustomerId");

            migrationBuilder.AddColumn<double>(
                name: "DistanceSinceLastMaintenance",
                table: "TaxiCars",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "TaxiCars",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "TotalDistance",
                table: "TaxiCars",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "Fares",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "From",
                table: "Fares",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "CarId",
                table: "Fares",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fares_CarId",
                table: "Fares",
                column: "CarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fares_Customers_CustomerId",
                table: "Fares",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Fares_TaxiCars_CarId",
                table: "Fares",
                column: "CarId",
                principalTable: "TaxiCars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fares_Customers_CustomerId",
                table: "Fares");

            migrationBuilder.DropForeignKey(
                name: "FK_Fares_TaxiCars_CarId",
                table: "Fares");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Fares_CarId",
                table: "Fares");

            migrationBuilder.DropColumn(
                name: "DistanceSinceLastMaintenance",
                table: "TaxiCars");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "TaxiCars");

            migrationBuilder.DropColumn(
                name: "TotalDistance",
                table: "TaxiCars");

            migrationBuilder.DropColumn(
                name: "CarId",
                table: "Fares");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Fares",
                newName: "TaxiCarId");

            migrationBuilder.RenameIndex(
                name: "IX_Fares_CustomerId",
                table: "Fares",
                newName: "IX_Fares_TaxiCarId");

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "Fares",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "From",
                table: "Fares",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Fares_TaxiCars_TaxiCarId",
                table: "Fares",
                column: "TaxiCarId",
                principalTable: "TaxiCars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
