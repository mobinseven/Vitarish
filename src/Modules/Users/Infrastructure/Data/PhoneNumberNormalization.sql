CREATE OR REPLACE FUNCTION pg_temp.vitarish_normalize_phone(input text)
RETURNS text
LANGUAGE plpgsql
IMMUTABLE
AS $function$
DECLARE
    value text;
BEGIN
    IF input IS NULL THEN
        RETURN NULL;
    END IF;

    -- Mirror UserPhoneNumberPolicy.TryNormalize: trim every .NET whitespace
    -- character, map Persian (U+06F0-U+06F9) and Arabic-Indic (U+0660-U+0669)
    -- digits to ASCII, then remove separators and Unicode direction marks.
    value := translate(
        btrim(input, E' \t\n\r\f\v\u001C\u001D\u001E\u001F\u0085\u00A0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u2028\u2029\u202F\u205F\u3000'),
        E'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩',
        '01234567890123456789');
    value := translate(value, E' \u00A0-().\u200E\u200F\u202A\u202B\u202C\u202D\u202E', '');

    IF value = '' THEN
        RETURN NULL;
    END IF;

    IF value LIKE '00%' THEN
        value := '+' || substr(value, 3);
    END IF;

    IF left(value, 1) <> '+' THEN
        IF length(value) = 11 AND left(value, 1) = '0' AND value ~ '^[0-9]+$' THEN
            value := '+98' || substr(value, 2);
        ELSIF length(value) = 10 AND left(value, 1) = '9' AND value ~ '^[0-9]+$' THEN
            value := '+98' || value;
        ELSIF length(value) = 12 AND left(value, 2) = '98' AND value ~ '^[0-9]+$' THEN
            value := '+' || value;
        ELSE
            RETURN NULL;
        END IF;
    END IF;

    IF value !~ '^\+[1-9][0-9]{1,14}$' THEN
        RETURN NULL;
    END IF;

    -- Iranian numbers must be mobile numbers (+989 followed by nine digits).
    IF value LIKE '+98%' AND value !~ '^\+989[0-9]{9}$' THEN
        RETURN NULL;
    END IF;

    RETURN value;
END;
$function$;
