-- Fix Applicant table column names
USE ResuMateDB;
GO

-- Rename columns to match the updated model
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'Cys')
    EXEC sp_rename 'Applicants.Cys', 'City', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'DataEdit')
    EXEC sp_rename 'Applicants.DataEdit', 'DateOfBirth', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'FrameView')
    EXEC sp_rename 'Applicants.FrameView', 'PhoneNumber', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'ReferenceSummary')
    EXEC sp_rename 'Applicants.ReferenceSummary', 'ProfessionalSummary', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'Object')
    EXEC sp_rename 'Applicants.Object', 'Objective', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'DefaultDataPath')
    EXEC sp_rename 'Applicants.DefaultDataPath', 'ProfilePhotoPath', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'ReturnFilePath')
    EXEC sp_rename 'Applicants.ReturnFilePath', 'ResumeFilePath', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'ReturnFileName')
    EXEC sp_rename 'Applicants.ReturnFileName', 'ResumeFileName', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'Traceback')
    EXEC sp_rename 'Applicants.Traceback', 'ResumeUploadDate', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'Subset')
    EXEC sp_rename 'Applicants.Subset', 'CreatedAt', 'COLUMN';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'Console')
    EXEC sp_rename 'Applicants.Console', 'UpdatedAt', 'COLUMN';

PRINT 'Column names updated successfully!';
GO
