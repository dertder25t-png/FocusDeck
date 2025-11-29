#!/bin/bash
# DEVELOPER_QUICK_START.sh
# Copy and paste commands from this file to run tests in your environment

echo "==========================================="
echo "FocusDeck Developer Environment Quick Start"
echo "==========================================="

# Navigate to project
cd /root/FocusDeck

# Step 1: Verify dotnet is installed
echo "Step 1: Verifying .NET SDK..."
dotnet --version

# Step 2: Clean previous builds
echo "Step 2: Cleaning previous builds..."
dotnet clean

# Step 3: Restore dependencies
echo "Step 3: Restoring NuGet packages..."
dotnet restore

# Step 4: Build the project
echo "Step 4: Building the project..."
dotnet build

# Step 5: Run all tests
echo "Step 5: Running all tests..."
dotnet test

# Step 6: If you want just the authentication tests
# echo "Step 6: Running authentication tests only..."
# dotnet test tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj \
#   --filter "ClassName=AutomationWorkflowIntegrationTests" \
#   --verbosity=detailed

echo "==========================================="
echo "Test run complete!"
echo "==========================================="
