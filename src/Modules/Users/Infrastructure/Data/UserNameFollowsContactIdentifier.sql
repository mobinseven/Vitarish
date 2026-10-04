DO $$
DECLARE
    account_record record;
    target_user_name text;
BEGIN
    FOR account_record IN
        SELECT
            "Id",
            "UserName",
            "Email",
            "PhoneNumber",
            CASE
                WHEN "UserName" LIKE '%@%' AND "Email" IS NOT NULL AND "UserName" <> "Email"
                    THEN "Email"
                WHEN "UserName" LIKE '%@%' AND "Email" IS NULL AND "PhoneNumber" IS NOT NULL AND "UserName" <> "PhoneNumber"
                    THEN "PhoneNumber"
                WHEN "UserName" ~ '^\+?[0-9]{9,15}$' AND "PhoneNumber" IS NOT NULL AND "UserName" <> "PhoneNumber"
                    THEN "PhoneNumber"
                WHEN "UserName" ~ '^\+?[0-9]{9,15}$' AND "PhoneNumber" IS NULL AND "Email" IS NOT NULL AND "UserName" <> "Email"
                    THEN "Email"
                ELSE NULL
            END AS new_user_name
        FROM "Users"."AspNetUsers"
    LOOP
        target_user_name := account_record.new_user_name;
        CONTINUE WHEN target_user_name IS NULL;

        IF EXISTS (
            SELECT 1
            FROM "Users"."AspNetUsers" AS other
            WHERE other."Id" <> account_record."Id"
              AND other."NormalizedUserName" = upper(target_user_name)
        ) THEN
            RAISE NOTICE 'User name follow skipped for account ID %; the target user name is already taken.', account_record."Id";
        ELSE
            UPDATE "Users"."AspNetUsers"
            SET "UserName" = target_user_name,
                "NormalizedUserName" = upper(target_user_name)
            WHERE "Id" = account_record."Id";
        END IF;
    END LOOP;
END $$;
