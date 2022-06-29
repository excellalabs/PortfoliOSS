using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfoliOSS.ModernData.Migrations
{
    public partial class InitialCreation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.OrganizationId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUser",
                columns: table => new
                {
                    OrganizationsOrganizationId = table.Column<int>(type: "int", nullable: false),
                    UsersUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUser", x => new { x.OrganizationsOrganizationId, x.UsersUserId });
                    table.ForeignKey(
                        name: "FK_OrganizationUser_Organizations_OrganizationsOrganizationId",
                        column: x => x.OrganizationsOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationUser_Users_UsersUserId",
                        column: x => x.UsersUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    RepoId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.RepoId);
                    table.ForeignKey(
                        name: "FK_Repositories_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId");
                    table.ForeignKey(
                        name: "FK_Repositories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    PullRequestId = table.Column<long>(type: "bigint", nullable: false),
                    RepositoryRepoId = table.Column<long>(type: "bigint", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsMerged = table.Column<bool>(type: "bit", nullable: false),
                    MergedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => new {x.PullRequestId, x.RepositoryRepoId});
                    table.ForeignKey(
                        name: "FK_PullRequests_Repositories_RepositoryRepoId",
                        column: x => x.RepositoryRepoId,
                        principalTable: "Repositories",
                        principalColumn: "RepoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PullRequests_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUser_UsersUserId",
                table: "OrganizationUser",
                column: "UsersUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_AuthorUserId",
                table: "PullRequests",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_RepositoryRepoId",
                table: "PullRequests",
                column: "RepositoryRepoId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_OrganizationId",
                table: "Repositories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_UserId",
                table: "Repositories",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationUser");

            migrationBuilder.DropTable(
                name: "PullRequests");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
