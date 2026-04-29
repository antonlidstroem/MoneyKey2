using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyKey.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "UserLists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListConfig",
                table: "UserLists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemData",
                table: "ListItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ListItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CsnEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BudgetId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalOriginalDebt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualRepayment = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualIncomeLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedAnnualIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCurrentlyStudying = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyStudyGrant = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MonthlyStudyLoan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CsnEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CsnEntries_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CsnEntries_BudgetId_UserId_Year",
                table: "CsnEntries",
                columns: new[] { "BudgetId", "UserId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CsnEntries");

            migrationBuilder.DropColumn(
                name: "ListConfig",
                table: "UserLists");

            migrationBuilder.DropColumn(
                name: "ItemData",
                table: "ListItems");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ListItems");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "UserLists",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
