-- ============================================================================
-- ITSD 81 – Desktop Application Development
-- Laboratory Activity 4 — PostgreSQL Integration & Advanced Data-Driven Queries
-- System: Campus Equipment Borrowing System
-- Database: PostgreSQL 18
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. BASIC RETRIEVAL: All Equipment Catalog (UC-01)
-- ----------------------------------------------------------------------------
SELECT 
    "Id", 
    "Identifier", 
    "Name", 
    "Description", 
    "IsAvailable"
FROM "Equipment"
ORDER BY "Identifier" ASC;

-- ----------------------------------------------------------------------------
-- 2. FILTERING: Currently Available Equipment (UC-02, BR-10)
-- ----------------------------------------------------------------------------
SELECT 
    "Id", 
    "Identifier", 
    "Name", 
    "Description", 
    "IsAvailable"
FROM "Equipment"
WHERE "IsAvailable" = TRUE
ORDER BY "Name" ASC;

-- ----------------------------------------------------------------------------
-- 3. JOIN QUERY: Active Borrowings with Student and Equipment Details (UC-04)
-- ----------------------------------------------------------------------------
SELECT 
    b."Id" AS "BorrowingId",
    s."StudentNumber",
    s."Name" AS "StudentName",
    e."Identifier" AS "EquipmentCode",
    e."Name" AS "EquipmentName",
    b."BorrowDate",
    b."DueDate",
    b."Status"
FROM "Borrowings" b
INNER JOIN "Students" s ON b."StudentId" = s."Id"
INNER JOIN "Equipment" e ON b."EquipmentId" = e."Id"
WHERE b."Status" = 'Active'
ORDER BY b."DueDate" ASC, b."Id" ASC;

-- ----------------------------------------------------------------------------
-- 4. HISTORY QUERY: Complete Loan History for a Specific Student (UC-07)
-- ----------------------------------------------------------------------------
SELECT 
    b."Id" AS "BorrowingId",
    e."Identifier",
    e."Name" AS "EquipmentName",
    b."BorrowDate",
    b."DueDate",
    b."ReturnDate",
    b."Status"
FROM "Borrowings" b
INNER JOIN "Equipment" e ON b."EquipmentId" = e."Id"
WHERE b."StudentId" = 1
ORDER BY b."BorrowDate" DESC, b."Id" DESC;

-- ----------------------------------------------------------------------------
-- 5. HISTORY QUERY: Complete Loan History for a Specific Equipment Item (UC-08)
-- ----------------------------------------------------------------------------
SELECT 
    b."Id" AS "BorrowingId",
    s."StudentNumber",
    s."Name" AS "StudentName",
    b."BorrowDate",
    b."DueDate",
    b."ReturnDate",
    b."Status"
FROM "Borrowings" b
INNER JOIN "Students" s ON b."StudentId" = s."Id"
WHERE b."EquipmentId" = 2
ORDER BY b."BorrowDate" DESC, b."Id" DESC;

-- ----------------------------------------------------------------------------
-- 6. MULTI-CRITERIA SEARCH, FILTER & SORT (UC-09, BR-17)
-- Case-insensitive search on student name, student number, or equipment name,
-- with status filtering and expected return date ordering.
-- ----------------------------------------------------------------------------
SELECT 
    b."Id",
    s."StudentNumber",
    s."Name" AS "StudentName",
    e."Identifier" AS "EquipmentCode",
    e."Name" AS "EquipmentName",
    b."BorrowDate",
    b."DueDate",
    b."ReturnDate",
    b."Status"
FROM "Borrowings" b
INNER JOIN "Students" s ON b."StudentId" = s."Id"
INNER JOIN "Equipment" e ON b."EquipmentId" = e."Id"
WHERE 
    (b."Status" = 'Active' OR 'Active' IS NULL)
    AND (
        LOWER(s."Name") LIKE LOWER('%laptop%')
        OR LOWER(s."StudentNumber") LIKE LOWER('%laptop%')
        OR LOWER(e."Name") LIKE LOWER('%laptop%')
        OR LOWER(e."Identifier") LIKE LOWER('%laptop%')
    )
ORDER BY b."DueDate" ASC, b."Id" ASC;

-- ----------------------------------------------------------------------------
-- 7. AGGREGATE QUERIES: Active Loan Count per Student (BR-09 Limit Verification)
-- ----------------------------------------------------------------------------
SELECT 
    s."Id" AS "StudentId",
    s."StudentNumber",
    s."Name",
    s."IsEligibleToBorrow",
    s."MaxBorrowLimit",
    COUNT(b."Id") AS "CurrentActiveLoans"
FROM "Students" s
LEFT JOIN "Borrowings" b ON s."Id" = b."StudentId" AND b."Status" = 'Active'
GROUP BY s."Id", s."StudentNumber", s."Name", s."IsEligibleToBorrow", s."MaxBorrowLimit"
ORDER BY "CurrentActiveLoans" DESC;

-- ----------------------------------------------------------------------------
-- 8. AGGREGATE QUERIES: Equipment Utilization & Total Borrowing Frequency
-- ----------------------------------------------------------------------------
SELECT 
    e."Id",
    e."Identifier",
    e."Name",
    e."IsAvailable",
    COUNT(b."Id") AS "TotalTimesBorrowed"
FROM "Equipment" e
LEFT JOIN "Borrowings" b ON e."Id" = b."EquipmentId"
GROUP BY e."Id", e."Identifier", e."Name", e."IsAvailable"
ORDER BY "TotalTimesBorrowed" DESC;

-- ----------------------------------------------------------------------------
-- 9. SAFE DESCRIPTIVE UPDATES: Preserving Relationships (UC-13, UC-16)
-- ----------------------------------------------------------------------------
-- Update Student descriptive data (clears contact if NULL, preserves PK & loans)
UPDATE "Students"
SET 
    "StudentNumber" = '2024-0001',
    "Name" = 'Mharc Allen Villaroya',
    "ContactInfo" = '09123456789'
WHERE "Id" = 1;

-- Update Equipment descriptive data (preserves availability & loan history)
UPDATE "Equipment"
SET 
    "Identifier" = 'EQ-001',
    "Name" = 'Epson High-Lumen LCD Projector',
    "Description" = 'Updated lamp unit 3800 lumens HDMI/VGA'
WHERE "Id" = 1;

-- ----------------------------------------------------------------------------
-- 10. GENERATED SQL INSPECTION (LINQ Translation Analysis)
-- ----------------------------------------------------------------------------

/*
===============================================================================
EXAMPLE 1: Active Borrowings Query (LINQ vs Generated PostgreSQL SQL)
===============================================================================
LINQ Expression:
    await _context.Borrowings
        .AsNoTracking()
        .Include(b => b.Student)
        .Include(b => b.Equipment)
        .Where(b => b.Status == BorrowingStatus.Active)
        .OrderBy(b => b.DueDate)
        .ThenBy(b => b.Id)
        .ToListAsync(ct);

Generated PostgreSQL SQL:
    SELECT b."Id", b."BorrowDate", b."DueDate", b."EquipmentId", b."ReturnDate", 
           b."Status", b."StudentId", s."Id", s."ContactInfo", s."IsEligibleToBorrow", 
           s."MaxBorrowLimit", s."Name", s."StudentNumber", e."Id", e."Description", 
           e."Identifier", e."IsAvailable", e."Name"
    FROM "Borrowings" AS b
    LEFT JOIN "Students" AS s ON b."StudentId" = s."Id"
    LEFT JOIN "Equipment" AS e ON b."EquipmentId" = e."Id"
    WHERE b."Status" = 'Active'
    ORDER BY b."DueDate", b."Id";

Explanation:
    EF Core translates the navigation property `.Include()` calls into SQL `LEFT JOIN`
    operations against Students and Equipment. The enum `BorrowingStatus.Active` is mapped
    via string conversion to 'Active'. `AsNoTracking()` informs EF Core to bypass change-tracker
    allocation, improving memory efficiency and query throughput for read-only presentation.
===============================================================================

===============================================================================
EXAMPLE 2: Single-Active-Borrowing Partial Unique Index Enforcement (BR-10, BR-16)
===============================================================================
EF Core Model Configuration:
    builder.HasIndex(b => b.EquipmentId)
        .IsUnique()
        .HasFilter("\"Status\" = 'Active'")
        .HasDatabaseName("IX_Borrowings_SingleActiveBorrowingPerEquipment");

Generated Migration DDL:
    CREATE UNIQUE INDEX "IX_Borrowings_SingleActiveBorrowingPerEquipment" 
    ON "Borrowings" ("EquipmentId") 
    WHERE "Status" = 'Active';

Explanation:
    PostgreSQL partial indexes physically enforce business rule BR-10 at the database
    storage engine level. Multiple returned records ("Status" = 'Returned') can exist
    for the same equipment, but at most ONE active borrowing ("Status" = 'Active') can
    ever be committed, protecting against race conditions and concurrent double-checkouts.
===============================================================================
*/
