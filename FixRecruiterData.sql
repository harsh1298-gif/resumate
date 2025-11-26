-- Fix existing recruiter accounts that are missing email addresses
-- This updates Recruiter records to match their associated IdentityUser email

UPDATE r
SET r.Email = u.Email
FROM Recruiters r
INNER JOIN AspNetUsers u ON r.UserId = u.Id
WHERE r.Email IS NULL OR r.Email = '';

-- Verify the fix
SELECT
    r.Id,
    r.Name,
    r.Email as RecruiterEmail,
    u.Email as UserEmail,
    u.UserName
FROM Recruiters r
INNER JOIN AspNetUsers u ON r.UserId = u.Id;
