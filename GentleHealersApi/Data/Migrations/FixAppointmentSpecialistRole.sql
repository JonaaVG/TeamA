-- Use the current single-role schema for the existing appointment checks.
CREATE OR ALTER TRIGGER dbo.TR_Appointment_SpecialistFollowUpCheck
ON dbo.Appointment
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Security.CurrentUserRole WHERE RoleName = 'Specialist')
        RETURN;

    DECLARE @CurrentUserId int = CONVERT(int, SESSION_CONTEXT(N'UserId'));
    DECLARE @SpecialistId int = (
        SELECT SpecialistId FROM dbo.Specialist WHERE UserId = @CurrentUserId
    );

    IF @SpecialistId IS NULL OR EXISTS (SELECT 1 FROM inserted WHERE SpecialistId <> @SpecialistId)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50010, 'Specialists can only schedule follow-up appointments for themselves.', 1;
    END;

    IF EXISTS (
        SELECT 1 FROM inserted AS i
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Appointment AS a
            WHERE a.PatientId = i.PatientId AND a.SpecialistId = @SpecialistId
              AND a.AppointmentId <> i.AppointmentId
              AND NOT EXISTS (SELECT 1 FROM inserted AS newAppointment WHERE newAppointment.AppointmentId = a.AppointmentId)
        )
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, 'Specialists can only schedule follow-ups for patients with a prior appointment.', 1;
    END;

    IF EXISTS (SELECT 1 FROM inserted WHERE CAST(StartTime AS date) <> CAST(dbo.GetLocalTime() AS date))
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50012, 'Specialist follow-up appointments must start today.', 1;
    END;
END;
GO
