DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'identity') THEN
        CREATE SCHEMA identity;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS identity.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'identity') THEN
            CREATE SCHEMA identity;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
    CREATE TABLE identity.tenants (
        "Id" uuid NOT NULL,
        "Slug" character varying(64) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Type" character varying(32) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_tenants" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_tenants_Type" CHECK ("Type" IN ('noi_bo', 'b2b_khach_hang', 'dao_tao'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
    CREATE TABLE identity.users (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Email" character varying(320) NOT NULL,
        "PasswordHash" character varying(512) NOT NULL,
        "Role" character varying(32) NOT NULL,
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_users" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_users_Role" CHECK ("Role" IN ('admin', 'user', 'viewer')),
        CONSTRAINT "FK_users_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES identity.tenants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
    CREATE UNIQUE INDEX "IX_tenants_Slug" ON identity.tenants ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
    CREATE UNIQUE INDEX "IX_users_Email" ON identity.users ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
    CREATE INDEX "IX_users_TenantId" ON identity.users ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260902094756_InitialIdentity') THEN
    INSERT INTO identity.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260902094756_InitialIdentity', '9.0.19');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260909165231_EmailChuThuong') THEN
    ALTER TABLE identity.users ADD CONSTRAINT "CK_users_Email_chu_thuong" CHECK ("Email" = lower("Email"));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity.__ef_migrations_history WHERE "MigrationId" = '20260909165231_EmailChuThuong') THEN
    INSERT INTO identity.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260909165231_EmailChuThuong', '9.0.19');
    END IF;
END $EF$;
COMMIT;

