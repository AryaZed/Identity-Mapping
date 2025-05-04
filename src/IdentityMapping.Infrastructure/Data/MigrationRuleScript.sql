-- Create MappingRules table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('MappingRules') AND type = 'U')
BEGIN
    CREATE TABLE MappingRules (
        Id NVARCHAR(50) PRIMARY KEY,
        ApplicationId NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        Condition NVARCHAR(1000) NULL,
        RuleType INT NOT NULL,
        SourceIdentifier NVARCHAR(100) NOT NULL,
        TargetIdentifier NVARCHAR(100) NULL,
        TransformExpression NVARCHAR(1000) NULL,
        Direction INT NOT NULL,
        Priority INT NOT NULL,
        IsEnabled BIT NOT NULL DEFAULT(1),
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL
    );

    CREATE INDEX IX_MappingRules_ApplicationId_RuleType_SourceIdentifier 
    ON MappingRules(ApplicationId, RuleType, SourceIdentifier);
    
    CREATE INDEX IX_MappingRules_Priority 
    ON MappingRules(Priority);
    
    PRINT 'Created MappingRules table';
END

-- Insert sample mapping rules
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('MappingRules') AND type = 'U')
    AND NOT EXISTS (SELECT 1 FROM MappingRules WHERE ApplicationId = 'default' AND Name = 'Email Normalization')
BEGIN
    -- Sample claim transformation rules
    INSERT INTO MappingRules (Id, ApplicationId, Name, Description, Condition, RuleType, SourceIdentifier, 
                             TargetIdentifier, TransformExpression, Direction, Priority, IsEnabled, CreatedAt, UpdatedAt)
    VALUES 
        -- Convert email to lowercase
        (NEWID(), 'default', 'Email Normalization', 'Converts email claims to lowercase', NULL, 0, 'email', 
         'email', 'lowercase', 0, 10, 1, GETUTCDATE(), GETUTCDATE()),
         
        -- Domain-specific rule
        (NEWID(), 'default', 'Corporate Email Rule', 'Special handling for corporate email addresses', 
         'sourceClaimValue.EndsWith("@corporate.com")', 0, 'email', 'verified_email', NULL, 0, 20, 1, GETUTCDATE(), GETUTCDATE()),
         
        -- Custom name formatting
        (NEWID(), 'default', 'Name Formatting', 'Proper case for display names', NULL, 0, 'name',
         'name', 'replace:ADMIN:Administrator', 0, 30, 1, GETUTCDATE(), GETUTCDATE()),
        
        -- Role transformation based on environment
        (NEWID(), 'default', 'Elevated Role in Dev', 'Elevates user roles in development environment', 
         'context["environment"] == "development" && sourceRole == "User"', 1, 'User', 'Admin', NULL, 0, 10, 0, GETUTCDATE(), GETUTCDATE()),
         
        -- Department-based role
        (NEWID(), 'default', 'HR Department Role', 'Maps department-specific roles', 
         'context.ContainsKey("department") && context["department"] == "HR"', 1, 'User', 'HRAccess', NULL, 0, 20, 1, GETUTCDATE(), GETUTCDATE()),

        -- Mobile number formatting
        (NEWID(), 'default', 'Mobile Number Formatting', 'Standardizes mobile number format', NULL, 0, 'phone_number',
         'phone_number', 'replace: :;replace:():;replace:-:', 0, 15, 1, GETUTCDATE(), GETUTCDATE()),
        
        -- Special permission for admin users
        (NEWID(), 'default', 'Admin Permission', 'Adds special permission claim for admins', 
         'sourceRole == "Admin" || sourceRole == "Administrator"', 0, 'role', 'permission', 'prefix:manage:', 0, 25, 1, GETUTCDATE(), GETUTCDATE()),
        
        -- Tenant-specific rule
        (NEWID(), 'default', 'Tenant ID Mapping', 'Maps tenant IDs between systems', 
         'context.ContainsKey("tenantId")', 0, 'tenant', 'organization', NULL, 2, 5, 1, GETUTCDATE(), GETUTCDATE());
        
    PRINT 'Inserted sample mapping rules';
END

PRINT 'Rules migration completed successfully';