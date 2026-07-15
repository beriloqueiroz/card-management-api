using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cards.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credit_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cardholder_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    nickname = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    brand = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    first_four_digits = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    last_four_digits = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pin_encrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    external_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_cards", x => x.id);
                    table.CheckConstraint("ck_credit_cards_credit_limit", "credit_limit >= 0");
                    table.CheckConstraint("ck_credit_cards_first_four_digits", "first_four_digits ~ '^[0-9]{4}$'");
                    table.CheckConstraint("ck_credit_cards_last_four_digits", "last_four_digits ~ '^[0-9]{4}$'");
                    table.CheckConstraint("ck_credit_cards_status", "status IN ('ACTIVE', 'BLOCKED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_credit_cards_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credit_cards_user_created_at",
                table: "credit_cards",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_credit_cards_user_expiration_date",
                table: "credit_cards",
                columns: new[] { "user_id", "expiration_date" });

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_cards");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
