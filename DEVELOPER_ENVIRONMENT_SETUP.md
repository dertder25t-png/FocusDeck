# Developer Environment Setup - .NET Testing & Build Guide

## ✅ CONFIRMED: .NET SDK IS INSTALLED

Your environment has **.NET SDK 9.0.306** installed and ready to use.

```
Location:  /usr/local/dotnet/sdk/9.0.306/
Executable: /usr/bin/dotnet (symlink to above)
Version:   9.0.306
Runtime:   9.0.10
Platform:  Linux x86_64 (Debian 12)
```

---

## 🚀 QUICK START: Running Tests

### **Method 1: Direct Command (Recommended)**

```bash
cd /root/FocusDeck
dotnet test
```

Or with options:

```bash
# Run specific test project
dotnet test tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj

# Run with verbose output
dotnet test --verbosity=detailed

# Run specific test class
dotnet test --filter ClassName=AutomationWorkflowIntegrationTests

# Run and show detailed test output
dotnet test --logger "console;verbosity=detailed"
```

### **Method 2: Build First, Then Test**

```bash
# Clean build
dotnet clean
dotnet build --configuration Debug

# Then run tests
dotnet test
```

### **Method 3: For Development (Watch Mode)**

```bash
# Build in watch mode (re-compiles on file changes)
dotnet watch --project tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj test
```

---

## 🔍 ENVIRONMENT VERIFICATION

If you need to verify your environment is set up correctly:

```bash
# Check dotnet installation
which dotnet
# Output: /usr/bin/dotnet

# Check version
dotnet --version
# Output: 9.0.306

# Check full info
dotnet --info

# Check if you can restore packages
cd /root/FocusDeck
dotnet restore

# Check if you can build
dotnet build

# Check if you can build release (what production uses)
dotnet build --configuration Release
```

---

## 📋 RUNNING TESTS - STEP BY STEP

### **Step 1: Navigate to project directory**
```bash
cd /root/FocusDeck
```

### **Step 2: Restore dependencies**
```bash
dotnet restore
```

### **Step 3: Build the project**
```bash
dotnet build
```

### **Step 4: Run tests**
```bash
dotnet test
```

### **Step 5: Check test results**
Look for:
- ✅ **Passed tests** - Test succeeded
- ❌ **Failed tests** - Test failed (shows error message)
- ⏭️ **Skipped tests** - Test was skipped

---

## 🎯 RUNNING SPECIFIC TESTS (Authentication Focus)

### **Run only automation workflow tests:**
```bash
dotnet test tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj \
  --filter "ClassName=AutomationWorkflowIntegrationTests"
```

### **Run only JWT tests:**
```bash
dotnet test tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj \
  --filter "ClassName=JwtAuthenticationTests" \
  --verbosity=detailed
```

### **Run tests with specific name pattern:**
```bash
dotnet test --filter "Name~CreateWorkflow"
```

### **Run a single test method:**
```bash
dotnet test --filter "FullyQualifiedName~AutomationWorkflowIntegrationTests.CreateWorkflow_ReturnsCreatedWorkflow"
```

---

## 🛠️ TROUBLESHOOTING

### **Error: "dotnet command not found"**

**Solution 1**: Use full path
```bash
/usr/bin/dotnet test
```

**Solution 2**: Check PATH environment variable
```bash
echo $PATH
# Should include /usr/bin or /usr/local/dotnet/bin
```

**Solution 3**: Add to PATH if missing
```bash
export PATH="/usr/local/dotnet:$PATH"
dotnet test
```

### **Error: "No projects in solution"**

**Solution**: Make sure you're in the correct directory
```bash
pwd
# Should show: /root/FocusDeck

ls -la FocusDeck.sln
# Should list the solution file
```

### **Error: "Package restore failed"**

**Solution**: Clear cache and restore
```bash
dotnet nuget locals all --clear
dotnet restore
```

### **Error: "Build failed"**

**Solution**: Clean and rebuild
```bash
dotnet clean
dotnet build --configuration Debug
```

### **Tests hang or timeout**

**Solution**: Run with timeout and detailed logging
```bash
timeout 120 dotnet test --verbosity=detailed --logger="console;verbosity=detailed"
```

### **Out of memory or resource issues**

**Solution**: Run tests in single-threaded mode
```bash
dotnet test -- RunConfiguration.MaxCpuCount=1
```

---

## 📊 UNDERSTANDING TEST OUTPUT

When you run `dotnet test`, you'll see output like:

```
Test run for /root/FocusDeck/tests/FocusDeck.Server.Tests/bin/Debug/net9.0/FocusDeck.Server.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1

Starting test execution, please wait...

[xUnit.net 00:00:05.25]     FocusDeck.Server.Tests.AutomationWorkflowIntegrationTests.CreateWorkflow_ReturnsCreatedWorkflow [PASS]
[xUnit.net 00:00:06.01]     FocusDeck.Server.Tests.JwtAuthenticationTests.ValidToken_Returns200 [PASS]
[xUnit.net 00:00:06.68]     FocusDeck.Server.Tests.AuthorizationTests.UnauthorizedRequest_Returns401 [PASS]

Test Run Successful.
Total tests: 3
     Passed: 3
     Failed: 0
Skipped: 0
Total time: 1.234 s
```

**Meaning:**
- ✅ **PASS** = Test succeeded
- ❌ **FAIL** = Test failed (details shown after summary)
- ⏸️ **SKIP** = Test was skipped (not run)

---

## 🔧 USEFUL TEST COMMANDS

### **Run all tests with coverage report:**
```bash
dotnet test /p:CollectCoverage=true
```

### **Run tests and output XML report:**
```bash
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### **Run tests matching a pattern:**
```bash
dotnet test --filter "Name~Auth"  # Runs all tests with "Auth" in the name
```

### **Run tests and fail on first failure:**
```bash
dotnet test --no-build -- RunConfiguration.StopOnFirstFailure=true
```

### **Run with different log levels:**
```bash
# Minimal output
dotnet test -q

# Normal output
dotnet test

# Detailed output
dotnet test --verbosity=detailed

# Very detailed
dotnet test -v=diagnostic
```

---

## 📍 PROJECT PATHS YOU NEED

```
Project Root:     /root/FocusDeck
Solution File:    /root/FocusDeck/FocusDeck.sln

Test Project:     /root/FocusDeck/tests/FocusDeck.Server.Tests/
Test Files:       /root/FocusDeck/tests/FocusDeck.Server.Tests/*.cs

Server Project:   /root/FocusDeck/src/FocusDeck.Server/
Server Code:      /root/FocusDeck/src/FocusDeck.Server/src/**/*.cs

Build Output:     /root/FocusDeck/src/FocusDeck.Server/bin/
Test Output:      /root/FocusDeck/tests/FocusDeck.Server.Tests/bin/
```

---

## 🚀 FOR YOUR DEVELOPER: EXACT COMMANDS TO RUN NOW

### **Start here - Step by step:**

```bash
# 1. Navigate to project
cd /root/FocusDeck

# 2. Clean everything
dotnet clean

# 3. Restore packages (download dependencies)
dotnet restore

# 4. Build the project
dotnet build

# 5. Run the authentication test specifically
dotnet test tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj \
  --filter "ClassName=AutomationWorkflowIntegrationTests" \
  --verbosity=detailed

# 6. If that works, run ALL tests
dotnet test --verbosity=detailed
```

### **Watch for authentication fixes - Run this after making changes:**

```bash
# Quick build and test
dotnet build && dotnet test \
  --filter "ClassName=AutomationWorkflowIntegrationTests" \
  --no-build
```

---

## ✅ VERIFICATION: YOUR ENVIRONMENT IS READY

To confirm everything works, run this test command:

```bash
cd /root/FocusDeck && dotnet test -q
```

You should see:
- ✅ Tests starting to run
- ✅ Tests passing or failing (shows which ones)
- ✅ Summary at the end with total passed/failed count

If you see this, your environment is **completely set up and ready**.

---

## 📞 HELP WITH SPECIFIC ISSUES

If tests are **still failing with 401 Unauthorized**:

```bash
# Run with maximum detail
dotnet test tests/FocusDeck.Server.Tests/FocusDeck.Server.Tests.csproj \
  --filter "Name~CreateWorkflow" \
  --verbosity=diagnostic \
  --logger="console;verbosity=diagnostic"
```

This will show:
- Exact error message
- Full stack trace
- What the test expected vs what it got

Paste this output if you need help debugging.

---

## 📚 REFERENCE: DOTNET CLI COMMANDS

```
dotnet --version           # Show .NET version
dotnet --info             # Show detailed .NET info
dotnet restore            # Download dependencies
dotnet build              # Compile code
dotnet build -c Release   # Build release version
dotnet clean              # Remove build artifacts
dotnet test               # Run tests
dotnet test -q            # Run tests quietly (less output)
dotnet test -v=detailed   # Run tests with detailed output
dotnet watch test         # Run tests in watch mode
dotnet run                # Run the application
dotnet pack               # Create NuGet package
```

---

## 🎯 NEXT STEPS FOR YOUR DEVELOPER

1. ✅ Confirm environment: Run `dotnet --version` → Should show 9.0.306
2. ✅ Navigate to project: `cd /root/FocusDeck`
3. ✅ Run tests: `dotnet test`
4. ✅ If tests fail, paste the output in error messages
5. ✅ If tests pass, begin working on authentication fixes

---

**You're all set! Your developer can now run tests and verify their authentication fixes.**
