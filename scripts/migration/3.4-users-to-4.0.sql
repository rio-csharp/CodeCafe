-- Carries CodeCafe 3.4 accounts into the 4.0 users table.
--
-- 4.0 rebuilt the schema instead of evolving it: 3.4 keeps accounts in "AspNetUsers", 4.0 keeps
-- them in users, and neither migration knows about the other. This copy is therefore the only
-- thing that carries accounts across, and it has to run after the 4.0 migration job has created
-- users and before the 4.0 API takes traffic.
--
-- Run it against a database holding BOTH schemas -- that is what a production upgrade looks like,
-- because the 4.0 migration job adds its tables next to the 3.4 ones. It is idempotent, so a
-- re-run is safe:
--
--   psql "$CONNECTION_STRING" -v ON_ERROR_STOP=1 -f scripts/migration/3.4-users-to-4.0.sql
--
-- Accounts only. Notebooks, pages and blocks are deliberately left behind: 3.4 stored a page as a
-- single ContentJson document with PlainTextContent beside it, 4.0 stores normalized block rows,
-- so carrying content needs a TipTap-JSON-to-blocks converter that does not exist.

BEGIN;

-- Fail loudly on a database where the 3.4 schema is already gone, rather than reporting a
-- successful migration of nothing.
DO $$
BEGIN
    IF to_regclass('public."AspNetUsers"') IS NULL THEN
        RAISE EXCEPTION 'AspNetUsers is missing: this database holds no 3.4 accounts to migrate.';
    END IF;
END
$$;

-- Rows the copy below will skip, listed before it runs so they are visible rather than inferred
-- from a smaller count.
SELECT u."Id", u."Email"
FROM "AspNetUsers" u
WHERE u."Email" IS NULL
   OR btrim(u."Email") = ''
   OR u."PasswordHash" IS NULL
   OR u."PasswordHash" = '';

-- The one non-obvious column. 3.4 used ASP.NET Identity, which normalizes an email to UPPER
-- invariant for NormalizedEmail; 4.0 normalizes to lower invariant and finds accounts by that
-- column. Copying the value verbatim would migrate every account into a state where the stored
-- password is correct and the lookup still misses, so it is recomputed from Email instead.
--
-- PasswordChangedAtUtc has no 3.4 counterpart. 4.0 uses it to reject access tokens issued before
-- a password change, so the oldest defensible answer -- the account's creation time -- is used;
-- it invalidates nothing that a later sign-in produces.
INSERT INTO users (
    "Id", "Email", "NormalizedEmail", "DisplayName", "PasswordHash",
    "CreatedAtUtc", "PasswordChangedAtUtc"
)
SELECT
    u."Id",
    btrim(u."Email"),
    lower(btrim(u."Email")),
    btrim(u."DisplayName"),
    u."PasswordHash",
    u."CreatedAtUtc",
    u."CreatedAtUtc"
FROM "AspNetUsers" u
WHERE u."Email" IS NOT NULL
  AND btrim(u."Email") <> ''
  AND u."PasswordHash" IS NOT NULL
  AND u."PasswordHash" <> ''
-- An id that is already present means this row was migrated before. An email that is already
-- taken by a different id is a real conflict and aborts the transaction instead of dropping the
-- account, which is the right way round: nothing is half-migrated.
ON CONFLICT ("Id") DO NOTHING;

SELECT count(*) AS accounts_after_migration FROM users;

COMMIT;
