-- Check and fix recruiter role assignments
-- This ensures all users linked to Recruiter records have the "Recruiter" role

-- First, check which recruiters are missing the role
SELECT
    u.Id as UserId,
    u.Email,
    u.UserName,
    r.Id as RecruiterId,
    r.Name as RecruiterName,
    CASE WHEN ur.UserId IS NULL THEN 'MISSING ROLE' ELSE 'HAS ROLE' END as RoleStatus
FROM AspNetUsers u
INNER JOIN Recruiters r ON u.Id = r.UserId
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles role ON ur.RoleId = role.Id AND role.Name = 'Recruiter';

-- Add missing Recruiter role assignments
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT DISTINCT u.Id, role.Id
FROM AspNetUsers u
INNER JOIN Recruiters r ON u.Id = r.UserId
CROSS JOIN AspNetRoles role
WHERE role.Name = 'Recruiter'
  AND NOT EXISTS (
      SELECT 1
      FROM AspNetUserRoles ur2
      WHERE ur2.UserId = u.Id
        AND ur2.RoleId = role.Id
  );

-- Verify the fix
SELECT
    u.Email,
    u.UserName,
    r.Name as RecruiterName,
    role.Name as RoleName
FROM AspNetUsers u
INNER JOIN Recruiters r ON u.Id = r.UserId
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles role ON ur.RoleId = role.Id
WHERE role.Name = 'Recruiter';
