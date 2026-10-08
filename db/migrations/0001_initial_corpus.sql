DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'corpus') THEN
        CREATE SCHEMA corpus;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS corpus.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'corpus') THEN
            CREATE SCHEMA corpus;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'corpus') THEN
            CREATE SCHEMA corpus;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
    CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
    CREATE TABLE corpus.documents (
        "Id" uuid NOT NULL,
        "TenantId" uuid,
        "DocumentNumber" character varying(100) NOT NULL,
        "Title" character varying(1000) NOT NULL,
        "EffectiveFrom" date,
        "EffectiveTo" date,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_documents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
    CREATE INDEX "IX_documents_DocumentNumber" ON corpus.documents ("DocumentNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
    CREATE INDEX "IX_documents_EffectiveFrom_EffectiveTo" ON corpus.documents ("EffectiveFrom", "EffectiveTo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
    CREATE INDEX "IX_documents_TenantId" ON corpus.documents ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM corpus.__ef_migrations_history WHERE "MigrationId" = '20260816133510_InitialCorpus') THEN
    INSERT INTO corpus.__ef_migrations_history ("MigrationId", "ProductVersion")
    VALUES ('20260816133510_InitialCorpus', '9.0.19');
    END IF;
END $EF$;
COMMIT;

