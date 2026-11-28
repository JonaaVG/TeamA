SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'Security.fn_PatientPracticePredicate', N'IF') IS NULL
    BEGIN
        EXEC(N'
            CREATE FUNCTION Security.fn_PatientPracticePredicate
            (
                @PatientId INT,
                @PatientUserId INT,
                @PracticeId INT
            )
            RETURNS TABLE
            WITH SCHEMABINDING
            AS
            RETURN
                SELECT 1 AS Allowed
                WHERE
                    @PatientUserId = CAST(SESSION_CONTEXT(N''UserId'') AS INT)
                    OR EXISTS (
                        SELECT 1
                        FROM Security.CurrentUserRole
                        WHERE RoleName IN (''AdministrativeStaff'', ''SystemAdministrator'', ''Specialist'')
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM dbo.GeneralPractitioner AS gp
                        INNER JOIN Security.CurrentUserRole AS currentRole
                            ON currentRole.RoleName = ''GeneralPractitioner''
                        WHERE gp.UserId = CAST(SESSION_CONTEXT(N''UserId'') AS INT)
                          AND gp.PracticeId = @PracticeId
                    )
                    OR EXISTS (
                        SELECT 1
                        FROM dbo.RecordAccess AS ra
                        WHERE ra.PatientId = @PatientId
                          AND ra.UserId = CAST(SESSION_CONTEXT(N''UserId'') AS INT)
                          AND ra.RevokedAt IS NULL
                    );
        ');
    END;

    -- A deployed database may already use this predicate on all three operations.
    IF (
        SELECT COUNT(*)
        FROM sys.security_predicates
        WHERE object_id = OBJECT_ID(N'Security.PatientPolicy')
          AND target_object_id = OBJECT_ID(N'dbo.Patient')
          AND (predicate_type = 0 OR operation IN (1, 2))
          AND CHARINDEX(N'fn_PatientPracticePredicate', predicate_definition) > 0
    ) <> 3
    BEGIN
        EXEC(N'
            ALTER SECURITY POLICY Security.PatientPolicy
                ALTER FILTER PREDICATE Security.fn_PatientPracticePredicate(PatientId, UserId, PracticeId) ON dbo.Patient,
                ALTER BLOCK PREDICATE Security.fn_PatientPracticePredicate(PatientId, UserId, PracticeId) ON dbo.Patient AFTER INSERT,
                ALTER BLOCK PREDICATE Security.fn_PatientPracticePredicate(PatientId, UserId, PracticeId) ON dbo.Patient AFTER UPDATE;
        ');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
