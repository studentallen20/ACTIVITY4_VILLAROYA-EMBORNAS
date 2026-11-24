# Campus Equipment Borrowing System — Laboratory Activity 4

## Advanced Data-Driven Workflows and PostgreSQL Integration

### Course Information
- **Course**: ITSD 81 – Desktop Application Development
- **Laboratory Activity**: Activity 4 — Advanced Data-Driven Workflows and PostgreSQL Integration
- **Student Pair**:
  - **Mharc Allen Villaroya**
  - **Prince Lawrence Embornas**

---

## I. Executive Overview

Laboratory Activity 4 advances the **Campus Equipment Borrowing System** developed in Laboratory Activities 1 through 3. The system transitions its primary operational database from SQLite to **PostgreSQL 18** using **Entity Framework Core 10** and the **Npgsql** provider.

In this activity, the system retains and strengthens all working workflows from earlier activities while introducing 9 critical new data-driven workflows:
1. **Student Management** (registration, descriptive editing, borrowing eligibility toggles);
2. **Equipment Management** (hardware registration, barcode/identifier uniqueness, descriptive updates);
3. **Atomic Multi-Item Checkout** (coordinated multi-loan issuance within an all-or-nothing database transaction);
4. **Historical Lifecycle Logs** (asynchronous student loan history and equipment loan history);
5. **Multi-Criteria Search Engine** (case-insensitive search, status filtering, and multi-field sorting with stable tie-breakers).

---

## II. Clean Architectural Foundation

The solution preserves the strict separation of concerns established throughout the laboratory sequence:

```text
[Avalonia Desktop View (XAML)]
         │
         ▼
[ViewModel (CommunityToolkit.Mvvm)]
         │
         ▼
[Application Service (Domain Rules BR-01 – BR-19)]
         │
         ▼
[Repository Abstraction & Unit of Work (Interfaces)]
         │
         ▼
[EF Core Infrastructure (Npgsql.EntityFrameworkCore.PostgreSQL)]
         │
         ▼
[PostgreSQL 18 Relational Database]
```

### Architectural Principles Enforced:
1. **Views and ViewModels never contain database logic**: No SQL, connection strings, migrations, or direct `DbContext` calls reside inside Views or ViewModels.
2. **Inward Dependency Rule**: The `Domain` and `Application` layers contain pure C# business logic with zero dependencies on GUI frameworks or database providers.
3. **DbContext Lifetime Strategy in Desktop MVVM**:
   - `EquipmentBorrowingDbContext` is configured with **Transient lifetime** (and managed per-unit-of-work) to completely eliminate EF Core concurrency errors ("*A second operation was started on this context instance before a previous operation completed*") during concurrent asynchronous UI queries (such as simultaneously loading equipment catalog and loan history).
4. **Tracking vs. No-Tracking (`AsNoTracking`)**:
   - Read-only master-detail queries, search results, and history tables explicitly call `.AsNoTracking()` to bypass EF Core change tracker allocation, maximizing query throughput and minimizing memory footprint.
   - Operations that modify entity state (e.g. `ReturnEquipmentService`, `UpdateStudentService`) use tracked entities before calling `SaveChangesAsync()`.

---

## III. Implemented Use Cases (14 Actor-Level Scenarios)

All 14 required use cases from the Activity 4 Use Case Suite are fully implemented and verified:

### Required Continuations (5 Use Cases)
- **UC-01: View Equipment Catalog** — Retrieves all hardware items with identifiers, names, descriptions, and real-time availability states.
- **UC-02: Find Available Equipment** — Filters the catalog to only hardware items currently available for loan.
- **UC-03: Borrow Equipment (Single Item)** — Validates student eligibility, loan limit (max 3), item availability, and future expected return date before issuing a single item.
- **UC-04: View Active Borrowings** — Displays active loans with borrower student numbers, equipment barcodes, and due dates.
- **UC-05: Return Equipment** — Processes item return, records the actual return timestamp, restores equipment availability, and rejects duplicate return attempts without altering state.

### Required New Workflows (9 Use Cases)
- **UC-06: Check Out Multiple Equipment Items (Atomic Multi-Item)**:
  - Issues $\ge 2$ distinct items to an eligible student in a single database transaction.
  - Revalidates student eligibility, projected active count ($\le 3$), item availability, and future due dates.
  - Commits **all** borrowings and equipment status changes together or rolls back **all** changes if any failure occurs.
  - Includes a built-in test seam (`Simulate Transaction Failure`) to demonstrate safe rollback and zero partial writes (A06 rubric demonstration).
- **UC-07: View Student Borrowing History**:
  - Asynchronously loads complete active and returned borrowing records for a selected student.
  - Features responsive loading indicators, empty state notifications, and error recovery.
- **UC-08: View Equipment Borrowing History**:
  - Asynchronously loads the complete loan lifecycle for a selected equipment item, displaying past holders and return dates.
- **UC-09: Search, Filter, and Sort Borrowings**:
  - Multi-criteria search engine supporting case-insensitive text search (student name, student number, equipment name/identifier).
  - Status filtering (`All`, `Active`, `Returned`).
  - Sorting by Checkout Date (`BorrowDate`) or Due Date (`DueDate`), Ascending or Descending, with borrowing `Id` as a stable tie-breaker.
  - Includes a 1-click Reset button.
- **UC-12: Register Student**:
  - Registers a new student with required institutional student number, full name, and optional contact info.
  - Enforces case-insensitive, whitespace-trimmed uniqueness at both application and PostgreSQL constraint levels.
  - Default eligibility is `true` with a borrowing limit of 3.
- **UC-13: Update Student Information**:
  - Allows staff to update full name, contact information, and student number while preserving internal ID (`Id`) and all existing borrowing records.
  - Clearing optional contact info stores it as absent (`null`).
- **UC-14: Change Student Borrowing Eligibility**:
  - Permits or restricts student borrowing privileges.
  - Restriction blocks new checkouts, but does **not** cancel or prevent return of existing active loans.
- **UC-15: Register Equipment**:
  - Registers new physical equipment with required unique identifier (barcode/code), name, and optional description.
  - Enforces normalized uniqueness; newly registered items default to `IsAvailable = true`.
- **UC-16: Update Equipment Information**:
  - Allows descriptive updates (identifier, name, description) while protecting availability and loan status from manual editing.

---

## IV. PostgreSQL Schema & Database Integrity (BR-16)

The relational schema is managed through EF Core migrations targeting PostgreSQL:

### Database Diagram

```text
+-----------------------------------+          +-----------------------------------+
|             Students              |          |             Equipment             |
+-----------------------------------+          +-----------------------------------+
| Id (PK, Serial)                   |          | Id (PK, Serial)                   |
| StudentNumber (VARCHAR 50, UNIQUE)|          | Identifier (VARCHAR 50, UNIQUE)   |
| Name (VARCHAR 150, NOT NULL)      |          | Name (VARCHAR 150, NOT NULL)      |
| ContactInfo (VARCHAR 150, NULL)   |          | Description (VARCHAR 500, NULL)   |
| IsEligibleToBorrow (BOOL, DEFAULT)|          | IsAvailable (BOOL, DEFAULT TRUE)  |
| MaxBorrowLimit (INT, DEFAULT 3)   |          +-----------------------------------+
+-----------------------------------+                            │ 1
                  │ 1                                            │
                  │                                              │ *
                  │ *                          +-----------------------------------+
                  +───────────────────────────>|            Borrowings             |
                                               +-----------------------------------+
                                               | Id (PK, Serial)                   |
                                               | StudentId (FK -> Students.Id)     |
                                               | EquipmentId (FK -> Equipment.Id)  |
                                               | BorrowDate (TIMESTAMP, NOT NULL)  |
                                               | DueDate (TIMESTAMP, NOT NULL)     |
                                               | ReturnDate (TIMESTAMP, NULL)      |
                                               | Status (VARCHAR 20, NOT NULL)     |
                                               +-----------------------------------+
```

### PostgreSQL-Enforced Constraints:
1. **Institutional Student Number Uniqueness**:
   ```sql
   CREATE UNIQUE INDEX "IX_Students_StudentNumber" ON "Students" ("StudentNumber");
   ```
2. **Equipment Identifier Uniqueness**:
   ```sql
   CREATE UNIQUE INDEX "IX_Equipment_Identifier" ON "Equipment" ("Identifier");
   ```
3. **Single-Active-Borrowing Protection (BR-10, BR-16)**:
   ```sql
   CREATE UNIQUE INDEX "IX_Borrowings_SingleActiveBorrowingPerEquipment" 
   ON "Borrowings" ("EquipmentId") 
   WHERE "Status" = 'Active';
   ```
   > **Note**: This partial unique index guarantees at the PostgreSQL database engine level that no piece of equipment can have more than one `Active` borrowing concurrently, preventing race conditions even under concurrent checkouts.
4. **Foreign Key Integrity with Restrict Delete (BR-14)**:
   - Foreign keys to `Students` and `Equipment` are configured with `ON DELETE RESTRICT` to prevent accidental deletion of students or equipment with active or historical loans.

---

## V. Project Structure

```text
NEWLAB/ACTIVITY4/
│
├── EquipmentBorrowing.sln                       # Unified Solution File
├── docs/
│   ├── database-queries.sql                    # SQL Demonstration Queries & Generated SQL Inspection
│   ├── use-case-diagram.md                     # Mermaid UML Use Case & Architecture Diagram
│   └── traceability-matrix.md                  # Complete Traceability & Acceptance Matrix (UC-01–16)
│
├── src/
│   ├── EquipmentBorrowing.Domain/              # Pure POCO Domain Entities (BR-01–14)
│   │   ├── Borrowing.cs
│   │   ├── BorrowingStatus.cs
│   │   ├── Equipment.cs
│   │   └── Student.cs
│   │
│   ├── EquipmentBorrowing.Application/         # Application Layer (Interfaces, Models, Services)
│   │   ├── Interfaces/
│   │   │   ├── IBorrowingRepository.cs
│   │   │   ├── IEquipmentRepository.cs
│   │   │   ├── IStudentRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Models/
│   │   │   ├── BorrowingDetailDto.cs
│   │   │   ├── BorrowResult.cs
│   │   │   ├── MultiBorrowResult.cs
│   │   │   ├── OperationResult.cs
│   │   │   └── ReturnResult.cs
│   │   └── Services/
│   │       ├── BorrowEquipmentService.cs           # UC-03
│   │       ├── ChangeStudentEligibilityService.cs  # UC-14
│   │       ├── CheckOutMultipleEquipmentService.cs # UC-06
│   │       ├── GetActiveBorrowingsService.cs       # UC-04
│   │       ├── GetAllEquipmentService.cs           # UC-01
│   │       ├── GetAvailableEquipmentService.cs     # UC-02
│   │       ├── GetEquipmentBorrowingHistoryService.cs # UC-08
│   │       ├── GetStudentBorrowingHistoryService.cs   # UC-07
│   │       ├── RegisterEquipmentService.cs         # UC-15
│   │       ├── RegisterStudentService.cs           # UC-12
│   │       ├── ReturnEquipmentService.cs           # UC-05
│   │       ├── SearchBorrowingsService.cs          # UC-09
│   │       ├── UpdateEquipmentService.cs           # UC-16
│   │       └── UpdateStudentService.cs             # UC-13
│   │
│   ├── EquipmentBorrowing.Infrastructure/       # PostgreSQL EF Core Persistence & Migrations
│   │   ├── Migrations/
│   │   │   ├── 20261124131511_InitialCreate.cs
│   │   │   └── EquipmentBorrowingDbContextModelSnapshot.cs
│   │   ├── Persistence/
│   │   │   ├── Configurations/
│   │   │   │   ├── BorrowingConfiguration.cs
│   │   │   │   ├── EquipmentConfiguration.cs
│   │   │   │   └── StudentConfiguration.cs
│   │   │   ├── DatabaseInitializer.cs         # Safe seed data on startup
│   │   │   ├── EquipmentBorrowingDbContext.cs
│   │   │   └── EquipmentBorrowingDesignTimeDbContextFactory.cs
│   │   └── Repositories/
│   │       ├── EfBorrowingRepository.cs
│   │       ├── EfEquipmentRepository.cs
│   │       ├── EfStudentRepository.cs
│   │       └── EfUnitOfWork.cs
│   │
│   └── EquipmentBorrowing.Desktop/              # Avalonia 12 MVVM Desktop UI
│       ├── Assets/
│       │   └── avalonia-logo.ico
│       ├── ViewModels/
│       │   ├── ActiveBorrowingsViewModel.cs
│       │   ├── BorrowingHistoryViewModel.cs
│       │   ├── CheckoutViewModel.cs
│       │   ├── EquipmentCatalogViewModel.cs
│       │   ├── MainWindowViewModel.cs
│       │   ├── StudentManagementViewModel.cs
│       │   └── ViewModelBase.cs
│       ├── Views/
│       │   ├── ActiveBorrowingsView.axaml / .cs
│       │   ├── BorrowingHistoryView.axaml / .cs
│       │   ├── CheckoutView.axaml / .cs
│       │   ├── EquipmentCatalogView.axaml / .cs
│       │   ├── MainWindow.axaml / .cs
│       │   └── StudentManagementView.axaml / .cs
│       ├── App.axaml / .cs
│       ├── appsettings.json                    # Configurable secret-free connection string
│       ├── Program.cs
│       └── ViewLocator.cs
│
└── tests/
    └── EquipmentBorrowing.Tests/               # Automated Unit & Integration Tests (15 Tests)
        ├── MultiItemCheckoutTests.cs           # UC-06 & BR-08 to BR-15 tests
        ├── SingleBorrowAndReturnTests.cs       # UC-03 & UC-05 tests
        ├── StudentAndEquipmentManagementTests.cs # UC-12, UC-13, UC-14, UC-15, UC-16 tests
        └── SearchAndHistoryTests.cs            # UC-07, UC-08, UC-09 tests
```

---

## VI. Setup, Configuration & Execution Guide

### 1. Prerequisites
- **.NET 10.0 SDK** (Installed and verified)
- **PostgreSQL 18** (Service running on `localhost:5432`)

### 2. Secret-Free PostgreSQL Configuration
The application reads its PostgreSQL connection string from:
1. Environment variable: `POSTGRES_CONNECTION` (or `ConnectionStrings__DefaultConnection`)
2. `src/EquipmentBorrowing.Desktop/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=YOUR_PASSWORD_HERE"
     }
   }
   ```
> If no credentials are configured, the application falls back safely to default local credentials and provides an observable connection status banner with friendly error recovery without exposing passwords or crashing (BR-18).

### 3. Applying Database Migrations
Migrations are automatically applied on application launch via `DatabaseInitializer.InitializeAsync()`. 

To apply migrations manually via the command line:
```powershell
cd "NEWLAB\ACTIVITY4"
dotnet ef database update --project src\EquipmentBorrowing.Infrastructure --startup-project src\EquipmentBorrowing.Infrastructure
```

### 4. Running the Avalonia Desktop Application
```powershell
cd "NEWLAB\ACTIVITY4"
dotnet run --project src\EquipmentBorrowing.Desktop
```

### 5. Running Automated Unit Tests
```powershell
cd "NEWLAB\ACTIVITY4"
dotnet test tests\EquipmentBorrowing.Tests\EquipmentBorrowing.Tests.csproj
```
All **15 automated tests** pass with zero errors, validating:
- Multi-item atomic checkout (UC-06);
- Rejection of duplicate items, $<2$ items, invalid dates, and over-limit requests;
- Single borrow and return workflows (UC-03, UC-05);
- Student eligibility restriction blocking checkout while allowing returns (UC-14);
- Unique student numbers and equipment barcodes (UC-12, UC-15);
- Multi-criteria search, filtering, and sorting (UC-09).

---

## VII. Git Development History

The Git repository was initialized locally in `NEWLAB/ACTIVITY4` with progressive, milestone-based conventional commits:

```text
74bc2d4 test: add automated test suite for use cases, business rules, and atomic transactions
6644f06 feat(desktop): implement Avalonia MVVM desktop interface with multi-item checkout, search, and management
b700656 feat(infrastructure): add initial EF Core PostgreSQL migration
3418cc1 feat(infrastructure): configure PostgreSQL EF Core DbContext, entity mappings, and constraints
d5c16b6 feat(application): implement repository interfaces and application services for 14 use cases
4c9ac81 chore: add standard EquipmentBorrowing.sln
8cdae40 chore: add git configuration and solution file
bf4768a feat(domain): implement domain entities, enums, and business rules (BR-01 to BR-14)
```
