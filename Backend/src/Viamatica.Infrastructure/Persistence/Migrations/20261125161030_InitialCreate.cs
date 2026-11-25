using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Viamatica.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attention_statuses",
                columns: table => new
                {
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attention_statuses", x => x.status_id);
                });

            migrationBuilder.CreateTable(
                name: "attention_types",
                columns: table => new
                {
                    attention_type_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prefix = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attention_types", x => x.attention_type_id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    client_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    identification = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reference_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    creation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.client_id);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                columns: table => new
                {
                    menu_item_id = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    route = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_items", x => x.menu_item_id);
                });

            migrationBuilder.CreateTable(
                name: "method_payments",
                columns: table => new
                {
                    method_payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_method_payments", x => x.method_payment_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "integer", nullable: false),
                    rol_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.rol_id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    service_description = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    speed_mbps = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "status_contracts",
                columns: table => new
                {
                    status_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_status_contracts", x => x.status_id);
                });

            migrationBuilder.CreateTable(
                name: "user_statuses",
                columns: table => new
                {
                    status_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_statuses", x => x.status_id);
                });

            migrationBuilder.CreateTable(
                name: "rol_menus",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "integer", nullable: false),
                    menu_item_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol_menus", x => new { x.rol_id, x.menu_item_id });
                    table.ForeignKey(
                        name: "fk_rol_menus_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "menu_item_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rol_menus_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    device_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.device_id);
                    table.ForeignKey(
                        name: "fk_devices_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    contract_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    method_payment_id = table.Column<int>(type: "integer", nullable: false),
                    previous_contract_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.contract_id);
                    table.ForeignKey(
                        name: "fk_contracts_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contracts_method_payments_method_payment_id",
                        column: x => x.method_payment_id,
                        principalTable: "method_payments",
                        principalColumn: "method_payment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contracts_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contracts_status_contracts_status_id",
                        column: x => x.status_id,
                        principalTable: "status_contracts",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    identification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rol_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    creation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_create = table.Column<int>(type: "integer", nullable: true),
                    user_approval = table.Column<int>(type: "integer", nullable: true),
                    date_approval = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    session_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_users_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "rol_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_users_user_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "user_statuses",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    method_payment_id = table.Column<int>(type: "integer", nullable: false),
                    user_cashier_id = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.payment_id);
                    table.ForeignKey(
                        name: "fk_payments_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payments_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payments_method_payments_method_payment_id",
                        column: x => x.method_payment_id,
                        principalTable: "method_payments",
                        principalColumn: "method_payment_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cashes",
                columns: table => new
                {
                    cash_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cash_description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    current_user_id = table.Column<int>(type: "integer", nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cashes", x => x.cash_id);
                    table.ForeignKey(
                        name: "fk_cashes_users_current_user_id",
                        column: x => x.current_user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "turns",
                columns: table => new
                {
                    turn_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cash_id = table.Column<int>(type: "integer", nullable: false),
                    user_gestor_id = table.Column<int>(type: "integer", nullable: false),
                    attention_type_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_turns", x => x.turn_id);
                    table.CheckConstraint("ck_turns_description", "description ~ '^[A-Z]{2}[0-9]{4}$'");
                    table.ForeignKey(
                        name: "fk_turns_attention_types_attention_type_id",
                        column: x => x.attention_type_id,
                        principalTable: "attention_types",
                        principalColumn: "attention_type_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_turns_cashes_cash_id",
                        column: x => x.cash_id,
                        principalTable: "cashes",
                        principalColumn: "cash_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_turns_users_user_gestor_id",
                        column: x => x.user_gestor_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_cashes",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    cash_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_by = table.Column<int>(type: "integer", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_cashes", x => new { x.user_id, x.cash_id });
                    table.ForeignKey(
                        name: "fk_user_cashes_cashes_cash_id",
                        column: x => x.cash_id,
                        principalTable: "cashes",
                        principalColumn: "cash_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_cashes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attentions",
                columns: table => new
                {
                    attention_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    turn_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    attention_type_id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    user_cashier_id = table.Column<int>(type: "integer", nullable: true),
                    attended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attentions", x => x.attention_id);
                    table.ForeignKey(
                        name: "fk_attentions_attention_statuses_status_id",
                        column: x => x.status_id,
                        principalTable: "attention_statuses",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attentions_attention_types_attention_type_id",
                        column: x => x.attention_type_id,
                        principalTable: "attention_types",
                        principalColumn: "attention_type_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attentions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "client_id");
                    table.ForeignKey(
                        name: "fk_attentions_turns_turn_id",
                        column: x => x.turn_id,
                        principalTable: "turns",
                        principalColumn: "turn_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attentions_users_user_cashier_id",
                        column: x => x.user_cashier_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.InsertData(
                table: "attention_statuses",
                columns: new[] { "status_id", "description" },
                values: new object[,]
                {
                    { 1, "Pendiente" },
                    { 2, "En atención" },
                    { 3, "Atendido" },
                    { 4, "Cancelado" }
                });

            migrationBuilder.InsertData(
                table: "attention_types",
                columns: new[] { "attention_type_id", "description", "prefix" },
                values: new object[,]
                {
                    { "ATC", "Atención al cliente", "AC" },
                    { "CAN", "Cancelación de contrato", "CC" },
                    { "CFP", "Cambio de forma de pago", "FP" },
                    { "CMS", "Cambio de servicio", "CM" },
                    { "CTR", "Contratación de servicio", "CS" },
                    { "PGS", "Pago de servicio", "PS" }
                });

            migrationBuilder.InsertData(
                table: "cashes",
                columns: new[] { "cash_id", "active", "cash_description", "current_user_id", "deleted_at", "is_deleted", "opened_at" },
                values: new object[,]
                {
                    { 1, true, "Caja 1", null, null, false, null },
                    { 2, true, "Caja 2", null, null, false, null },
                    { 3, true, "Caja 3", null, null, false, null }
                });

            migrationBuilder.InsertData(
                table: "menu_items",
                columns: new[] { "menu_item_id", "icon", "label", "order", "route" },
                values: new object[,]
                {
                    { 1, "home", "Inicio", 1, "/inicio" },
                    { 2, "dashboard", "Dashboard", 2, "/dashboard" },
                    { 3, "group", "Usuarios", 3, "/usuarios" },
                    { 4, "confirmation_number", "Asignación de turnos", 4, "/turnos" },
                    { 5, "point_of_sale", "Procesos de caja", 5, "/caja" },
                    { 6, "badge", "Clientes", 6, "/clientes" }
                });

            migrationBuilder.InsertData(
                table: "method_payments",
                columns: new[] { "method_payment_id", "description" },
                values: new object[,]
                {
                    { 1, "Efectivo" },
                    { 2, "Tarjeta de crédito" },
                    { 3, "Tarjeta de débito" },
                    { 4, "Transferencia bancaria" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "rol_id", "rol_name" },
                values: new object[,]
                {
                    { 1, "Administrador" },
                    { 2, "Gestor" },
                    { 3, "Cajero" }
                });

            migrationBuilder.InsertData(
                table: "services",
                columns: new[] { "service_id", "deleted_at", "is_deleted", "price", "service_description", "service_name", "speed_mbps" },
                values: new object[,]
                {
                    { 1, null, false, 25.00m, "Plan residencial de fibra óptica", "Internet Hogar 100", 100 },
                    { 2, null, false, 35.00m, "Plan residencial de fibra óptica de alta velocidad", "Internet Hogar 300", 300 },
                    { 3, null, false, 60.00m, "Plan para negocios con IP fija", "Internet Pyme 600", 600 }
                });

            migrationBuilder.InsertData(
                table: "status_contracts",
                columns: new[] { "status_id", "description" },
                values: new object[,]
                {
                    { "CAN", "Contrato cancelado" },
                    { "REN", "Renovación de servicio" },
                    { "SUS", "Contrato sustituido" },
                    { "VIG", "Contrato vigente" }
                });

            migrationBuilder.InsertData(
                table: "user_statuses",
                columns: new[] { "status_id", "description" },
                values: new object[,]
                {
                    { "ACT", "Activo" },
                    { "BLO", "Bloqueado" },
                    { "INA", "Inactivo" },
                    { "PEN", "Pendiente de aprobación" }
                });

            migrationBuilder.InsertData(
                table: "devices",
                columns: new[] { "device_id", "device_name", "service_id" },
                values: new object[,]
                {
                    { 1, "Router WiFi 5", 1 },
                    { 2, "Router WiFi 6", 2 },
                    { 3, "Extensor de señal", 2 },
                    { 4, "Router empresarial WiFi 6", 3 },
                    { 5, "ONT de fibra", 3 }
                });

            migrationBuilder.InsertData(
                table: "rol_menus",
                columns: new[] { "menu_item_id", "rol_id" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 1, 2 },
                    { 3, 2 },
                    { 4, 2 },
                    { 1, 3 },
                    { 5, 3 },
                    { 6, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_attention_types_prefix",
                table: "attention_types",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attentions_attention_type_id",
                table: "attentions",
                column: "attention_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_attentions_client_id",
                table: "attentions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_attentions_status_id",
                table: "attentions",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_attentions_turn_id",
                table: "attentions",
                column: "turn_id");

            migrationBuilder.CreateIndex(
                name: "ix_attentions_user_cashier_id",
                table: "attentions",
                column: "user_cashier_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashes_current_user_id",
                table: "cashes",
                column: "current_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_clients_identification",
                table: "clients",
                column: "identification",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_client_id",
                table: "contracts",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_method_payment_id",
                table: "contracts",
                column: "method_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_service_id",
                table: "contracts",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_status_id",
                table: "contracts",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_service_id",
                table: "devices",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_client_id",
                table: "payments",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_contract_id",
                table: "payments",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_method_payment_id",
                table: "payments",
                column: "method_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_rol_menus_menu_item_id",
                table: "rol_menus",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_turns_attention_type_id",
                table: "turns",
                column: "attention_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_turns_cash_id",
                table: "turns",
                column: "cash_id");

            migrationBuilder.CreateIndex(
                name: "ix_turns_date",
                table: "turns",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_turns_user_gestor_id",
                table: "turns",
                column: "user_gestor_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_cashes_cash_id",
                table: "user_cashes",
                column: "cash_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_rol_id",
                table: "users",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_status_id",
                table: "users",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attentions");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "rol_menus");

            migrationBuilder.DropTable(
                name: "user_cashes");

            migrationBuilder.DropTable(
                name: "attention_statuses");

            migrationBuilder.DropTable(
                name: "turns");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "menu_items");

            migrationBuilder.DropTable(
                name: "attention_types");

            migrationBuilder.DropTable(
                name: "cashes");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "method_payments");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "status_contracts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_statuses");
        }
    }
}
