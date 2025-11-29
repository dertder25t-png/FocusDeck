CREATE TABLE "ServiceConfigurations" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_ServiceConfigurations" PRIMARY KEY,
    "ServiceName" TEXT NOT NULL,
    "ClientId" TEXT NULL,
    "ClientSecret" TEXT NULL,
    "ApiKey" TEXT NULL,
    "AdditionalConfig" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "TenantId" TEXT NOT NULL
);
