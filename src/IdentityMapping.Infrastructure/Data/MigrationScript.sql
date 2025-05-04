-- Add MobileNumber and MobileVerified columns to UserIdentities table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('UserIdentities') AND name = 'MobileNumber')
BEGIN
    ALTER TABLE UserIdentities ADD MobileNumber NVARCHAR(50) NULL;
    PRINT 'Added MobileNumber column to UserIdentities table';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('UserIdentities') AND name = 'MobileVerified')
BEGIN
    ALTER TABLE UserIdentities ADD MobileVerified BIT NOT NULL DEFAULT(0);
    PRINT 'Added MobileVerified column to UserIdentities table';
END

-- Create RoleMappings table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('RoleMappings') AND type = 'U')
BEGIN
    CREATE TABLE RoleMappings (
        Id NVARCHAR(50) PRIMARY KEY,
        ApplicationId NVARCHAR(50) NOT NULL,
        LegacyRoleName NVARCHAR(100) NOT NULL,
        CentralizedRoleName NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        Description NVARCHAR(500) NULL
    );

    CREATE UNIQUE INDEX IX_RoleMappings_ApplicationId_LegacyRoleName 
    ON RoleMappings(ApplicationId, LegacyRoleName);
    
    CREATE UNIQUE INDEX IX_RoleMappings_ApplicationId_CentralizedRoleName 
    ON RoleMappings(ApplicationId, CentralizedRoleName);
    
    PRINT 'Created RoleMappings table';
END

-- Create ClaimMappings table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('ClaimMappings') AND type = 'U')
BEGIN
    CREATE TABLE ClaimMappings (
        Id NVARCHAR(50) PRIMARY KEY,
        ApplicationId NVARCHAR(50) NOT NULL,
        LegacyClaimType NVARCHAR(100) NOT NULL,
        CentralizedClaimType NVARCHAR(100) NOT NULL,
        ValueTransformation NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL,
        IncludeInCentralized BIT NOT NULL DEFAULT(1),
        IncludeInLegacy BIT NOT NULL DEFAULT(1),
        Description NVARCHAR(500) NULL
    );

    CREATE UNIQUE INDEX IX_ClaimMappings_ApplicationId_LegacyClaimType 
    ON ClaimMappings(ApplicationId, LegacyClaimType);
    
    CREATE UNIQUE INDEX IX_ClaimMappings_ApplicationId_CentralizedClaimType 
    ON ClaimMappings(ApplicationId, CentralizedClaimType);
    
    PRINT 'Created ClaimMappings table';
END

-- Insert some common role mappings
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('RoleMappings') AND type = 'U')
    AND NOT EXISTS (SELECT 1 FROM RoleMappings WHERE ApplicationId = 'default')
BEGIN
    INSERT INTO RoleMappings (Id, ApplicationId, LegacyRoleName, CentralizedRoleName, CreatedAt, Description)
    VALUES 
        (NEWID(), 'default', 'Admin', 'Administrator', GETUTCDATE(), 'Default mapping for Administrator role'),
        (NEWID(), 'default', 'User', 'User', GETUTCDATE(), 'Default mapping for User role'),
        (NEWID(), 'default', 'Manager', 'Manager', GETUTCDATE(), 'Default mapping for Manager role'),
        (NEWID(), 'default', 'ReadOnly', 'Reader', GETUTCDATE(), 'Default mapping for read-only role');
        
    PRINT 'Inserted default role mappings';
END

-- Insert some common claim mappings
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('ClaimMappings') AND type = 'U')
    AND NOT EXISTS (SELECT 1 FROM ClaimMappings WHERE ApplicationId = 'default')
BEGIN
    INSERT INTO ClaimMappings (Id, ApplicationId, LegacyClaimType, CentralizedClaimType, ValueTransformation, CreatedAt, Description)
    VALUES 
        (NEWID(), 'default', 'name', 'name', NULL, GETUTCDATE(), 'User name claim'),
        (NEWID(), 'default', 'email', 'email', NULL, GETUTCDATE(), 'Email claim'),
        (NEWID(), 'default', 'role', 'role', NULL, GETUTCDATE(), 'Role claim'),
        (NEWID(), 'default', 'sub', 'sub', NULL, GETUTCDATE(), 'Subject identifier'),
        (NEWID(), 'default', 'phone', 'phone_number', NULL, GETUTCDATE(), 'Phone number claim'),
        (NEWID(), 'default', 'given_name', 'given_name', NULL, GETUTCDATE(), 'Given name claim'),
        (NEWID(), 'default', 'family_name', 'family_name', NULL, GETUTCDATE(), 'Family name claim');
        
    PRINT 'Inserted default claim mappings';
END

PRINT 'Migration completed successfully'; 