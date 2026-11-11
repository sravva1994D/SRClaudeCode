# Brainstorm Review: Patient Management Application

**Review Date**: 2026-11-10  
**Reviewed By**: BrainstormingAgent  
**Status**: ✅ Ready for Planning Phase  
**Confidence Level**: HIGH

---

## Executive Summary

### Project Overview
A web-based Patient Management Application designed for a single general physician to streamline daily clinical activities. The system addresses manual paperwork inefficiencies by digitizing patient records, appointment scheduling, consultation workflows, and prescription management.

**Project Type**: Healthcare Vertical SaaS (Single-Clinic Edition)  
**Complexity**: Medium (straightforward domain, single user, no multi-tenancy)  
**Phase**: Phase 1 (MVP - Single Doctor, Single Clinic)  

### Business Objectives
1. Eliminate manual paper-based patient record systems
2. Reduce consultation time from current state to 2-3 minutes per patient
3. Provide fast patient lookup (<5 seconds) with complete history
4. Generate digital prescriptions with printing capability
5. Enable data portability through CSV/PDF export
6. Achieve 80% reduction in paper usage

### Expected Outcomes
- Faster patient consultation workflow
- Centralized, searchable patient records
- Reduced risk of data loss or incomplete records
- Improved clinic efficiency and professionalism
- Patient satisfaction through faster service
- Data-backed clinic analytics capability

### Key Stakeholders
- **Clinic Owner/Doctor**: Primary user, product sponsor
- **Product Owner**: Requirements authority
- **Development Team**: Implementation responsibility
- **Future Secondary Users**: Receptionist (Phase 2), additional doctors (Phase 2+)

---

## Product Vision and Goals

### Primary Goals
1. **Digitize Consultation Records**: Replace paper-based consultations with structured digital records
2. **Optimize Clinic Workflow**: Reduce consultation time and administrative burden
3. **Ensure Data Accessibility**: Enable quick patient history retrieval
4. **Generate Professional Output**: Create printable, compliant prescriptions
5. **Data Portability**: Support data export for reporting and backup

### Secondary Goals
1. Establish foundation for future multi-user system
2. Create audit trail for medical compliance
3. Enable basic analytics on clinic operations
4. Improve data accuracy through structured entry
5. Support patient communication (future phases)

### Success Criteria (Measurable)
1. ✅ Consultation completion: 2-3 minutes per patient
2. ✅ Patient search: <5 seconds retrieval with full history
3. ✅ Paper reduction: 80% decrease in paper usage
4. ✅ Prescription quality: Smooth generation and printing
5. ✅ Data export: CSV/PDF formats working reliably
6. ✅ Usability: <1 hour training for new users
7. ✅ System uptime: 99% availability
8. ✅ Data integrity: Zero data loss incidents

### Business Value
- **Cost Reduction**: Elimination of paper, filing, storage costs
- **Efficiency Gain**: 30-40% reduction in administrative time
- **Professional Perception**: Modern clinic image
- **Revenue Protection**: Prevented lost records = retained patients
- **Scalability Path**: Foundation for multi-doctor clinics

---

## Functional Requirements Analysis

### Core Modules (8 Primary)

#### 1. Authentication & Access Control
- **Purpose**: Secure single-user access
- **Features**:
  - Login with username/password
  - Session management
  - Password reset capability
  - Account security basics
- **Acceptance Criteria**:
  - User can login securely
  - Sessions timeout after 30 minutes of inactivity
  - Password validation enforced (min 8 chars, complexity)
  - Failed login attempts limited to 5 per 15 minutes

#### 2. Patient Management
- **Purpose**: Maintain comprehensive patient database
- **Features**:
  - Add new patient
  - Edit patient details
  - View patient profile
  - Search by name/phone
  - View patient list
  - Mark patient as inactive (soft delete)
- **Acceptance Criteria**:
  - All required fields validated (name, age, gender, contact)
  - Duplicate detection (same name + DOB)
  - Search returns results in <2 seconds
  - Phone number formatted consistently
  - Age auto-calculated from DOB

#### 3. Appointment Scheduling
- **Purpose**: Manage clinic appointments and availability
- **Features**:
  - Create appointment
  - View daily/weekly calendar
  - Update appointment status (Scheduled → Completed → Cancelled/No-show)
  - Reschedule appointment
  - View appointment history
- **Acceptance Criteria**:
  - Calendar view shows time slots (15/30-min intervals)
  - Status transitions enforced (no direct No-show without Scheduled)
  - Overbooking prevention (one patient per time slot)
  - Past appointments cannot be modified
  - Show upcoming appointments on dashboard

#### 4. Consultation Recording
- **Purpose**: Capture complete consultation details
- **Sub-Features**:
  
  **A. Vitals Capture** (Mandatory)
  - Temperature (Celsius/Fahrenheit)
  - Blood Pressure (Systolic/Diastolic)
  - Pulse (BPM)
  - Acceptance Criteria: All vitals required, validated ranges, timestamp recorded
  
  **B. Complaints** (Symptoms)
  - Free-text symptom entry
  - Acceptance Criteria: Text input, character limit (2000), auto-saved
  
  **C. Diagnosis**
  - Diagnosis notes (free text)
  - Diagnosis code (optional reference)
  - Acceptance Criteria: Text input, searchable, linked to consultation
  
  **D. Medication/Prescription**
  - Medicine name
  - Dosage (e.g., "500mg")
  - Frequency (e.g., "Twice daily")
  - Duration (e.g., "7 days")
  - Special instructions (e.g., "After food")
  - Add multiple medicines per consultation
  - Acceptance Criteria: All fields required, validates against common meds (optional), supports multiple entries

#### 5. Prescription Generation
- **Purpose**: Create printable, professional prescriptions
- **Features**:
  - Auto-populate prescription with:
    - Clinic/doctor header
    - Patient details (name, age, contact)
    - Consultation date
    - Vitals summary
    - Diagnosis
    - Medication list
    - Doctor signature area
    - Footer with clinic details
  - Print to PDF
  - Print to physical printer
- **Acceptance Criteria**:
  - All required elements included
  - Professional formatting
  - Print quality acceptable for patient handoff
  - Prescription can be printed multiple times

#### 6. Patient History & Visit Records
- **Purpose**: Enable access to complete patient medical history
- **Features**:
  - View all patient visits (chronological)
  - Access visit details: vitals, complaints, diagnosis, medications
  - Filter visits by date range
  - Search within patient history (complaints, diagnosis, meds)
  - Download visit record as PDF
- **Acceptance Criteria**:
  - Complete visit history visible
  - Date filtering accurate
  - All visit data accessible
  - Export maintains formatting

#### 7. Search & Navigation
- **Purpose**: Enable quick patient access
- **Features**:
  - Search by name (partial matching)
  - Search by phone number
  - Recent patients quick access
  - Quick patient list
  - Dashboard showing recent/upcoming appointments
- **Acceptance Criteria**:
  - Search results in <2 seconds
  - Case-insensitive name search
  - Phone search handles formatting variations
  - Recent patients list (last 20) readily accessible

#### 8. Data Export
- **Purpose**: Enable data portability and backup
- **Features**:
  - Export patient list (CSV)
  - Export visit data (CSV/PDF)
  - Export prescription (PDF)
  - Date range filtering for exports
- **Acceptance Criteria**:
  - CSV format standard compliant
  - PDF maintains formatting
  - All relevant data included
  - Export completes in <10 seconds

### Feature Breakdown & User Stories

**User Story 1: Patient Registration**
- As a doctor, I want to quickly add new patients to the system
- Acceptance: Form completion <1 minute, validation prevents invalid entries

**User Story 2: Appointment Scheduling**
- As a doctor, I want to view my daily schedule and manage appointments
- Acceptance: Calendar loads in <2 seconds, status changes immediate

**User Story 3: Consultation Recording**
- As a doctor, I want to record complete consultation details (vitals, complaints, diagnosis, meds)
- Acceptance: All fields accessible, data saved, no loss on disconnect

**User Story 4: Prescription Generation**
- As a doctor, I want to generate professional prescriptions I can print
- Acceptance: Prescription formats correctly, prints without issues

**User Story 5: Patient History Access**
- As a doctor, I want to view a patient's complete visit history
- Acceptance: History loads in <3 seconds, all visits visible, sortable by date

**User Story 6: Quick Patient Search**
- As a doctor, I want to find patients quickly by name or phone
- Acceptance: Search results in <2 seconds, case-insensitive, partial matching

**User Story 7: Data Export**
- As a doctor, I want to export patient/visit data for backup or analysis
- Acceptance: CSV/PDF generated correctly, can be opened in standard tools

### Module Dependencies

```
Authentication
    ↓
Patient Management ← ← Appointment Scheduling
    ↓                       ↓
Consultation Recording      ↓
    ├─→ Vitals              ↓
    ├─→ Complaints          ↓
    ├─→ Diagnosis           ↓
    └─→ Medication/Prescription
                 ↓
         Prescription Generation
                 ↓
         Patient History & Visits
                 ↓
         Search & Navigation
                 ↓
         Data Export
```

---

## Non-Functional Requirements Analysis

### Performance Requirements
- **Page Load Time**: <2 seconds (goal: <1 second)
- **Search Response**: <2 seconds for patient search
- **History Load**: <3 seconds for complete visit history
- **Export Generation**: <10 seconds for data export
- **Prescription Print**: <5 seconds print dialog
- **Database Query**: <500ms average query time
- **Concurrent Users**: 1 (but design for 5 simultaneous sessions)

**Targets**: 
- 95th percentile latency <2 seconds
- 99th percentile latency <5 seconds

### Scalability Requirements
- **Patient Volume**: 5,000-10,000 patients (single clinic)
- **Visit History**: 50,000-100,000 visits initially
- **Appointment Slots**: 30-40 per day
- **Growth Rate**: 10% per year initially
- **Peak Usage**: 1-3 concurrent users
- **Design Capacity**: Support 10x current volume without major refactoring

### Availability & Reliability
- **Uptime Target**: 99% (allowing 7.2 hours downtime per month)
- **Backup Frequency**: Daily automated backups
- **Backup Retention**: 30 days
- **Recovery Time Objective (RTO)**: <4 hours
- **Recovery Point Objective (RPO)**: <1 hour
- **Data Loss Risk**: Zero tolerance
- **Graceful Degradation**: Read-only mode if DB temporarily unavailable

### Security Requirements
- **Authentication**: Single user, username/password login with JWT tokens
- **Session Management**: 30-minute inactivity timeout
- **Data Encryption**: HTTPS in transit, encrypted at rest (SQL Server TDE)
- **Password Policy**: Minimum 8 characters, complexity required
- **Audit Logging**: All data modifications logged with timestamp/user
- **Access Control**: All patient data accessible to authenticated user only
- **HIPAA Consideration**: Design for HIPAA compliance (though Phase 1 may not be fully HIPAA certified)

### Usability Requirements
- **Training Time**: Minimal (<1 hour for doctor)
- **UI Complexity**: Minimalist, focused on fast data entry
- **Navigation**: Intuitive, <3 clicks to any function
- **Form Entry**: Fast form layouts, auto-focus, smart defaults
- **Error Messages**: Clear, actionable error messages
- **Mobile Responsiveness**: Responsive design (tablet support priority)
- **Accessibility**: WCAG 2.1 AA compliance (minimum)

### Maintainability Requirements
- **Code Quality**: SOLID principles, DRY, testable code
- **Documentation**: API documentation, code comments for complex logic
- **Test Coverage**: 70%+ unit test coverage, critical path 90%+
- **Technical Debt**: Documented and tracked
- **Logging**: Comprehensive logging at appropriate levels
- **Monitoring**: Application health monitoring, error tracking

### Auditability & Compliance
- **Audit Trail**: All create/update/delete operations logged
- **Data Retention**: Comply with local healthcare data retention laws
- **Access Logs**: Track who accessed what data and when
- **Compliance Ready**: Design for future HIPAA/local compliance

### Monitoring & Observability
- **Application Logs**: Error, warning, info, debug levels
- **Performance Metrics**: Response times, query execution times
- **Health Checks**: System health endpoint
- **Error Tracking**: Centralized error logging and alerting
- **User Activity**: Login/logout tracking

---

## User Roles and Permissions

### Identified Roles

#### 1. Doctor (Primary User)
**Responsibilities**:
- Complete patient consultations
- Manage patient records
- Schedule and manage appointments
- Generate prescriptions
- Review patient history
- Export data for backup

**Permissions**:
- ✅ Create/edit/delete patients
- ✅ Create/view/update appointments
- ✅ Create/view/update consultations
- ✅ Add vitals, complaints, diagnosis, medications
- ✅ Generate prescriptions
- ✅ View patient history
- ✅ Search patients
- ✅ Export data
- ✅ Change own password
- ✅ View system logs (limited)

**Access Boundaries**:
- Cannot access other doctor accounts (single user system)
- Cannot access system settings/admin functions
- Cannot delete historical data (only mark as archived)
- Cannot access audit logs directly (logged in background)

> **Scope note**: The BRD says Phase 1 has a **single user (the doctor)** and lists receptionist and multi-user access as out of scope. Only the Doctor role is built in Phase 1. The roles below are documented so the RBAC model and schema don't need a redesign in Phase 2. They must not be built in Phase 1 without a scope change.

#### 2. Admin (Phase 2 — not in BRD scope)
**Responsibilities**: Manage user accounts, configure clinic profile and prescription template, oversee backups, review audit logs
**Permissions**: Manage users and roles, edit clinic settings, read audit logs, trigger exports
**Access Boundaries**: No clinical data entry (vitals, diagnosis, prescriptions). Read access to clinical records only if the clinic's policy allows it. In Phase 1 the doctor does these tasks.

#### 3. Nurse (Phase 2 — not mentioned in BRD)
**Responsibilities**: Record vitals before the consultation, update patient contact details
**Permissions**: Read patient profile and today's appointments, create/edit vitals for an open visit
**Access Boundaries**: Cannot record diagnosis or medications, generate prescriptions, or export data. Cannot edit a completed visit.

#### 4. Receptionist (Phase 2 — explicitly out of scope in BRD)
**Responsibilities**: Register patients, schedule/reschedule/cancel appointments, mark no-shows
**Permissions**: Create/edit patient demographics, full appointment management, patient search
**Access Boundaries**: No access to complaints, diagnosis, medications, prescriptions, or visit history (minimum-necessary access to clinical data)

#### 5. Patient (Phase 3+ — not in BRD)
**Responsibilities**: View own appointments and prescriptions (patient portal)
**Permissions**: Read-only access to their own records
**Access Boundaries**: Strictly their own data. Requires separate identity verification and consent handling. A significant scope item that needs its own BRD.

#### 6. Additional Roles Discovered
- **System / Service Account**: runs scheduled backups and maintenance jobs; no interactive login; least-privilege database rights
- **Auditor / Compliance Reviewer (optional)**: read-only access to audit logs for regulatory review

### Role-Permission Summary (Target Model)

| Capability | Doctor | Admin | Nurse | Receptionist | Patient |
|------------|--------|-------|-------|--------------|---------|
| Patient demographics | CRUD | Read | Edit contact | CRUD | Own (read) |
| Appointments | CRUD | Read | Read | CRUD | Own (read) |
| Vitals | CRUD | — | Create/Edit (open visit) | — | — |
| Complaints / Diagnosis / Medications | CRUD | — | — | — | — |
| Prescription generate/print | ✅ | — | — | — | Own (read) |
| Visit history | ✅ | Policy-based | Limited | — | Own (read) |
| Export (CSV/PDF) | ✅ | ✅ | — | — | — |
| User & clinic settings | Own password, clinic profile | ✅ | Own password | Own password | Own password |
| Audit log | Read | Read | — | — | — |

**Phase 1 Implementation**: Model roles and permissions as data (a role and permission claims in the JWT), with only the Doctor role seeded. Authorize endpoints by **policy** (e.g., "CanRecordDiagnosis"), not by hard-coded role names, so Phase 2 only needs data changes.

---

## End-to-End User Workflows

### Workflow 1: Patient Registration
**Actor**: Doctor  
**Trigger**: New patient arrives at clinic

```
1. Doctor clicks "Add Patient"
2. System shows registration form
3. Doctor enters patient details:
   - Name
   - DOB (age auto-calculated)
   - Gender
   - Phone
   - Address (optional)
4. System validates data
5. Doctor clicks "Save"
6. System saves patient, shows confirmation
7. Patient appears in system, can schedule appointments
```

**Performance Goal**: <1 minute  
**Error Cases**: Duplicate detection, invalid phone format  
**Dependencies**: None

---

### Workflow 2: Appointment Scheduling
**Actor**: Doctor  
**Trigger**: Patient arrives or calls for appointment

```
1. Doctor opens appointment calendar
2. Doctor views daily/weekly schedule
3. Doctor clicks on available time slot
4. System shows appointment creation form
5. Doctor selects patient (search/recent)
6. Doctor enters appointment notes (optional)
7. Doctor clicks "Schedule"
8. System saves appointment
9. Confirmation shows appointment time
```

**Performance Goal**: <2 minutes  
**Error Cases**: Overbooking prevention  
**Dependencies**: Patient must exist in system

---

### Workflow 3: Consultation Recording
**Actor**: Doctor  
**Trigger**: Appointment time, patient present

```
STEP 1: Start Consultation
1. Doctor opens appointment from calendar
2. System shows appointment details
3. Doctor clicks "Start Consultation"
4. System creates new visit record

STEP 2: Record Vitals (Mandatory)
5. Doctor records temperature
6. Doctor records blood pressure (systolic/diastolic)
7. Doctor records pulse
8. System validates vitals are within safe ranges

STEP 3: Record Complaints
9. Doctor enters patient symptoms (free text)
10. System saves complaint notes

STEP 4: Record Diagnosis
11. Doctor enters diagnosis notes
12. System saves diagnosis

STEP 5: Add Medications
13. Doctor clicks "Add Medication"
14. Doctor enters:
    - Medicine name
    - Dosage
    - Frequency
    - Duration
    - Special instructions
15. System saves medication entry
16. Doctor can add more medications (repeat step 13-15)

STEP 6: Complete Consultation
17. Doctor clicks "Complete Consultation"
18. System marks appointment as completed
19. System saves all consultation data
```

**Performance Goal**: 2-3 minutes total  
**Error Cases**: Missing vitals validation, unsaved data protection  
**Dependencies**: Appointment must exist, patient must be selected

---

### Workflow 4: Prescription Generation
**Actor**: Doctor  
**Trigger**: After consultation recording complete

```
1. Doctor views completed consultation
2. Doctor clicks "Generate Prescription"
3. System pre-fills prescription with:
   - Clinic/doctor header
   - Patient details
   - Consultation date
   - Vitals summary
   - Diagnosis
   - Medication list (from consultation)
   - Signature area
4. Doctor reviews prescription
5. Doctor can edit if needed (optional)
6. Doctor clicks "Print"
7. System opens print dialog with PDF preview
8. Doctor sends to printer
9. Prescription prints successfully
```

**Performance Goal**: <30 seconds to print dialog  
**Error Cases**: Formatting issues, missing medications  
**Dependencies**: Completed consultation with vitals + medications

---

### Workflow 5: Patient History Access
**Actor**: Doctor  
**Trigger**: Need to review patient's medical history

```
1. Doctor searches for patient (name/phone)
2. System shows search results
3. Doctor clicks on patient name
4. System shows patient profile with visit history
5. Doctor can filter history by date range
6. Doctor clicks on specific visit
7. System shows visit details:
   - Date
   - Vitals
   - Complaints
   - Diagnosis
   - Medications prescribed
8. Doctor can download visit as PDF
9. Doctor can print prescription again if needed
```

**Performance Goal**: <3 seconds total  
**Error Cases**: Patient not found, corrupted visit data  
**Dependencies**: Patient must have existing visits

---

### Workflow 6: Quick Patient Search
**Actor**: Doctor  
**Trigger**: During patient lookup

```
1. Doctor enters patient name (partial)
2. System shows matching results in real-time
3. Doctor clicks on patient
4. System shows patient profile/appointments
OR
1. Doctor enters phone number
2. System searches by phone
3. System shows matching patient(s)
4. Doctor clicks to open patient profile
```

**Performance Goal**: <2 seconds search  
**Error Cases**: No matches, multiple matches  
**Dependencies**: Patients must exist in system

---

### Workflow 7: Data Export
**Actor**: Doctor  
**Trigger**: Backup or analysis requirement

```
PATIENT LIST EXPORT:
1. Doctor opens "Tools" → "Export"
2. Doctor selects "Patient List"
3. Doctor clicks "Export as CSV"
4. System generates CSV file
5. Browser downloads file
6. Doctor can open in Excel

VISIT HISTORY EXPORT:
1. Doctor opens "Tools" → "Export"
2. Doctor selects "Visit History"
3. Doctor selects date range
4. Doctor chooses format (CSV or PDF)
5. System generates file
6. Browser downloads file
```

**Performance Goal**: <10 seconds  
**Error Cases**: Large data sets, format issues  
**Dependencies**: Data must exist in system

---

### Cross-Workflow Dependencies

```
Patient Registration
        ↓
Appointment Scheduling → Consultation Recording
        ↑                     ↓
        └← Patient Search ←  Prescription Generation
                              ↓
                        Patient History Access
                              ↓
                          Data Export
```

---

## Domain and Entity Analysis

### Business Entities Identified

#### 1. Patient
**Purpose**: Core entity representing a person receiving medical care  
**Key Attributes**:
- PatientId (PK)
- FirstName, LastName
- DateOfBirth (Age auto-calculated)
- Gender
- PhoneNumber
- Address, City, State, PostalCode
- Email (optional)
- IsActive (soft delete)
- CreatedDate, ModifiedDate
- CreatedBy, ModifiedBy (audit)

**Relationships**:
- 1 Patient → N Appointments
- 1 Patient → N Visits
- 1 Patient → N Prescriptions

**Ownership**: Doctor (single owner)  
**Business Rules**:
- Phone number must be unique or allow duplicates per clinic policy
- DOB is mandatory, age auto-calculated
- Cannot delete patient (soft delete only)
- Patient cannot have overlapping appointments

---

#### 2. Appointment
**Purpose**: Scheduled time slot for patient consultation  
**Key Attributes**:
- AppointmentId (PK)
- PatientId (FK)
- AppointmentDate
- AppointmentTime (or single DateTime)
- Status (Scheduled, Completed, Cancelled, NoShow)
- Notes (optional)
- CreatedDate, ModifiedDate

**Relationships**:
- N Appointments → 1 Patient
- 0-1 Appointment → 1 Visit (appointment leads to visit)

**Ownership**: Doctor (via Patient)  
**Business Rules**:
- One patient per time slot (no double-booking)
- Cannot modify past appointments
- Status transitions: Scheduled → (Completed | Cancelled | NoShow)
- Completed appointment should have corresponding Visit record

---

#### 3. Visit / Consultation Record
**Purpose**: Complete medical record of a consultation  
**Key Attributes**:
- VisitId (PK)
- PatientId (FK)
- AppointmentId (FK, optional)
- VisitDate
- CreatedDate, ModifiedDate

**Relationships**:
- N Visits → 1 Patient
- 1 Visit → 1 Appointment (optional, may exist without appointment)
- 1 Visit → 1 Vitals record
- 1 Visit → N Complaints (or single text field)
- 1 Visit → N Diagnoses (or single text field)
- 1 Visit → N Medications
- 1 Visit → 1 Prescription

**Ownership**: Patient (via Visit)  
**Business Rules**:
- Vitals mandatory for any visit
- Cannot modify completed visit (soft delete, archive only)
- Each visit is immutable historical record

---

#### 4. Vitals
**Purpose**: Record vital signs for a consultation  
**Key Attributes**:
- VitalsId (PK)
- VisitId (FK)
- Temperature (Celsius, with decimals)
- BloodPressureSystolic (mmHg)
- BloodPressureDiastolic (mmHg)
- Pulse (BPM)
- RecordedDate

**Relationships**:
- 1 Vitals → 1 Visit (one vitals record per visit)

**Ownership**: Visit  
**Business Rules**:
- All fields mandatory
- Vitals required to complete consultation
- Temperature range: 35-42 Celsius (alert if outside)
- BP range validation
- Pulse range: 40-200 BPM

---

#### 5. Complaint
**Purpose**: Record patient-reported symptoms  
**Key Attributes**:
- ComplaintId (PK)
- VisitId (FK)
- ComplaintText (free text, up to 2000 chars)
- RecordedDate

**Relationships**:
- N Complaints → 1 Visit (typically 1-5 per visit)

**Ownership**: Visit  
**Business Rules**:
- Text field, free-form entry
- Can be empty (optional, though typically provided)
- Immutable (no edit after visit complete)

---

#### 6. Diagnosis
**Purpose**: Record doctor's diagnosis/assessment  
**Key Attributes**:
- DiagnosisId (PK)
- VisitId (FK)
- DiagnosisText (free text)
- DiagnosisCode (optional, for future ICD-10 integration)
- RecordedDate

**Relationships**:
- N Diagnoses → 1 Visit (typically 1-3 per visit)

**Ownership**: Visit  
**Business Rules**:
- Text field, free-form entry
- Optional (can have visit without diagnosis if needed)
- Can reference diagnosis code (future integration)

---

#### 7. Medication
**Purpose**: Record prescribed medication details  
**Key Attributes**:
- MedicationId (PK)
- VisitId (FK)
- MedicineName
- Dosage (e.g., "500mg")
- Frequency (e.g., "Twice daily")
- Duration (e.g., "7 days")
- SpecialInstructions (optional, e.g., "After food")
- RecordedDate

**Relationships**:
- N Medications → 1 Visit (1-10+ per visit typical)

**Ownership**: Visit  
**Business Rules**:
- All fields except special instructions mandatory
- Multiple medications per visit
- Linked to Prescription (included in prescription)

---

#### 8. Prescription
**Purpose**: Printable prescription document  
**Key Attributes**:
- PrescriptionId (PK)
- VisitId (FK)
- PatientId (FK)
- PrescriptionDate
- GeneratedDate
- PrintCount (number of times printed)
- CreatedDate

**Relationships**:
- 1 Prescription → 1 Visit
- N Prescriptions → 1 Patient (historical tracking)

**Ownership**: Visit  
**Business Rules**:
- Auto-generated from Visit data
- Immutable (recreated if needed, not edited)
- Can be printed multiple times
- Contains patient + visit + vitals + diagnosis + medications

---

#### 9. User / Doctor Account
**Purpose**: Authentication and user management  
**Key Attributes**:
- UserId (PK)
- Username (unique)
- Email
- PasswordHash
- PasswordSalt
- ClinicName
- IsActive
- LastLoginDate
- CreatedDate, ModifiedDate

**Relationships**:
- 1 User → N Audit Logs
- 1 User → N Patients (creator)
- 1 User → N Visits (creator)

**Ownership**: System  
**Business Rules**:
- Phase 1: Single user per installation
- Password minimum 8 characters, complexity required
- Cannot delete user (soft delete)
- Session timeout 30 minutes

---

#### 10. Audit Log
**Purpose**: Track all data modifications for compliance  
**Key Attributes**:
- AuditLogId (PK)
- TableName
- RecordId
- UserId (FK)
- ActionType (Create, Update, Delete, Archive)
- OldValue (JSON)
- NewValue (JSON)
- Timestamp
- IpAddress

**Relationships**:
- N Audit Logs → 1 User

**Ownership**: System (read-only)  
**Business Rules**:
- Auto-logged on all data changes
- Immutable (cannot delete/modify)
- Retention: 7 years (per healthcare standards)

---

### Entity Relationship Summary

```
┌─────────────┐
│    User     │
│  (Doctor)   │
└──────┬──────┘
       │ creates/owns
       │
       ├─────────────────┐
       │                 │
   ┌───▼──────┐    ┌─────▼──────┐
   │ Patient  │◄───│ Appointment│
   └───┬──────┘    └─────┬──────┘
       │                 │
       ├─────────┬───────┤
       │         │       │
   ┌───▼──────┬──┴┐     │
   │   Visit  ◄──┘◄─────┘
   └───┬──────┘
       │
       ├─────────┬──────────┬──────────┬────────────┐
       │         │          │          │            │
   ┌───▼──┐ ┌───▼───┐ ┌────▼────┐ ┌─▼──────┐ ┌───▼────┐
   │Vitals│ │Complaint│ │Diagnosis│ │Medication│ │ Prescription│
   └──────┘ └───────┘ └─────────┘ └────────┘ └────────┘
       
┌────────────┐
│ AuditLog   │ (logs all changes)
└────────────┘
```

### Data Integrity Constraints

- Patient must exist before appointment
- Appointment must exist (or visit standalone) before visit
- Visit must have vitals (mandatory)
- Vitals within valid ranges enforced
- Appointment cannot be double-booked
- Historical data immutable (no updates post-completion)
- Soft deletes used (IsActive flag, data never destroyed)

---

## Angular 19 Frontend Recommendations

### Feature-Based Folder Structure

> **Angular 19 note**: Components are standalone by default. Read the `*.module.ts` entries below as **feature route files** (`*.routes.ts`), and `app.module.ts` as `app.config.ts` (application providers). No NgModules are required.

```
src/
├── app/
│   ├── core/                          # Singleton services
│   │   ├── auth/
│   │   │   ├── auth.service.ts
│   │   │   ├── auth.guard.ts
│   │   │   ├── auth.interceptor.ts
│   │   │   └── jwt.interceptor.ts
│   │   ├── http/
│   │   │   ├── api.service.ts
│   │   │   └── error.interceptor.ts
│   │   ├── models/
│   │   │   ├── patient.model.ts
│   │   │   ├── appointment.model.ts
│   │   │   ├── visit.model.ts
│   │   │   └── ...
│   │   └── core.module.ts
│   │
│   ├── shared/                        # Reusable components & services
│   │   ├── components/
│   │   │   ├── nav-bar/
│   │   │   ├── side-menu/
│   │   │   ├── confirm-dialog/
│   │   │   ├── loading-spinner/
│   │   │   └── error-alert/
│   │   ├── pipes/
│   │   │   ├── age.pipe.ts
│   │   │   ├── phone-format.pipe.ts
│   │   │   └── date-format.pipe.ts
│   │   ├── directives/
│   │   │   ├── required-field.directive.ts
│   │   │   └── focus.directive.ts
│   │   ├── services/
│   │   │   ├── notification.service.ts
│   │   │   ├── dialog.service.ts
│   │   │   └── loading.service.ts
│   │   └── shared.module.ts
│   │
│   ├── features/
│   │   ├── dashboard/
│   │   │   ├── pages/
│   │   │   │   └── dashboard.component.ts
│   │   │   ├── services/
│   │   │   │   └── dashboard.service.ts
│   │   │   └── dashboard.module.ts
│   │   │
│   │   ├── patient/
│   │   │   ├── pages/
│   │   │   │   ├── patient-list/
│   │   │   │   ├── patient-detail/
│   │   │   │   └── patient-form/
│   │   │   ├── components/
│   │   │   │   ├── patient-card/
│   │   │   │   ├── patient-search/
│   │   │   │   └── patient-history/
│   │   │   ├── services/
│   │   │   │   └── patient.service.ts
│   │   │   └── patient.module.ts
│   │   │
│   │   ├── appointment/
│   │   │   ├── pages/
│   │   │   │   ├── calendar/
│   │   │   │   └── appointment-form/
│   │   │   ├── components/
│   │   │   │   ├── calendar-view/
│   │   │   │   └── appointment-card/
│   │   │   ├── services/
│   │   │   │   └── appointment.service.ts
│   │   │   └── appointment.module.ts
│   │   │
│   │   ├── consultation/
│   │   │   ├── pages/
│   │   │   │   └── consultation-form/
│   │   │   ├── components/
│   │   │   │   ├── vitals-form/
│   │   │   │   ├── complaint-form/
│   │   │   │   ├── diagnosis-form/
│   │   │   │   ├── medication-form/
│   │   │   │   └── consultation-review/
│   │   │   ├── services/
│   │   │   │   └── consultation.service.ts
│   │   │   └── consultation.module.ts
│   │   │
│   │   ├── prescription/
│   │   │   ├── pages/
│   │   │   │   └── prescription-view/
│   │   │   ├── components/
│   │   │   │   └── prescription-template/
│   │   │   ├── services/
│   │   │   │   └── prescription.service.ts
│   │   │   └── prescription.module.ts
│   │   │
│   │   ├── export/
│   │   │   ├── services/
│   │   │   │   ├── csv-export.service.ts
│   │   │   │   └── pdf-export.service.ts
│   │   │   └── export.module.ts
│   │   │
│   │   └── settings/
│   │       ├── pages/
│   │       │   └── settings.component.ts
│   │       ├── components/
│   │       │   ├── change-password/
│   │       │   └── clinic-info/
│   │       └── settings.module.ts
│   │
│   ├── app.component.ts
│   ├── app.routing.ts
│   └── app.module.ts
│
└── assets/
    ├── images/
    ├── styles/
    │   ├── variables.scss
    │   ├── globals.scss
    │   └── themes/
    └── icons/
```

### Shared Folder Strategy (Standalone)

**`shared/` Contains**:
- Standalone presentational components (page header, confirm dialog, loading indicator, empty state, error alert)
- Standalone pipes (age from DOB, phone format, date format)
- Standalone directives (autofocus, required-field marker)
- No services with state. Stateful services belong in `core/` or a feature.

**Usage**: Each component imports exactly the shared pieces and Material components it uses. There is no shared "barrel module", so unused code is tree-shaken.

---

### Core Folder Strategy (Standalone)

**`core/` Contains**:
- Auth service, token storage, current-user state (signals)
- Functional route guards and functional HTTP interceptors
- API base URL / environment configuration
- Global error handler, notification service
- Shared models/interfaces for API contracts

**Usage**: Register app-wide providers once in `app.config.ts` (`provideRouter`, `provideHttpClient(withInterceptors(...))`, `provideAnimationsAsync`). Services use `providedIn: 'root'`, so they are singletons without `forRoot()`.

---

### Lazy Loading Approach

**Lazy-Loaded Feature Routes**:
- Dashboard (default route after login)
- Patients (list, detail, form, history)
- Appointments (daily list/calendar, form)
- Consultation (visit capture)
- Prescription (preview/print)
- Export
- Settings (clinic profile, change password)

**Routing Approach**:
- The root route table lazy-loads each feature's route file with `loadChildren`
- Individual heavy pages lazy-load with `loadComponent`
- An authenticated layout shell route wraps all features and applies the auth guard once
- Use `@defer` blocks for non-critical widgets (e.g., the dashboard's recent-patients panel)

**Benefits**:
- Faster initial load (only auth + core)
- Smaller bundle size
- Better performance on slow networks
- Scales well as app grows

---

### Angular Material Usage

**Components to Use**:
- **mat-toolbar**: Top navigation bar
- **mat-sidenav**: Side menu (if needed)
- **mat-card**: Content containers
- **mat-form-field & mat-input**: Form inputs
- **mat-select**: Dropdowns
- **mat-datepicker**: Date picking (appointment dates)
- **mat-table**: Data tables (patient list, visit history)
- **mat-button**: Action buttons
- **mat-dialog**: Confirmation dialogs
- **mat-snack-bar**: Toast notifications
- **mat-progress-bar**: Loading indicators
- **mat-icon**: Icons throughout UI

**Material Theme**: Create custom theme (blue/green for healthcare feel)

**Customization**: Extend Material with custom SCSS variables for clinic branding

---

### State Management Approach

**Recommendation**: Signals for UI/local state, RxJS for HTTP and async streams, plain injectable services as feature stores. No NgRx for Phase 1.

**Phase 1 Pattern**:
- HTTP calls return Observables (RxJS)
- Feature stores (e.g., patient store, consultation store) hold state in signals and expose read-only/computed signals
- Use `toSignal` / `rxResource` to bridge HTTP results into signals where it simplifies templates
- Search box uses RxJS `debounceTime` + `distinctUntilChanged` + `switchMap`
- Use the `async` pipe or `takeUntilDestroyed` for subscriptions; never leave manual subscriptions open

**When to Reconsider**: Add NgRx SignalStore only if Phase 2 brings multiple roles editing shared data concurrently.

**Benefits for Phase 1**:
- Simpler than NgRx
- Sufficient for single-user app
- Follows Angular best practices
- Easy to refactor to NgRx later if needed

---

### Route Guards

**AuthGuard** (CanActivate):
- Check if user logged in
- Redirect to login if not
- Protect all feature routes

**RoleGuard** (CanActivate, Phase 2):
- Check user role/permissions
- Redirect if unauthorized
- Prepare for multi-user system

**UnsavedChangesGuard** (CanDeactivate):
- Warn user before leaving form with unsaved changes
- Prevent accidental data loss

**PrefetchGuard** (CanActivate):
- Prefetch necessary data before component loads
- Ensures data available when component renders

---

### HTTP Interceptors

**JWT Interceptor**:
- Automatically add Authorization header to all requests
- Include JWT token from local storage
- Handle token refresh on 401 response

**Error Interceptor**:
- Catch HTTP errors
- Convert to user-friendly messages
- Log errors to console (debug)
- Handle 401 → redirect to login
- Handle 500 → show error message
- Handle network timeout → show retry option

**Loading Interceptor** (Optional):
- Track pending requests
- Show/hide loading spinner
- Prevent multiple identical requests

---

### Form Validation Strategy

**Reactive Forms** (Primary):
- Use FormBuilder for complex forms
- Validators: required, minLength, maxLength, pattern, custom validators
- Real-time validation feedback
- Dynamic form controls

**Custom Validators**:
- Phone number format validator
- Age validator (not negative, not >120)
- Date range validator (appointment not in past)
- Duplicate patient validator (name + DOB combination)

**Validation Display**:
- Show error message below form field
- Disable submit button until valid
- Highlight invalid fields
- Real-time feedback as user types

**Examples**:
- Patient form: name (required), DOB (required + valid), phone (pattern)
- Appointment form: date (required + not past), time (required)
- Consultation form: all vitals required, ranges validated
- Medication form: name, dosage, frequency required

---

### Error Handling Strategy

**API Errors**:
- Catch in error interceptor
- Display user-friendly message in snack bar/toast
- Log technical details to console (dev only)
- Show "Try Again" button for retryable errors

**Form Validation Errors**:
- Display inline next to field
- Show specific error (required, minLength, etc)
- Disable submit until resolved

**Business Logic Errors**:
- Duplicate patient detection → show error message with option to search existing
- Appointment conflict → show error, ask to select different time
- Missing vitals → prevent consultation completion

**Error Logging** (Phase 2):
- Send errors to server logging service
- Track error patterns
- Alert on critical errors

---

### Components Architecture

**Smart vs Presentational**:

**Smart Components** (Containers):
- Connected to services
- Handle business logic
- Pass data to presentational components
- Examples: PatientListComponent, ConsultationFormComponent

**Presentational Components** (Dumb):
- Receive data via @Input
- Emit events via @Output
- Reusable across different contexts
- Examples: PatientCardComponent, VitalsFormComponent

**Benefits**:
- Reusable presentational components
- Easier to test
- Clear separation of concerns
- Flexible composition

---

### Components & Pages Identified

**Dashboard Page**:
- Welcome message
- Quick stats (today's appointments, recent patients)
- Quick action buttons (new patient, new appointment, search)
- Upcoming appointments list

**Patient Pages**:
- Patient List (searchable table)
- Patient Detail (profile + history)
- Patient Form (create/edit)
- Patient Search (quick search dialog)

**Appointment Pages**:
- Calendar View (daily/weekly)
- Appointment Form (create/reschedule)

**Consultation Page**:
- Consultation Form (vitals + complaints + diagnosis + meds)
- Multi-step form or single long form

**Prescription Page**:
- Prescription View (formatted prescription)
- Print dialog

**Dialogs**:
- Confirm Delete
- Select Patient
- Error Message
- Loading Indicator

**Components**:
- Patient Card (small patient summary)
- Vitals Form (temperature, BP, pulse inputs)
- Medication Form (add/edit medications)
- Complaint/Diagnosis Forms (text areas)
- Calendar (appointment calendar view)

---

### Services Required

**Patient Service**:
- CRUD operations for patients
- Search by name/phone
- Get patient with full history

**Appointment Service**:
- CRUD for appointments
- Get calendar for date
- Update appointment status

**Consultation Service**:
- Create/update consultation
- Save vitals, complaints, diagnosis, medications
- Retrieve consultation details

**Prescription Service**:
- Generate prescription from visit
- Export to PDF
- Print prescription

**Export Service**:
- Export patient list to CSV
- Export visit data to CSV/PDF
- Handle large data sets

**Notification Service**:
- Show success messages
- Show error messages
- Show confirmation dialogs

**Auth Service** (in Core):
- Login
- Logout
- Token management
- Session handling

---

## ASP.NET Core Web API Recommendations

### Clean Architecture Layers

```
Presentation Layer (Controllers, DTOs)
         ↓
Application Layer (Services, Use Cases, Validators)
         ↓
Domain Layer (Entities, Business Rules)
         ↓
Infrastructure Layer (Data Access, External Services)
```

**Layer Responsibilities**:

**Presentation (API Layer)**:
- HTTP Controllers
- Request/Response DTOs
- Routing
- Input validation (model state)
- Authentication/Authorization attributes
- Example: `PatientController`, `AppointmentController`

**Application (Business Logic)**:
- Service classes
- Use case implementations
- Application-level validation (FluentValidation)
- Mapping between DTOs and domain entities
- Orchestration between services
- Example: `CreatePatientService`, `ScheduleAppointmentService`

**Domain (Business Rules)**:
- Entity models (Patient, Appointment, Visit, etc.)
- Value objects
- Domain-level validation
- Business rules enforcement
- Example: `Patient` entity with `IsValidAge()` rule

**Infrastructure (Data Access)**:
- Dapper repositories
- Database connection management
- Transactions (Unit of Work)
- Database migrations
- External service integrations
- Example: `PatientRepository`, `AppointmentRepository`

---

### Project Structure

```
PatientManagement.API/
├── PatientManagement.API/                 # Presentation/API Layer
│   ├── Controllers/
│   │   ├── PatientController.cs
│   │   ├── AppointmentController.cs
│   │   ├── ConsultationController.cs
│   │   ├── PrescriptionController.cs
│   │   └── ExportController.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   │   ├── CreatePatientRequest.cs
│   │   │   ├── CreateAppointmentRequest.cs
│   │   │   └── ...
│   │   ├── Responses/
│   │   │   ├── PatientResponse.cs
│   │   │   ├── AppointmentResponse.cs
│   │   │   └── ...
│   │   └── Shared/
│   │       ├── PaginatedResponse.cs
│   │       └── ApiResponse.cs
│   ├── Middlewares/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── AuthenticationMiddleware.cs
│   │   └── LoggingMiddleware.cs
│   ├── Filters/
│   │   ├── ValidationFilter.cs
│   │   └── AuthorizationFilter.cs
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── ApplicationBuilderExtensions.cs
│   ├── Program.cs (Startup configuration)
│   └── appsettings.json (Configuration)
│
├── PatientManagement.Application/         # Application/Business Logic Layer
│   ├── Services/
│   │   ├── PatientService.cs
│   │   ├── AppointmentService.cs
│   │   ├── ConsultationService.cs
│   │   ├── PrescriptionService.cs
│   │   ├── ExportService.cs
│   │   └── AuthService.cs
│   ├── Validators/
│   │   ├── CreatePatientValidator.cs
│   │   ├── CreateAppointmentValidator.cs
│   │   ├── CreateConsultationValidator.cs
│   │   └── ...
│   ├── Mappings/
│   │   └── MappingProfile.cs (AutoMapper)
│   ├── Models/
│   │   ├── Requests/
│   │   │   ├── CreatePatientModel.cs
│   │   │   └── ...
│   │   └── Responses/
│   │       ├── PatientModel.cs
│   │       └── ...
│   ├── Interfaces/
│   │   ├── IPatientService.cs
│   │   ├── IAppointmentService.cs
│   │   └── ...
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
│
├── PatientManagement.Domain/              # Domain Layer
│   ├── Entities/
│   │   ├── Patient.cs
│   │   ├── Appointment.cs
│   │   ├── Visit.cs
│   │   ├── Vitals.cs
│   │   ├── Complaint.cs
│   │   ├── Diagnosis.cs
│   │   ├── Medication.cs
│   │   ├── Prescription.cs
│   │   ├── AuditLog.cs
│   │   └── User.cs
│   ├── ValueObjects/
│   │   ├── PhoneNumber.cs
│   │   ├── BloodPressure.cs
│   │   └── ...
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   ├── PatientNotFoundException.cs
│   │   ├── DuplicatePatientException.cs
│   │   └── ...
│   ├── Interfaces/
│   │   ├── IPatientRepository.cs
│   │   ├── IAppointmentRepository.cs
│   │   ├── IUnitOfWork.cs
│   │   └── ...
│   └── Constants/
│       └── BusinessRules.cs
│
├── PatientManagement.Infrastructure/      # Infrastructure/Data Layer
│   ├── Persistence/
│   │   ├── SqlServerContext.cs (DB Connection)
│   │   ├── Migrations/ (DB Versions)
│   │   └── Scripts/ (SQL Scripts)
│   ├── Repositories/
│   │   ├── PatientRepository.cs
│   │   ├── AppointmentRepository.cs
│   │   ├── ConsultationRepository.cs
│   │   ├── RepositoryBase.cs (Base class)
│   │   └── ...
│   ├── UnitOfWork/
│   │   └── UnitOfWork.cs
│   ├── Security/
│   │   ├── JwtTokenProvider.cs
│   │   ├── PasswordHasher.cs
│   │   └── TokenValidator.cs
│   ├── Logging/
│   │   ├── LoggerAdapter.cs
│   │   └── AuditLogger.cs
│   ├── ExternalServices/
│   │   ├── PdfExportService.cs
│   │   └── CsvExportService.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
│
└── PatientManagement.Tests/               # Unit Tests
    ├── UnitTests/
    │   ├── Services/
    │   ├── Repositories/
    │   └── Validators/
    └── IntegrationTests/
        ├── Controllers/
        └── Services/
```

---

### API Design Strategy

**RESTful Principles**:
- Resources identified (Patient, Appointment, Visit, etc.)
- HTTP verbs used correctly (GET, POST, PUT, DELETE, PATCH)
- URLs represent resources, not actions
- Consistent URL structure

**API Endpoints** (Sample):

```
PATIENT ENDPOINTS:
GET    /api/v1/patients                      # List all patients
POST   /api/v1/patients                      # Create patient
GET    /api/v1/patients/{id}                 # Get patient details
PUT    /api/v1/patients/{id}                 # Update patient
DELETE /api/v1/patients/{id}                 # Delete patient (soft)
GET    /api/v1/patients/search?query=name    # Search patients
GET    /api/v1/patients/{id}/history         # Get patient's visit history

APPOINTMENT ENDPOINTS:
GET    /api/v1/appointments                           # List appointments
POST   /api/v1/appointments                           # Create appointment
GET    /api/v1/appointments/{id}                      # Get appointment
PUT    /api/v1/appointments/{id}                      # Update appointment
DELETE /api/v1/appointments/{id}                      # Cancel appointment
GET    /api/v1/appointments/date/{date}               # Get appointments for date
PATCH  /api/v1/appointments/{id}/status?status=completed  # Update status

CONSULTATION ENDPOINTS:
POST   /api/v1/consultations                 # Create consultation/visit
GET    /api/v1/consultations/{id}            # Get consultation details
PUT    /api/v1/consultations/{id}            # Update consultation
GET    /api/v1/patients/{patientId}/consultations  # Get patient's consultations

PRESCRIPTION ENDPOINTS:
GET    /api/v1/prescriptions/{consultationId}  # Get prescription
POST   /api/v1/prescriptions/{consultationId}/print  # Print prescription

EXPORT ENDPOINTS:
GET    /api/v1/export/patients-csv           # Export patient list to CSV
GET    /api/v1/export/visits-csv?from=&to=   # Export visits to CSV
GET    /api/v1/export/visits-pdf?visitId=    # Export visit to PDF

AUTH ENDPOINTS:
POST   /api/v1/auth/login                    # Login, return JWT
POST   /api/v1/auth/logout                   # Logout
POST   /api/v1/auth/refresh-token            # Refresh JWT token
```

**Response Format**:
```json
{
  "success": true,
  "data": { /* actual response */ },
  "error": null,
  "timestamp": "2026-11-10T14:30:00Z"
}
```

**Error Response Format**:
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "PATIENT_NOT_FOUND",
    "message": "Patient with ID 123 not found",
    "details": null
  },
  "timestamp": "2026-11-10T14:30:00Z"
}
```

**API Versioning**: `/api/v1/` in URL (prepare for v2 in future)

---

### Service Layer Approach

**Service Pattern**:
Each feature has corresponding service class handling business logic

**Example: PatientService**:
```
IPatientService
├── GetPatientAsync(id)
├── GetAllPatientsAsync()
├── SearchPatientsAsync(query)
├── CreatePatientAsync(request)
├── UpdatePatientAsync(id, request)
├── DeletePatientAsync(id)
└── GetPatientWithHistoryAsync(id)
```

**Service Characteristics**:
- Async/await throughout
- Inject dependencies (repositories, validators, external services)
- Throw domain exceptions for business rule violations
- Map between DTOs and domain entities
- Call validators
- Coordinate between repositories (Unit of Work)

---

### Dependency Injection Usage

**Service Registration Guidance**:
- Each layer exposes one registration extension method (Application, Infrastructure) called from the API host
- Application services (patient, appointment, consultation, prescription, export, auth): **Scoped**
- Repositories, Unit of Work, connection factory: **Scoped** (one connection/transaction per request)
- JWT token provider, password hasher, clock abstraction: **Singleton** (stateless)
- FluentValidation validators: registered by assembly scan
- Current-user context (reads claims from the HTTP context): **Scoped**

**Benefits**:
- Loose coupling (depend on interfaces)
- Easy to test (mock dependencies)
- Centralized configuration
- Lifetime management (Scoped for request-scoped repositories)

---

### Exception Handling Middleware

**Custom Exceptions** (Domain Layer):
- `PatientNotFoundException`
- `DuplicatePatientException`
- `AppointmentConflictException`
- `ValidationException`

**Exception Handling Middleware**:
- Catch exceptions globally
- Map to appropriate HTTP status codes
- Return consistent error responses
- Log exceptions
- Never expose sensitive details

**Status Code Mapping**:
- 400: Validation errors
- 401: Unauthorized (no JWT)
- 403: Forbidden (no permission)
- 404: Resource not found
- 409: Conflict (duplicate, appointment conflict)
- 500: Unexpected server error

---

### Logging Strategy

**Log Levels**:
- **Debug**: Detailed diagnostic info (variable values, method entry/exit)
- **Information**: General app flow (login, service calls completed)
- **Warning**: Potentially harmful situations (missing optional data)
- **Error**: Error events (exceptions, failed operations)
- **Critical**: Very serious errors (system cannot recover)

**Log Points**:
- Service method entry/exit
- Exception details
- Database query execution (slow queries)
- API request/response (timestamp, user, endpoint)
- Audit events (create/update/delete)

**Implementation**: Inject ILogger into services

---

### Validation Strategy

**Input Validation Layers**:

1. **Model-Level** (Data annotations):
   - Basic validation attributes
   - Rarely sufficient alone

2. **FluentValidation** (Validators):
   - Complex business logic
   - Cross-field validation
   - Database lookups
   - Example: `CreatePatientValidator` ensures no duplicate

3. **Domain-Level** (Entity methods):
   - Business rule enforcement
   - Example: `Patient.ValidateAge()` ensures age >0 and <150

**Validator Examples**:
- CreatePatientValidator: required fields, phone format, age range
- CreateAppointmentValidator: date not in past, no double-booking
- CreateConsultationValidator: all vitals present and in range

---

### API Versioning Approach

**URL-based Versioning** (Chosen approach):
- `/api/v1/patients` (current)
- `/api/v2/patients` (future, if needed)

**Benefits**:
- Clear version in URL
- Easy to support multiple versions simultaneously
- Easy for clients to understand

**Implementation**:
- Separate controllers per version (if significantly different)
- Or conditional logic based on version parameter

---

### DTOs & Models Identified

**Request DTOs** (Input):
- `CreatePatientRequest` (name, DOB, gender, phone, address)
- `UpdatePatientRequest` (all updatable fields)
- `CreateAppointmentRequest` (patientId, date, time, notes)
- `UpdateAppointmentStatusRequest` (status)
- `CreateConsultationRequest` (patientId, appointmentId)
- `AddVitalsRequest` (temperature, BP systolic/diastolic, pulse)
- `AddComplaintRequest` (text)
- `AddDiagnosisRequest` (text, optional code)
- `AddMedicationRequest` (name, dosage, frequency, duration, instructions)

**Response DTOs** (Output):
- `PatientResponse` (all patient details)
- `PatientSummaryResponse` (limited patient info for lists)
- `AppointmentResponse` (appointment details)
- `ConsultationResponse` (full consultation details)
- `VitalsResponse` (vitals data)
- `PrescriptionResponse` (prescription details)
- `AuditLogResponse` (audit trail entry)

**Common DTOs**:
- `PaginatedResponse<T>` (list results with pagination)
- `ApiResponse<T>` (consistent response envelope)
- `ErrorResponse` (error details)

---

### Controllers Identified

- `PatientController`: Patient CRUD, search, history
- `AppointmentController`: Appointment CRUD, calendar, status updates
- `ConsultationController`: Create consultation, add details
- `PrescriptionController`: Generate/retrieve prescriptions
- `ExportController`: CSV/PDF exports
- `AuthController`: Login, logout, refresh token
- `HealthController`: System health check (monitoring)

---

## Dapper Design Recommendations

### Repository Structure

**Repository Pattern** with Dapper:
```
IPatientRepository
├── GetPatientAsync(id): Task<Patient>
├── GetAllPatientsAsync(): Task<IEnumerable<Patient>>
├── SearchPatientsAsync(query): Task<IEnumerable<Patient>>
├── CreatePatientAsync(patient): Task<int>
├── UpdatePatientAsync(patient): Task<bool>
├── DeletePatientAsync(id): Task<bool>
├── GetPatientWithHistoryAsync(id): Task<PatientWithHistory>
└── CheckDuplicateAsync(firstName, lastName, dob): Task<bool>
```

**Generic Repository Base** (Optional):
- Base repository for common CRUD operations
- Reduces code duplication
- Entity-specific repositories extend base

**Dapper Configuration Guidance**:
- Obtain connections only through a connection factory abstraction (never construct connections inside services)
- Register custom type handlers only for value objects (e.g., blood pressure, phone number), not whole entities
- Map to persistence models or entities explicitly; keep DTO mapping in the Application layer

---

### Query Organization Approach

**Query Organization Strategy**:

**Option 1: Queries in Repository** (Recommended for Phase 1):
- SQL queries in private methods
- Cleaner class structure
- Single responsibility (repository handles all patient data access)
- Example: `_patientQuery` private method

**Option 2: Separate Query Classes** (For complex queries):
- `PatientQueries` class containing all patient SQL queries
- Better for very complex queries
- Separates concerns (query definition vs execution)

**Query Formatting Guidance**:
- Keep SQL as named constants grouped per repository (or a per-aggregate query class)
- List columns explicitly; never `SELECT *`
- Always pass values as parameters; never concatenate input into SQL text
- Apply the soft-delete filter (`IsActive`) consistently in every read query
- Use async Dapper methods throughout

---

### Transaction Strategy

**Unit of Work Pattern**:
- Manages transactions across repositories
- Ensures atomic operations
- Example: Create patient + log audit = both succeed or both fail

**Unit of Work Design Guidance**:
- Unit of Work owns one connection and one transaction per request
- Exposes repositories that share that connection/transaction
- Provides begin, commit, and rollback operations; disposes cleanly on failure
- Services open a transaction only for multi-statement writes

**Operations That Require a Transaction**:
- Completing a consultation (visit + vitals + complaints + diagnosis + medications + appointment status update + audit entries)
- Creating a patient with its audit entry
- Rescheduling an appointment (status change + new slot)
- Read-only queries do **not** need a transaction

---

### Stored Procedure vs Inline Query Recommendation

**Stored Procedures**: Use for:
- Complex calculations or business logic
- Performance-critical queries
- Frequently executed queries needing optimization
- Example: Calculate patient age stats, generate monthly reports

**Inline Queries (Dapper)**: Use for:
- Simple CRUD operations (recommended)
- Dynamic queries based on filters
- Maintainability (SQL alongside C# code)
- Easier version control and testing

**Phase 1 Recommendation**: Primarily parameterized inline queries. Candidates for stored procedures later: patient visit-history aggregation and large CSV/PDF export datasets, if profiling shows a need.

---

### SQL Performance Considerations

**Indexing Strategy**:
- Primary Key: PatientId, AppointmentId, VisitId (auto-indexed)
- Foreign Keys: PatientId on Appointments, Visits (indexed)
- Search Columns: Patient.PhoneNumber, Patient.FirstName (for search performance)
- Date Filters: Appointment.AppointmentDate, Visit.VisitDate
- Active Status: Patient.IsActive, Appointment.IsActive (filter indexes)

**Query Optimization**:
- Select only needed columns (avoid SELECT *)
- Use WHERE clauses (don't load all records then filter)
- Pagination for large result sets (OFFSET/FETCH)
- Join optimization (avoid Cartesian products)
- Query analysis: EXPLAIN PLAN or SQL Server Execution Plan

**N+1 Query Prevention**:
- Load related data in single query (JOIN) vs multiple queries
- Example: Don't loop loading medications for each visit, join in single query
- Use Dapper's `GridReader` for multiple result sets in one call

---

### Pagination Strategy

**Pagination Approach**:
- Server-side OFFSET/FETCH paging with a deterministic ORDER BY (e.g., last name, first name, id)
- Return total count and the page in a single round trip (multiple result sets)
- Standard paged response: items, total count, page number, page size
- Cap page size (e.g., max 100) to protect the server
- Use keyset (seek) paging only if visit history grows large enough for OFFSET to slow down

**Benefits**:
- Limits memory usage (don't load all patients)
- Better performance (database-level filtering)
- Better UI (pages can be requested on demand)

---

### Audit Field Strategy

**Audit Fields on Every Entity**:
- `CreatedDate`: DateTime when record created
- `CreatedBy`: UserId who created record
- `ModifiedDate`: DateTime of last modification
- `ModifiedBy`: UserId who last modified

**Audit Log Table** (Separate):
- `AuditLogId`
- `TableName`
- `RecordId`
- `ActionType` (Create, Update, Delete)
- `OldValue` (JSON before change)
- `NewValue` (JSON after change)
- `Timestamp`
- `UserId`

**Audit Field Population**:
- Set audit fields in one place (a base repository or Application-layer helper), never in individual controllers
- Take the timestamp from an injectable clock (UTC) and the user from the current-user context (JWT claims)
- Write the audit log row in the same transaction as the data change

---

## SQL Server Database Recommendations

### Database Schema Candidates

**Master Tables** (Reference data, slow-changing):
- `Users` (doctor account)
- `Statuses` (appointment statuses)
- `Genders` (M/F/Other)

**Transaction Tables** (Frequent changes, large volume):
- `Patients` (core data, few changes per patient)
- `Appointments` (frequent changes during day)
- `Visits` (frequent additions)
- `Vitals` (per visit)
- `Complaints` (per visit)
- `Diagnoses` (per visit)
- `Medications` (per visit)
- `Prescriptions` (per visit)
- `AuditLogs` (append-only)

**Relationship Design**:
```
Users (1) → (N) Patients
Users (1) → (N) AuditLogs

Patients (1) → (N) Appointments
Patients (1) → (N) Visits

Appointments (1) → (0-1) Visit

Visits (1) → (1) Vitals
Visits (1) → (N) Complaints
Visits (1) → (N) Diagnoses
Visits (1) → (N) Medications
Visits (1) → (1) Prescriptions
```

---

### Index Recommendations

**Clustered Indexes** (Primary Keys):
- `Patients.PK_Patients` on `PatientId`
- `Appointments.PK_Appointments` on `AppointmentId`
- `Visits.PK_Visits` on `VisitId`

**Non-Clustered Indexes** (Search/Filter):
- `IX_Patients_PhoneNumber` on `PhoneNumber` (search)
- `IX_Patients_FirstNameLastName` on `(FirstName, LastName)` (search)
- `IX_Patients_IsActive` on `IsActive` (filter)
- `IX_Appointments_PatientId` on `PatientId` (FK)
- `IX_Appointments_AppointmentDate` on `AppointmentDate` (calendar view)
- `IX_Appointments_IsActive` on `IsActive` (filter)
- `IX_Visits_PatientId` on `PatientId` (history)
- `IX_Visits_VisitDate` on `VisitDate` (date range filters)

**Covering Indexes** (if needed for search performance):
- `IX_Patients_Phone_INCLUDE` (PhoneNumber INCLUDE all patient detail columns)
- Provides fast search + all data without lookup

---

### Audit Columns & Soft Delete Strategy

**Audit Columns on All Transactional Tables**:

| Column | Type (logical) | Purpose |
|--------|----------------|---------|
| IsActive | Boolean, default true | Soft delete flag |
| CreatedDate | UTC datetime | Record creation time |
| CreatedBy | User reference | Creator |
| ModifiedDate | UTC datetime, nullable | Last change time |
| ModifiedBy | User reference, nullable | Last modifier |
| RowVersion | Concurrency token | Optimistic concurrency (prevents lost updates) |

**Soft Delete Implementation**:
- Instead of DELETE, set `IsActive = 0`
- Preserves data for auditing
- Prevents orphaned foreign key issues
- All queries include `WHERE IsActive = 1`
- Hard delete only in extreme cases (purge old data per retention policy)

**Audit Log Table Design**:
- Columns: id (64-bit), table name, record id, action type (Create/Update/Delete/Archive/Export/Print), old value (JSON), new value (JSON), UTC timestamp, user id, IP address
- Indexes: (table name, record id) for record history; timestamp for date-range review
- Append-only: the application database user gets INSERT and SELECT only, no UPDATE/DELETE
- Consider SQL Server temporal tables for clinical tables as an alternative or complement

---

### Normalization Recommendations

**Normalization Level**: 3NF (Third Normal Form)

**Rationale**: 
- Eliminates data redundancy
- Ensures data integrity
- Performance acceptable for Phase 1 scale
- Can denormalize specific queries later if needed

**Example: Proper Normalization**

- ❌ **Avoid**: storing all medications as one delimited text column on the visit
- ✅ **Prefer**: a separate Medications table with one row per medicine, linked to the visit

**Snapshot Exception**: a printed prescription is a legal document. Store a snapshot of the clinic header, patient details, and medications as they were at print time, so later edits to master data don't change past prescriptions.

**Denormalization Considerations** (Phase 2):
- Cache frequently calculated fields (e.g., PatientAge from DOB)
- Add calculated columns for frequently used aggregations
- Create summary/reporting tables from operational data

---

## Authentication and Authorization Strategy

### JWT Implementation Approach

**JWT (JSON Web Token) Flow**:

**1. Login**:
```
User sends: POST /api/v1/auth/login
  { username, password }

Backend:
  - Verify username/password
  - Hash check password
  - Generate JWT token with claims
  - Generate refresh token
  - Return both tokens

Response: { accessToken, refreshToken }
```

**2. Authenticated Requests**:
```
Client sends: GET /api/v1/patients
  Header: Authorization: Bearer {accessToken}

Backend:
  - Validate JWT signature
  - Check expiration
  - Extract user claims
  - Allow request if valid

Response: Patient data
```

**3. Token Refresh**:
```
When accessToken expires, client sends:
  POST /api/v1/auth/refresh-token
    { refreshToken }

Backend:
  - Validate refresh token
  - Issue new accessToken (same refresh token)
  
Response: { accessToken, refreshToken }
```

**JWT Token Structure**:
```
Header.Payload.Signature

Payload contains claims:
{
  "sub": "username",
  "userId": 1,
  "iat": 1699625400,
  "exp": 1699629000,  // 1 hour expiry
  "roles": ["doctor"]
}
```

---

### Refresh Token Strategy

**Access Token**:
- Short-lived (1 hour)
- Used for API calls
- Included in every request
- Safer to expire frequently

**Refresh Token**:
- Long-lived (30 days)
- Stored in secure HttpOnly cookie (preferred)
- Or secure storage in client app
- Used to obtain new access token
- Can be revoked

**Token Rotation** (Best practice):
- Each refresh generates new access + refresh token
- Old refresh token invalidated
- Prevents token reuse if compromised

---

### Role-Based Access Control (RBAC)

**Phase 1 Roles**:
- `Doctor`: Full system access (primary user)

**Phase 2+ Roles** (Prepared for):
- `Doctor`: Create consultations, manage patients
- `Receptionist`: Manage appointments, basic patient data
- `Admin`: System configuration, user management
- `View-Only`: View patient data, no modifications

**Permission Matrix** (Phase 2):

| Operation | Doctor | Receptionist | Admin | Viewer |
|-----------|--------|-------------|-------|--------|
| Create Patient | ✅ | ✅ | ✅ | ❌ |
| Edit Patient | ✅ | ⚠️ (basic) | ✅ | ❌ |
| Create Appointment | ✅ | ✅ | ✅ | ❌ |
| Create Consultation | ✅ | ❌ | ❌ | ❌ |
| View Patient | ✅ | ✅ | ✅ | ✅ |
| Export Data | ✅ | ❌ | ✅ | ❌ |
| Manage Users | ❌ | ❌ | ✅ | ❌ |

**Implementation**: 
- Claims-based authorization ([Authorize(Roles = "Doctor")])
- Policy-based authorization for complex rules

---

### Claims Strategy

**JWT Claims**:
- `sub`: Subject (username or userId)
- `userId`: Database user ID
- `roles`: Array of role names
- `email`: Email address
- `iat`: Issued at (timestamp)
- `exp`: Expiration (timestamp)

**Custom Claims** (if needed):
- `clinicId`: Which clinic user belongs to (Phase 2)
- `permissions`: Specific permissions (if granular control needed)

---

### Password Security Approach

**Password Hashing**:
- Use an adaptive, salted algorithm: ASP.NET Core Identity's `PasswordHasher` (PBKDF2) or bcrypt/Argon2
- Never store plain or reversibly encrypted passwords
- Each password gets a unique salt (built into these algorithms)
- Verify by hashing the supplied password and comparing in constant time
- Store the hash format/version so the work factor can be raised later

**Password Policy**:
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character (!@#$%^&*)
- No password reuse (last 5 passwords)
- Expiration: 90 days (Phase 2 requirement)

**Password Reset**:
- Send reset link via email
- Link expires in 1 hour
- New password set via secure form
- Log password change event

---

### Session Management Approach

**Session Timeout**:
- 30 minutes of inactivity → auto-logout
- Implemented via token expiration
- Client-side: Timer tracks inactivity, refreshes token before expiry
- Server-side: Validate token expiration

**Concurrent Sessions** (Phase 1):
- Single user, no concern for concurrent sessions
- Phase 2: Limit to 1 session per user (prevent sharing credentials)

**Logout**:
- Client: Clear tokens from storage
- Server: Add token to blacklist (Redis cache) if needed
- Invalidate refresh token in database

---

## Security Risk Analysis

### Authentication Risks

**Risk 1: Brute Force Attack on Login**
- **Threat**: Attacker tries many password combinations
- **Likelihood**: Medium
- **Impact**: Account takeover
- **Mitigation**:
  - Limit failed login attempts (5 attempts per 15 minutes)
  - Lock account temporarily after limit exceeded
  - Log all login attempts (audit trail)
  - Email alert on suspicious activity (Phase 2)

**Risk 2: Weak Passwords**
- **Threat**: Users choose easily guessable passwords
- **Likelihood**: High (if not enforced)
- **Impact**: Account compromise
- **Mitigation**:
  - Enforce password complexity (8+ chars, mixed case, numbers, special)
  - Check against common password dictionary
  - Educate users on password strength

**Risk 3: Credential Storage**
- **Threat**: Passwords stored in plain text or weak encryption
- **Likelihood**: Low (if bcrypt used)
- **Impact**: Mass compromise if database breached
- **Mitigation**:
  - Use bcrypt for password hashing (salted, slow)
  - Never store plain passwords
  - Audit access to user tables

---

### Authorization Risks

**Risk 4: Privilege Escalation**
- **Threat**: User elevates their own permissions
- **Likelihood**: Low (if JWT tokens not tampered)
- **Impact**: Unauthorized access to data
- **Mitigation**:
  - Validate JWT signature (cryptographic)
  - Never trust client-provided roles
  - Server-side authorization checks
  - Audit all access

**Risk 5: Horizontal Access** (Patient A accessing Patient B data)
- **Threat**: User accesses other patients' records
- **Likelihood**: Medium (if not properly checked)
- **Impact**: HIPAA violation, privacy breach
- **Mitigation**:
  - Verify patient access ownership in every API call
  - Never rely only on client-side filtering
  - Use user context (from JWT) to scope queries

---

### Data Exposure Risks

**Risk 6: Sensitive Data in Transit (Not Encrypted)**
- **Threat**: HTTPS not enforced, data intercepted over network
- **Likelihood**: Low (if HTTPS enforced)
- **Impact**: Patient data exposure (passwords, medical records)
- **Mitigation**:
  - Enforce HTTPS only (no HTTP)
  - HSTS header (tell browsers to always use HTTPS)
  - Certificate pinning (mobile apps)

**Risk 7: Sensitive Data at Rest (Not Encrypted)**
- **Threat**: Database breached, sensitive data readable
- **Likelihood**: Low (if encryption enabled)
- **Impact**: Patient privacy breach, HIPAA violation
- **Mitigation**:
  - SQL Server Transparent Data Encryption (TDE)
  - Encrypt sensitive fields (passwords, social security if captured)
  - Restrict database access (VPN, IP whitelist)

**Risk 8: Logs Containing Sensitive Data**
- **Threat**: Passwords/tokens logged in plain text
- **Likelihood**: Medium (common mistake)
- **Impact**: Compromise if logs accessible
- **Mitigation**:
  - Never log passwords or full tokens
  - Mask sensitive data in logs
  - Restrict log access
  - Use structured logging with sanitization

---

### Input Validation Risks

**Risk 9: SQL Injection**
- **Threat**: Attacker injects SQL via form inputs
- **Likelihood**: Very Low (if Dapper used with parameters)
- **Impact**: Database compromise, data theft
- **Mitigation**:
  - Use parameterized queries (Dapper does this)
  - Never concatenate user input into SQL strings
  - Input validation (whitelist known patterns)
  - Security testing (SQL injection payloads)

**Risk 10: XSS (Cross-Site Scripting)**
- **Threat**: Attacker injects JavaScript via data fields
- **Likelihood**: Low (API-only, not rendering user data on same site)
- **Impact**: Session hijacking, malware distribution
- **Mitigation**:
  - Encode output (HTML encode when displayed in Angular)
  - Content Security Policy (CSP) headers
  - Input validation (reject script tags)
  - Angular's built-in XSS protections

**Risk 11: Injection Attacks** (Command, LDAP, etc)
- **Threat**: Non-SQL injection attacks
- **Likelihood**: Low (API-based, limited external service calls)
- **Impact**: System compromise
- **Mitigation**:
  - Input validation (whitelist allowed values)
  - Parameterized queries for all data access
  - Avoid dynamic command execution

---

### CSRF Risk

**Risk 12: Cross-Site Request Forgery**
- **Threat**: Attacker tricks user into making unauthorized request
- **Likelihood**: Low (API uses JWT, not cookies)
- **Impact**: Unauthorized operations
- **Mitigation**:
  - Use JWT tokens (not cookies) for authentication
  - Validate Origin/Referer headers
  - CSRF tokens for form submissions (if applicable)
  - SameSite cookie attribute (if using cookies)

---

### API Security Risks

**Risk 13: Missing Rate Limiting**
- **Threat**: Attacker floods API with requests
- **Likelihood**: Medium
- **Impact**: Denial of Service, service unavailability
- **Mitigation**:
  - Implement rate limiting per user (e.g., 100 req/min)
  - Implement rate limiting per IP (e.g., 1000 req/min)
  - Return 429 Too Many Requests when limit exceeded
  - Exponential backoff for retries

**Risk 14: API Key Exposure**
- **Threat**: API keys hardcoded in frontend/public repos
- **Likelihood**: High (if not careful)
- **Impact**: Unauthorized API access
- **Mitigation**:
  - Use JWT tokens (time-limited) instead of static keys
  - Environment variables for any secrets
  - Never commit secrets to version control
  - Secret scanning tools (GitHub, GitLab)

**Risk 15: Insufficient Logging**
- **Threat**: Attacks go undetected, audit trail incomplete
- **Likelihood**: Medium (if logging not comprehensive)
- **Impact**: Security breaches undiscovered, HIPAA audit failure
- **Mitigation**:
  - Log all authentication attempts
  - Log data access (who accessed what when)
  - Log permission changes
  - Monitor logs for suspicious patterns
  - Alert on critical events

---

### Infrastructure & Deployment Risks

**Risk 16: Unencrypted Communication**
- **Threat**: API traffic not encrypted
- **Likelihood**: Low (if HTTPS enforced)
- **Mitigation**:
  - Enforce HTTPS in production
  - Use TLS 1.2+ (minimum)
  - Strong cipher suites
  - HSTS headers

**Risk 17: Weak Database Security**
- **Threat**: Database accessible from internet
- **Likelihood**: High (if not configured properly)
- **Impact**: Direct data compromise
- **Mitigation**:
  - Database behind VPN/private network
  - Firewall restricted to app servers only
  - Strong database passwords
  - Principle of least privilege (database user permissions)
  - Regular patching of database server

**Risk 18: Dependency Vulnerabilities**
- **Threat**: Third-party libraries have known vulnerabilities
- **Likelihood**: High (common in software ecosystem)
- **Impact**: Unknown, depends on vulnerability
- **Mitigation**:
  - Regular dependency updates
  - Security scanning tools (npm audit, Snyk)
  - Vulnerability monitoring (GitHub alerts)
  - Keep framework versions current

---

### Summary: High-Priority Mitigations

| Risk | Priority | Mitigation |
|------|----------|-----------|
| Brute Force | HIGH | Login attempt limiting |
| Weak Passwords | HIGH | Password complexity enforcement |
| Horizontal Access | CRITICAL | Verify patient ownership in API calls |
| SQL Injection | MEDIUM | Parameterized queries (Dapper) |
| Unencrypted Transit | HIGH | Enforce HTTPS, HSTS |
| Unencrypted Database | HIGH | SQL Server TDE |
| Weak DB Security | CRITICAL | VPN, firewall, private network |
| Insufficient Logging | HIGH | Comprehensive audit logging |
| Dependency Vulns | MEDIUM | Regular updates, scanning |

---

## Architecture Evaluation

### Clean Architecture Suitability

**Pros**:
✅ Clear separation of concerns (UI, business logic, data)
✅ Testable (domain logic independent of frameworks)
✅ Maintainable (changes in one layer don't ripple)
✅ Framework-agnostic (can swap Angular for Vue, SQL Server for PostgreSQL)
✅ Scales well as features grow
✅ SOLID principles naturally enforced

**Cons**:
❌ More files/classes than simple architecture (overhead for small projects)
❌ Steeper learning curve for junior developers
❌ Can feel over-engineered for very simple features

**Suitability for This Project**: ⭐⭐⭐⭐⭐ (5/5)
- Domain is clear (healthcare)
- Scalability path planned (Phase 2+)
- Team experience likely sufficient
- Long-term maintainability important

---

### Repository Pattern Suitability

**Pros**:
✅ Abstraction over data access (swap SQL Server for MongoDB later)
✅ Testable (mock repositories in tests)
✅ Centralized database queries (easier to optimize)
✅ Single source of truth for entity queries
✅ Separation of concerns

**Cons**:
❌ Extra abstraction layer (more code)
❌ Can hide query complexity
❌ Dapper already abstracts data access (repository is less necessary)

**Suitability for This Project**: ⭐⭐⭐⭐ (4/5)
- Good for consistency
- Prepares for future data store changes
- Dapper queries are already SQL-based (not much abstraction benefit)
- Recommended to implement (good practice)

---

### Unit of Work Pattern Suitability

**Pros**:
✅ Transaction management simplified
✅ Atomicity guaranteed (all-or-nothing)
✅ Audit logging coordinated
✅ Prevents partial updates

**Cons**:
❌ Added complexity (tracking state)
❌ Not needed if transactions used sparingly
❌ Dapper doesn't have built-in UoW (must implement manually)

**Suitability for This Project**: ⭐⭐⭐ (3/5)
- Useful for multi-step operations (create patient + audit log)
- Moderate complexity to implement
- Recommended if time allows

---

### CQRS Suitability (Evaluated as Requested)

**What CQRS would mean here**: Separate command handlers (writes) from query handlers (reads), usually through a mediator. Full CQRS adds separate read/write models or stores.

**Pros**:
✅ Read queries (history, search, exports) can be tuned independently of write logic
✅ Handlers are small and easy to test
✅ Cross-cutting behaviors (validation, logging, transactions) fit naturally as pipeline behaviors
✅ Dapper suits hand-written read queries that project straight into DTOs

**Cons**:
❌ One user, one database, about 8 modules: separate read/write stores add no value
❌ Mediator indirection adds files and makes navigating the code harder for a small team
❌ Popular mediator libraries have moved to commercial licensing (check licensing before adopting)
❌ Eventual consistency (from separate stores) is unacceptable for clinical records that must be visible right after saving

**Verdict**: ⭐⭐ (2/5) for full CQRS. **Not required.**

**Recommendation: "CQRS-lite" by convention only**
- Organize the Application layer into `Commands` (create/update use cases using the Unit of Work) and `Queries` (read-only Dapper queries returning DTOs directly, no transaction)
- One database and one model. No event sourcing, no separate read store.
- Plain application services or simple handler interfaces; no mediator library dependency in Phase 1
- Revisit only if Phase 2 adds heavy reporting/analytics; at that point a read replica or reporting views are the likely first step

---

### Dapper Suitability

**Pros**:
✅ Lightweight, micro-ORM (minimal overhead)
✅ Full SQL control (for complex queries)
✅ Fast performance (direct SQL execution)
✅ No magic (straightforward mapping)
✅ Large community, well-documented
✅ Great for performance-critical queries
✅ Low memory footprint

**Cons**:
❌ Manual mapping (no auto-mapping like Entity Framework)
❌ No change tracking (must manually manage state)
❌ More boilerplate code than Entity Framework
❌ Less abstraction from database

**Suitability for This Project**: ⭐⭐⭐⭐⭐ (5/5)
- Performance requirements not extreme (but not negligible)
- Schema simple enough for Dapper
- Team likely familiar with SQL
- Excellent choice for this project

**Alternative Considered**: Entity Framework Core
- More powerful (but overkill for this project)
- More abstraction (less SQL control)
- Slightly slower (performance not critical for Phase 1)
- Recommendation: Stick with Dapper

---

### SQL Server Suitability

**Pros**:
✅ Powerful, enterprise-grade database
✅ Good SQL performance
✅ Strong security features (TDE, encryption)
✅ Excellent tooling (SQL Server Management Studio)
✅ .NET ecosystem (native ADO.NET support)
✅ Backup/recovery capabilities
✅ Audit logging features

**Cons**:
❌ Licensing costs (if not using Express/Developer editions)
❌ Windows-only for older versions (though SQL Server 2017+ runs on Linux)
❌ More complex than needed for simple schemas
❌ Overkill for single-clinic app (could use SQLite for Phase 1)

**Suitability for This Project**: ⭐⭐⭐⭐ (4/5)
- Good choice for production
- SQL Server Express is free (development + production)
- Overkill for Phase 1, but good for future growth
- Alternative: SQLite for Phase 1 (free, file-based, no server)
- Recommendation: Use SQL Server Express for production-ready setup

---

### Angular 19 Suitability

**Pros**:
✅ Modern framework (latest version, current tooling)
✅ Excellent for single-page applications
✅ Strong typing (TypeScript)
✅ Component-based architecture
✅ Reactive programming (RxJS)
✅ Large ecosystem (Material, ng-bootstrap, etc)
✅ Good testing support (Jasmine, Karma)
✅ Strong community and documentation

**Cons**:
❌ Learning curve (steeper than jQuery)
❌ Setup complexity (Angular CLI helps)
❌ Larger bundle size (mitigated by lazy loading)
❌ Overkill for very simple UIs

**Suitability for This Project**: ⭐⭐⭐⭐ (4/5)
- Excellent fit for modern web app
- Reactive forms perfect for data entry
- Angular Material great for healthcare UX
- Team likely has Angular experience
- Recommendation: Excellent choice, proceed with confidence

**Alternative Considered**: React
- More lightweight
- Simpler learning curve
- Recommendation: Angular is better for this (Material, forms, typing)

---

## Missing Requirements Analysis

### Missing Business Requirements

1. **Backup & Disaster Recovery**
   - BRD mentions regular automated backups but not specifics
   - Missing: Backup frequency, retention period, recovery procedures
   - **Recommendation**: Define 24-hour backups, 30-day retention, RTO/RPO targets

2. **Data Export for Compliance**
   - Export exists but compliance requirements not specified
   - Missing: HIPAA compatibility, data retention periods, retention schedules
   - **Recommendation**: Document compliance requirements, define retention policies

3. **Multi-Clinic Expansion (Phase 2)**
   - Single clinic Phase 1, but future expansion planned
   - Missing: Multi-tenancy architecture, clinic isolation, per-clinic configuration
   - **Recommendation**: Design for multi-tenancy from start (separate tenant IDs in tables)

4. **Practice/Clinic Customization**
   - Prescription header (clinic name, address, phone) is static
   - Missing: Ability to customize clinic info, logo, prescription footer
   - **Recommendation**: Admin settings for clinic information

---

### Missing Technical Requirements

5. **API Rate Limiting**
   - No mention of rate limiting for API calls
   - Missing: Limits per user, per IP, handling of exceeding limits
   - **Recommendation**: Implement rate limiting (100 req/min per user, 1000/min per IP)

6. **Caching Strategy**
   - No mention of caching frequently accessed data
   - Missing: Which data to cache, cache invalidation strategy, TTL
   - **Recommendation**: Cache patient list, appointments (5-min TTL), invalidate on changes

7. **Asynchronous Operations**
   - Large data exports should be asynchronous
   - Missing: Export job queuing, notification when export ready
   - **Recommendation**: Queue export jobs, email download link when ready

8. **API Documentation**
   - Not explicitly mentioned
   - Missing: OpenAPI/Swagger documentation for API
   - **Recommendation**: Generate Swagger docs from API (auto-documented endpoints)

9. **Health Monitoring**
   - No mention of application health checks
   - Missing: Health endpoint, monitoring alerts, downtime notification
   - **Recommendation**: Implement `/health` endpoint, monitoring dashboard

10. **Database Scaling**
    - Single database adequate for Phase 1
    - Missing: Read replicas, sharding strategy for Phase 2+
    - **Recommendation**: Plan for read replicas if reporting added (Phase 2)

---

### Missing Security Requirements

11. **Two-Factor Authentication**
    - Not mentioned in BRD
    - Single user might not require it, but good for security
    - **Recommendation**: Implement TOTP (Time-based One-Time Password) in Phase 2

12. **Encryption Key Management**
    - Data encryption mentioned but key management not specified
    - Missing: Key storage, rotation, access control
    - **Recommendation**: Use Azure Key Vault or similar for key management

13. **Penetration Testing**
    - Not mentioned as requirement
    - Missing: Security testing before production deployment
    - **Recommendation**: Conduct security audit/penetration test before launch

14. **Incident Response Plan**
    - Not defined
    - Missing: How to respond to security breaches, data loss, downtime
    - **Recommendation**: Create incident response plan, define escalation path

15. **Compliance Certification**
    - HIPAA alignment mentioned but not formal certification
    - Missing: HIPAA compliance plan, business associate agreements
    - **Recommendation**: Plan HIPAA audit for Phase 2, define required changes

---

### Missing Reporting Requirements

16. **Analytics & Dashboards**
    - Mentioned as out-of-scope but valuable for doctor
    - Missing: Patient statistics, appointment trends, common diagnoses
    - **Recommendation**: Plan basic analytics dashboard for Phase 2

17. **Report Generation**
    - Export mentioned but complex reports not specified
    - Missing: Patient summary reports, visit summaries, clinic statistics
    - **Recommendation**: Define report requirements for Phase 2

---

### Missing Audit Requirements

18. **Audit Trail Completeness**
    - Audit mentioned but details sparse
    - Missing: Specific events to audit, retention, querying capability
    - **Recommendation**: Audit all CRUD operations, expose audit logs to doctor (read-only)

19. **User Activity Logging**
    - Who accessed what data, when, from where
    - Missing: Login/logout logging, API call logging with user context
    - **Recommendation**: Log all API calls with user, timestamp, IP address

---

### Missing Deployment Requirements

20. **Deployment Automation**
    - Not mentioned
    - **Status: Deferred (Phase 1 is local-only).** No CI/CD pipeline is set up while implementation runs on the developer workstation. Builds, tests and quality checks are run locally through the standard CLI commands. Deployment is manual.
    - **Revisit**: before the first shared or production environment. Then choose GitHub Actions or Azure DevOps and add automated tests, deployment scripts and database migration automation.

21. **Infrastructure as Code**
    - Server setup not automated
    - Missing: Infrastructure definition, environment parity (dev/staging/prod)
    - **Recommendation**: Use Terraform/CloudFormation for infrastructure setup

22. **Configuration Management**
    - Environment-specific configs not mentioned
    - Missing: Dev/staging/prod configuration differences
    - **Recommendation**: Use appsettings.{environment}.json for configuration

23. **Database Migration Strategy**
    - Schema changes not addressed
    - Missing: Versioning strategy, rollback procedures, data migration
    - **Recommendation**: Use migration tools (Flyway, Liquibase) for version control

24. **Documentation**
    - No mention of documentation requirements
    - Missing: API documentation, user guide, admin guide, deployment guide
    - **Recommendation**: Generate Swagger docs, create user/admin guides, deployment runbook

---

## Risks and Assumptions

### Technical Risks

**Risk 1: Single Clinic Scale Assumptions**
- **Issue**: Current design for 1 clinic, 1 user
- **Risk**: Refactoring needed for Phase 2 (multi-clinic/multi-user)
- **Likelihood**: High (Phase 2 is planned)
- **Impact**: Additional development effort in Phase 2
- **Mitigation**: Design with multi-tenancy in mind from start (clinic ID in all tables)

**Risk 2: Performance Under Load**
- **Issue**: No load testing done
- **Risk**: System may slow down if volume increases unexpectedly
- **Likelihood**: Medium (single clinic shouldn't exceed design capacity)
- **Impact**: Poor user experience
- **Mitigation**: Load testing before production, baseline performance metrics

**Risk 3: Data Integrity Issues**
- **Issue**: Soft deletes without proper cleanup may cause issues
- **Risk**: Orphaned records, database bloat, query complexity
- **Likelihood**: Low (if soft delete strategy well-implemented)
- **Impact**: Future performance degradation
- **Mitigation**: Regular database maintenance, clear archival policies

**Risk 4: Dapper Mapping Errors**
- **Issue**: Manual object mapping with Dapper
- **Risk**: Mismatches between SQL results and C# objects
- **Likelihood**: Low (if careful)
- **Impact**: Runtime errors, data loss
- **Mitigation**: Unit tests for all repository methods, integration tests

---

### Delivery Risks

**Risk 5: Scope Creep**
- **Issue**: "Simple" requirements often expand
- **Risk**: Additional features requested beyond Phase 1 scope
- **Likelihood**: High (common in healthcare)
- **Impact**: Delayed delivery, budget overrun
- **Mitigation**: Strict scope control, documented out-of-scope items, change request process

**Risk 6: Integration Complexity**
- **Issue**: Multiple technologies (Angular, ASP.NET, SQL Server, JWT, etc)
- **Risk**: Integration issues between layers
- **Likelihood**: Medium
- **Impact**: Rework, delays
- **Mitigation**: Early integration testing, clear interfaces between layers

**Risk 7: Testing Time Underestimated**
- **Issue**: Healthcare apps require thorough testing
- **Risk**: Insufficient testing, bugs in production
- **Likelihood**: Medium
- **Impact**: Doctor loses confidence, rework required
- **Mitigation**: Allocate 30%+ of time to testing, include doctor in UAT

**Risk 8: Doctor User Training**
- **Issue**: Doctor must adopt new system
- **Risk**: Resistance to change, slow adoption, lost benefits
- **Likelihood**: Medium (depends on UX quality and training)
- **Impact**: System under-utilized, lost ROI
- **Mitigation**: Excellent UX design, comprehensive training, post-launch support

---

### Security Risks

**Risk 9: HIPAA Compliance Gap**
- **Issue**: Healthcare data is highly regulated
- **Risk**: Non-compliance could result in legal liability
- **Likelihood**: Low (if security measures implemented)
- **Impact**: Legal consequences, patient trust loss
- **Mitigation**: Implement all security measures, audit for HIPAA compliance

**Risk 10: Data Breach**
- **Issue**: Medical records are high-value targets
- **Risk**: Unauthorized access to patient data
- **Likelihood**: Low (if security well-designed)
- **Impact**: Privacy breach, legal liability, reputation damage
- **Mitigation**: Encryption, access controls, audit logging, incident response plan

---

### Performance Risks

**Risk 11: Database Query Performance**
- **Issue**: Simple queries may degrade as data grows
- **Risk**: Long search times, slow patient history loading
- **Likelihood**: Low (for Phase 1 volume)
- **Impact**: Poor UX, slow workflows
- **Mitigation**: Proper indexing, query optimization, performance monitoring

**Risk 12: Large Export Performance**
- **Issue**: Exporting 10,000 patients to CSV
- **Risk**: Long wait time, timeout
- **Likelihood**: Low (CSV export simple)
- **Impact**: User frustration
- **Mitigation**: Asynchronous export, pagination, progress indication

---

### Dependencies & Integration Risks

**Risk 13: Third-Party Library Vulnerabilities**
- **Issue**: Dependencies (NuGet, npm packages) may have security issues
- **Risk**: Vulnerability in transitive dependency
- **Likelihood**: Medium
- **Impact**: Security breach if not patched
- **Mitigation**: Regular dependency updates, security scanning

**Risk 14: Browser Compatibility**
- **Issue**: Angular app may not work in all browsers
- **Risk**: Doctor cannot use app on their browser
- **Likelihood**: Low (Angular works well cross-browser)
- **Impact**: Unable to use system
- **Mitigation**: Test across target browsers, use compatibility layer if needed

---

### Assumptions

**Assumption 1: Single User Role**
- Assumption: Doctor is the only user in Phase 1
- Basis: BRD states "single user" phase
- Risk if Wrong: Must refactor for multi-user in Phase 2

**Assumption 2: Web Browser Available**
- Assumption: Doctor has internet-connected computer with modern browser
- Basis: "web-based" requirement
- Risk if Wrong: Offline/mobile support needed (significant scope increase)

**Assumption 3: SQL Server Available**
- Assumption: SQL Server instance available for deployment
- Basis: Technology stack choice
- Risk if Wrong: Must provision database infrastructure
- Mitigation: Use SQL Server Express (free)

**Assumption 4: Doctor Can Define Requirements**
- Assumption: Doctor available to clarify requirements/provide feedback
- Basis: Single user system requires their input
- Risk if Wrong: Misalignment with needs, poor adoption
- Mitigation: Regular check-ins, demo sessions during development

**Assumption 5: Clinic Has Single Workflow**
- Assumption: Consultation workflow is standardized (vitals → complaints → diagnosis → meds)
- Basis: BRD describes fixed workflow
- Risk if Wrong: Doctor's actual workflow different, app doesn't fit
- Mitigation: Workflow validation in Phase 0 (discovery)

**Assumption 6: Patient Volume Moderate**
- Assumption: <10,000 patients, <100 visits/day
- Basis: Small clinic context
- Risk if Wrong: Performance issues, infrastructure overload
- Mitigation: Load testing, capacity planning

**Assumption 7: Backup Infrastructure Provided**
- Assumption: Hosting environment provides backup capability
- Basis: "Regular automated backups" mentioned
- Risk if Wrong: Data loss risk
- Mitigation: Confirm backup strategy before launch

**Assumption 8: HIPAA Compliance Not Required for Phase 1**
- Assumption: Phase 1 may not be HIPAA-certified but designed for future compliance
- Basis: "Security" mentioned but not "HIPAA"
- Risk if Wrong: Cannot store real patient data legally
- Mitigation: Implement HIPAA measures, confirm before production launch

---

## Open Questions

### Decisions Log (Resolved with Product Owner, 2026-11-10)

| # | Question | Decision | Impact on Design |
|---|----------|----------|------------------|
| 1 | Appointment slot length | **15 minutes**, fixed | Daily schedule in 15-min slots; one patient per slot |
| 2 | Editing completed visits | **Fully editable** at any time | No visit lock. Every edit writes old/new values to the audit log; record "last modified" on the visit and show it in history. Reprinted prescriptions reflect current data, so store the print date and a snapshot per print. |
| 3 | Extra patient fields | **Blood group and chronic conditions** added to BRD fields | Patient gets a blood group (fixed list) and a chronic-conditions field (free text in Phase 1). Show them on the consultation screen and patient profile. |
| 4 | Prescription customisation | **Editable text settings**, no logo | Settings page for clinic name, doctor name, qualifications, registration no., address, phone, footer notes; stored as one clinic-profile record |
| 5 | Visit history filters | **Date range + keyword search** | Keyword search over complaints, diagnosis and medicine names within one patient's history; needs full-text or indexed LIKE strategy |
| 6 | Medicine entry | **Free text + suggestions from own history** | Autocomplete from distinct medicine names previously prescribed; no drug master table |
| 7 | Out-of-range vitals | **Warn, allow save**; block only impossible values | Two ranges per vital: a "normal" range (warning) and a "plausible" range (hard validation) |
| 8 | Record retention | **Keep indefinitely**; soft delete only | No purge jobs in Phase 1; revisit when the legal retention period is confirmed |
| 9 | Hosting | **Development**: Angular CLI dev server + ASP.NET Core API on Kestrel/IIS Express + SQL Server LocalDB (`(LocalDb)\MSSQLLocalDB`, Windows Authentication), all on the developer workstation. **Production (future)**: Angular on IIS; API on IIS or Azure App Service; SQL Server Standard Edition or Azure SQL Database. | Connection strings per environment (`appsettings.Development.json` uses LocalDB + Windows auth; production uses a least-privilege SQL login or managed identity, kept in a secret store). **LocalDB has no Transparent Data Encryption**, so the "encryption at rest" requirement is met only in production. Development must use synthetic test data, never real patient records. Dev CORS: allow `http://localhost:4200` only. |
| 10 | Doctor login method | **Username + password with JWT** (access + refresh tokens) | As designed in the Authentication section; independent of the Windows auth used for the database |
| 11 | Browser support | **Chrome, Edge, Safari** (latest two versions) | E2E tests run on Chromium and WebKit; Firefox not formally supported |
| 12 | Devices | **Desktop/laptop + tablet** | Desktop-first layouts, tested at tablet width; consultation form must be touch-friendly; phones not optimised |
| 13 | Accessibility | **WCAG 2.1 AA, best effort** | Accessibility checks run locally with automated tools during development; not a release blocker |
| 14 | Language | **English only, i18n-ready** | UI and prescription text kept in translation files from day one |
| 15 | Encryption in transit | **TLS 1.2 minimum, 1.3 preferred** | HTTPS redirection + HSTS in production; ASP.NET Core dev certificate locally |
| 16 | Encryption at rest | **TDE + encrypted backups** (production only) | Production database must be Azure SQL or SQL Server 2019+ Standard/Enterprise; no column-level encryption, so keyword search (Q5) stays possible |
| 17 | Audit scope | **Data changes only** | Audit log covers create/update/delete with old/new values. Logins, exports, prints and record views are **not** audited. Failed-login counts are still kept on the user record so lockout works. |
| 18 | Backup / recovery | **No backup for now** | ⚠️ **Conflicts with BRD** ("No data loss", "Regular automated backups"). Accepted for development on LocalDB with synthetic data only. **Must be decided before production go-live**; the FinishingAgent should treat a missing backup plan as a deployment blocker. |
| 19 | Budget / timeline | **No fixed deadline** | PlanningAgent sets milestones from effort estimates; MoSCoW priorities guide order |
| 20 | UAT | **Doctor, 2 weeks, synthetic test data** | Plan a 2-week UAT window plus a fix cycle before release; prepare a seeded demo dataset |
| 21 | Post-launch support | **Development team, 1 month** | Hypercare for 1 month after go-live, then support on request |
| 22 | Future expansion | **Multi-user likely, single clinic** | Keep policy-based RBAC with only the Doctor role seeded. **No ClinicId / multi-tenancy columns.** |

**Superseded recommendations** (the decisions above take precedence over the following text elsewhere in this review):
- *Risks → Risk 1* and *Missing Requirements → item 3*: "design with multi-tenancy / clinic ID in all tables" is **withdrawn** (Q22).
- *Non-Functional → Availability & Reliability* and *SQL Server → Backup & Recovery*: daily backups, 30-day retention and RPO/RTO targets are **deferred** until a production backup decision is made (Q18).
- *Audit Requirements → item 19* and *Security → Insufficient Logging*: login and API-access audit logging is **out of scope** (Q17). Application logs may still record errors, but not as an audit trail.
- *Patient entity*: add blood group and chronic conditions (Q3).
- *Visit entity*: "cannot modify completed visit / immutable" is **withdrawn**; visits stay editable with full change auditing (Q2).
- *Appointment scheduling*: slot interval fixed at 15 minutes (Q1).
- *Missing Deployment Requirements → item 20* and *Deployment Recommendation → CI/CD Pipeline*: CI/CD pipeline and automated deployment are **deferred**. Implementation is local-only for now; quality gates run locally and deployment is manual until a shared environment is needed.

### Functional Questions

1. **Appointment Duration**: How long is each appointment slot? (15, 30, 60 minutes?)
   - **Impact**: Appointment scheduling UI, calendar granularity
   - **Recommendation**: Confirm with doctor, likely 15-30 minute slots

2. **Consultation Data Immutability**: Can doctor edit consultation after marked complete?
   - **Impact**: Data integrity, audit trail requirements
   - **Recommendation**: Recommend no edit after complete (create new visit if change needed)

3. **Patient Demographics Minimum**: Are there additional patient fields needed? (emergency contact, insurance, medical history?)
   - **Impact**: Database schema, form complexity
   - **Recommendation**: Confirm scope with BRD (name, age, gender, contact sufficient for Phase 1)

4. **Prescription Customization**: Can doctor customize prescription template? (header logo, footer notes?)
   - **Impact**: Prescription generation complexity
   - **Recommendation**: Start with static template, add customization in Phase 2

5. **Visit History Filtering**: Can doctor filter visit history by type? (by diagnosis, by medication, date range?)
   - **Impact**: Database indexing, search functionality
   - **Recommendation**: Implement date range filter initially, add advanced filters in Phase 2

6. **Medication Database**: Should system provide standard medication list or free text?
   - **Impact**: Autocomplete, validation, inventory tracking
   - **Recommendation**: Free text for Phase 1 (simple), build medication database in Phase 2

7. **Vital Signs Ranges**: What are acceptable ranges for vitals? (temp 35-42C, BP 60-200/40-130, pulse 40-200?)
   - **Impact**: Validation logic, warnings
   - **Recommendation**: Define and implement range checking, warn on abnormal values

8. **Data Retention**: How long should old patient records be retained? (indefinitely, 7 years per HIPAA, other?)
   - **Impact**: Database size, archive strategy, compliance
   - **Recommendation**: Confirm with doctor/legal, recommend 7 years minimum

---

### Technical Questions

9. **Database Location**: On-premise or cloud? (AWS, Azure, local server?)
   - **Impact**: Infrastructure, deployment, disaster recovery
   - **Recommendation**: Recommend cloud (Azure for .NET), confirm with stakeholders

10. **Authentication Method**: Is username/password sufficient or should SSO/integration with EMR be considered?
    - **Impact**: Auth implementation complexity
    - **Recommendation**: Start with username/password, plan SSO for Phase 2

11. **Multi-Browser Support**: Must support Safari, Firefox, Edge or just Chrome?
    - **Impact**: Testing scope, browser-specific issues
    - **Recommendation**: Support major browsers (Chrome, Edge, Safari, Firefox)

12. **Mobile Device Support**: Must work on iPad/tablet or just desktop?
    - **Impact**: Responsive design requirements, touch optimization
    - **Recommendation**: Design responsive for tablets (doctor may use iPad), mobile app Phase 2

13. **Accessibility Requirements**: WCAG 2.1 Level AA compliance needed?
    - **Impact**: UI design, component selection, testing
    - **Recommendation**: Aim for WCAG 2.1 AA, not a blocking requirement for Phase 1

14. **Language Support**: English only or multi-language needed?
    - **Impact**: UI localization, form labels, messages
    - **Recommendation**: English for Phase 1, internationalization in Phase 2

---

### Security Questions

15. **Encryption in Transit**: TLS 1.2+ mandatory or sufficient?
    - **Impact**: Security configuration, infrastructure
    - **Recommendation**: Require TLS 1.2+ minimum, prefer TLS 1.3

16. **Encryption at Rest**: Database encryption mandatory?
    - **Impact**: Database configuration, performance (minimal)
    - **Recommendation**: Implement SQL Server TDE, encrypt sensitive fields

17. **Audit Logging Scope**: What events must be logged? (all data changes, logins, exports, errors?)
    - **Impact**: Audit table design, storage, query performance
    - **Recommendation**: Log all data changes (create/update/delete), logins, exports

18. **Disaster Recovery RTO/RPO**: What are acceptable recovery time/point objectives?
    - **Impact**: Backup strategy, infrastructure
    - **Recommendation**: RTO <4 hours, RPO <1 hour sufficient for Phase 1

---

### Business Questions

19. **Budget Constraints**: Is there a fixed budget or time constraint?
    - **Impact**: Feature prioritization, tech debt acceptance
    - **Recommendation**: Confirm scope matches budget, plan Phase 2 separately

20. **User Acceptance Testing**: Who will perform UAT? How long?
    - **Impact**: Timeline, feedback loop
    - **Recommendation**: Doctor performs UAT, allocate 2-3 weeks before launch

21. **Support Model**: Will development team provide post-launch support?
    - **Impact**: Launch readiness, documentation requirements
    - **Recommendation**: Plan 1-month post-launch support at minimum

22. **Future Expansion Plans**: Confirmed timeline for multi-user/multi-clinic?
    - **Impact**: Architecture decisions, scalability design
    - **Recommendation**: Plan Phase 2 for Q2/Q3 (assuming Q1 Phase 1), confirm with stakeholders

---

## Final Recommendations

### Angular 19 Recommended Implementation

**Architecture**:
- Standalone components throughout (no NgModules); providers configured in `app.config.ts`
- Feature-based folders (dashboard, patients, appointments, consultation, prescription, export, settings), each lazy-loaded via its own route file
- `core/` for root-provided singletons (auth, interceptors, guards, error handling)
- `shared/` for standalone presentational components, pipes, and directives
- Built-in control flow (`@if`, `@for`) and `@defer` for non-critical UI

**State Management**:
- Phase 1: Signals-based feature stores plus RxJS for HTTP and search streams
- Phase 2+: Consider NgRx SignalStore only if multi-role concurrency makes state complex

**UI Component Library**:
- Angular Material for professional, consistent UI
- Custom theming for clinic branding
- Responsive design (desktop + tablet support)

**Form Handling**:
- Reactive Forms (FormBuilder) for complex forms
- FluentValidation mirrored on client-side
- Real-time validation feedback

**Performance Optimization**:
- Lazy loading feature modules
- OnPush change detection for components
- Virtual scrolling for long lists (patient history)
- Compression, minification in production build

**Testing Strategy**:
- Unit tests: Jest or Vitest (Karma is deprecated; Jasmine is acceptable if the team prefers it)
- E2E tests: Playwright or Cypress (Protractor is end-of-life)
- 70%+ code coverage target; 90%+ on consultation and prescription flows

---

### ASP.NET Core Web API Recommended Implementation

**Architecture**:
- Clean Architecture with 4 layers (Presentation, Application, Domain, Infrastructure)
- Repository Pattern for data access abstraction
- Unit of Work Pattern for multi-statement write transactions
- CQRS-lite: Commands/Queries folder convention in the Application layer; no mediator library, no separate read store
- Dependency Injection throughout
- Policy-based authorization so Phase 2 roles need only data changes

**Project Structure**:
- Separate projects per layer (.API, .Application, .Domain, .Infrastructure)
- Organized controllers, services, repositories, DTOs

**API Design**:
- RESTful endpoints following HTTP conventions
- Versioning via URL (/api/v1/)
- Consistent response format (success, data, error, timestamp)
- API documentation via Swagger/OpenAPI

**Authentication & Authorization**:
- JWT tokens (1-hour expiry)
- Refresh token rotation (30-day expiry)
- Role-based access control prepared for Phase 2

**Data Validation**:
- FluentValidation for complex business rules
- Input sanitization, output encoding
- Domain-level validation in entities

**Performance**:
- Dapper for efficient SQL execution
- Query optimization with proper indexes
- Caching for frequently accessed data (patients list, appointments)
- Rate limiting to prevent abuse

**Testing**:
- Unit tests for services and repositories (80%+ coverage)
- Integration tests for API endpoints
- Mock database for unit tests

---

### Dapper Design Recommended Implementation

**Repository Pattern**:
- Generic repository base class for common CRUD operations
- Entity-specific repositories extending base
- All repositories implement interfaces (IPatientRepository, IAppointmentRepository, etc)

**Query Organization**:
- SQL queries defined as private constants in repository methods
- Parameterized queries exclusively (no string concatenation)
- Comments explaining complex queries

**Transaction Management**:
- UnitOfWork class managing transaction scope
- Async transactions throughout (ExecuteAsync, QueryAsync)
- Proper rollback on exceptions

**Performance**:
- Indexes on foreign keys, search columns (phone, name), filter columns (IsActive)
- Pagination for large result sets (OFFSET/FETCH)
- Covering indexes for frequently queried fields
- Query execution time monitoring

**Data Mapping**:
- Dapper's QueryFirstAsync, QueryAsync for object mapping
- Custom type handlers for complex types (BloodPressure value object)
- DTOs for API responses (separate from domain entities)

---

### SQL Server Database Recommended Implementation

**Schema Design**:
- Normalized (3NF) schema to eliminate redundancy
- All tables with audit columns (CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
- Soft delete (IsActive flag) instead of hard delete
- Separate AuditLog table for immutable change tracking

**Indexes**:
- Clustered index on primary keys (PatientId, AppointmentId, VisitId, etc)
- Non-clustered indexes on foreign keys and frequently searched columns
- Covering indexes for performance-critical queries
- Regular index maintenance and statistics update

**Security**:
- Transparent Data Encryption (TDE) for at-rest encryption
- Restricted database access (VPN, IP whitelist)
- Strong password for database user
- Principle of least privilege (separate read/write users if possible)

**Backup & Recovery**:
- Daily full backups
- Hourly transaction log backups (if using Full recovery model)
- 30-day retention minimum
- Test recovery procedures monthly
- Automated backups to cloud (Azure Backup)

**Maintenance**:
- Weekly index defragmentation
- Monthly update statistics
- Quarterly consistency checks (DBCC CHECKDB)
- Query performance monitoring

---

### Authentication & Authorization Recommended Implementation

**JWT Implementation**:
- Asymmetric signing (RS256 with public/private keys)
- 1-hour access token expiry
- 30-day refresh token with rotation
- Claims-based authorization (roles, userId)

**Password Security**:
- BCrypt hashing (salt automatically included)
- Minimum 8 characters, complexity required
- Login attempt limiting (5 failures = 15-minute lockout)
- Password change logging

**Session Management**:
- Stateless (JWT-based, no server session store)
- 30-minute inactivity timeout
- Token refresh before expiry (client-side timer)
- Logout invalidates refresh token

**Authorization**:
- Role-based access control (Doctor role for Phase 1)
- Prepared for claims-based authorization (Phase 2)
- Verify patient ownership on every data access call
- Policy-based authorization for complex rules

---

### Security Measures Recommended

**Critical**:
1. ✅ Enforce HTTPS only (disable HTTP)
2. ✅ Parameterized queries (prevent SQL injection)
3. ✅ Password hashing with bcrypt (never store plain)
4. ✅ Input validation (prevent injection attacks)
5. ✅ Authentication with JWT tokens
6. ✅ Patient ownership verification (prevent horizontal access)
7. ✅ Database encryption (SQL Server TDE)
8. ✅ Audit logging (all data changes)

**Important**:
9. ⚠️ Rate limiting (prevent brute force and DoS)
10. ⚠️ Login attempt limiting (prevent brute force)
11. ⚠️ Error message sanitization (don't expose sensitive details)
12. ⚠️ CORS configuration (restrict API access)
13. ⚠️ Security headers (HSTS, CSP, X-Frame-Options)

**Recommended for Phase 1.5**:
14. 📋 Penetration testing / security audit
15. 📋 HIPAA compliance assessment
16. 📋 Two-factor authentication (TOTP)

---

### Missing Requirements to Implement

**Phase 1 Additions**:
- API rate limiting (100 req/min per user)
- Backup automation (daily 30-day retention)
- Health check endpoint (/health)
- Comprehensive error logging
- API documentation (Swagger/OpenAPI)
- Clinic configuration (name, address, phone for prescriptions)

**Phase 1.5 (Post-Launch)**:
- Performance optimization (caching, query tuning)
- Advanced search filters
- Prescription customization (clinic logo, footer)
- HIPAA compliance audit
- Penetration testing

**Phase 2 (Multi-User/Multi-Clinic)**:
- User role management (Admin, Receptionist, Doctor roles)
- Multi-clinic support (clinic isolation, multi-tenancy)
- Analytics dashboard (patient stats, appointment trends)
- Advanced reporting (patient summaries, clinic statistics)
- SSO integration (if EMR integration planned)

---

### Deployment Recommendation

**Infrastructure**:
- Cloud deployment (Azure recommended for .NET)
- Web App (App Service) for API
- SQL Database (managed SQL Server)
- Application Insights (monitoring)
- Azure Key Vault (secrets management)

**CI/CD Pipeline (Deferred)**:
- Not in scope while implementation is local. No GitHub Actions or Azure DevOps pipeline is created in Phase 1.
- Local quality gates instead: build, unit and integration tests, lint and formatting run from the command line before each commit or merge.
- Deployment to any shared environment is manual and documented in a runbook.
- Revisit before the first shared or production environment (see Missing Deployment Requirements, item 20).

**Pre-Launch Checklist**:
- ✅ Load testing (performance baseline)
- ✅ Security audit
- ✅ Doctor acceptance testing
- ✅ Backup/recovery testing
- ✅ Documentation complete
- ✅ Support team trained
- ✅ Incident response plan ready

---

### Final Readiness Assessment

**Project Status**: ✅ **READY FOR PLANNING PHASE**. All 22 open questions are resolved (see the Decisions Log under Open Questions). One item is carried forward as a pre-production blocker: the backup and recovery plan (Q18).

**Confidence Level**: HIGH ⭐⭐⭐⭐⭐

**Summary**:
- Business requirements clear and achievable
- Technical stack well-suited to requirements
- Architecture sound (Clean Architecture, Repository Pattern)
- Security measures comprehensive
- No blockers identified

**Recommendation**:
✅ **PROCEED TO PLANNING PHASE** with following approach:
1. Detailed API specification (OpenAPI/Swagger)
2. Database schema design document
3. UI mockups and user flow diagrams
4. Detailed project timeline (Phase 1 estimated 12-16 weeks)
5. Risk mitigation plan
6. Testing strategy document

**Next Steps**:
1. **PlanningAgent** → Create detailed implementation plan
2. **WorktreeAgent** → Setup git workflow and feature branches
3. **ImplementationAgent** → Begin coding with clear architecture
4. **VerificationAgent** → Comprehensive testing throughout
5. **GapAnalysisAgent** → Verify all requirements delivered
6. **CodeReviewAgent** → Quality gate before deployment
7. **FinishingAgent** → Deployment readiness verification

---

**Review Completed**: 2026-11-10  
**Document Version**: 1.0  
**Status**: Ready for Planning Phase  
**Next Phase**: Implementation Planning
