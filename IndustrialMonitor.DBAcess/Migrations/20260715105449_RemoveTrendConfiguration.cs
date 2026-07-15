using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrialMonitor.DBAcess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTrendConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monitor_TrendAxises");

            migrationBuilder.DropTable(
                name: "monitor_Trends");

            migrationBuilder.DropTable(
                name: "monitor_TrendSections");

            migrationBuilder.DropTable(
                name: "monitor_TrendSerieses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitor_TrendAxises",
                columns: table => new
                {
                    AxisNum = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsShowSeperator = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsShowTitle = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LabelFormater = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Maximum = table.Column<double>(type: "double", nullable: false),
                    Minimum = table.Column<double>(type: "double", nullable: false),
                    Position = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrendNum = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitor_TrendAxises", x => x.AxisNum);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "monitor_Trends",
                columns: table => new
                {
                    TrendNum = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsShowLegend = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TrendName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitor_Trends", x => x.TrendNum);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "monitor_TrendSections",
                columns: table => new
                {
                    SectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AxisNum = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitor_TrendSections", x => x.SectionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "monitor_TrendSerieses",
                columns: table => new
                {
                    SeriesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ANum = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeviceNum = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrendNum = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VarNum = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monitor_TrendSerieses", x => x.SeriesId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
