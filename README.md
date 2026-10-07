
# Bin It — Smart Waste Management System

Bin It is a smart waste management platform that connects citizens, municipal staff, drivers, and AI-assisted planning in one system.

The platform supports waste reporting, report verification, collection planning, fleet and route planning, dispatch approval, driver assignments, and operational issue handling.

## Deployment Links

- **Web Application:** `https://binit-web.onrender.com`
- **Backend API:** `https://binit-4pse.onrender.com`
- **Backend Health Check:** `https://binit-4pse.onrender.com/health`
- **AI Service:** `https://binit-ai.onrender.com`
- **Android APK:** `https://drive.google.com/drive/folders/106yqQzY5UXgQt0M_vITuKF5FWFRj74gP`
- **Demonstration Video:** `https://drive.google.com/drive/folders/106yqQzY5UXgQt0M_vITuKF5FWFRj74gP`


## Project Scope

The system includes:

- Citizen mobile application
- Driver mobile application
- Municipal staff web application
- ASP.NET Core backend API
- PostgreSQL database (Supabase)
- Supabase file storage
- AI planning service
- Cloud deployment

## Team Members

- `Dinujaya K.K.S.` — `IT24101619` — `Component 1: Waste Reporting & Citizen Management`
- `Ahamed M.M.S.` — `IT24100514` — `Component 2: Waste Collection & Bin Management`
- `Appuhamy P.P.D.A.S.K.` — `IT24100566` — `Component 3: Fleet, Driver & Route Management`
- `Perera H.M.N.S.` — `IT24100067` — `Component 4: Operations, Complaints & Analytics`

  ## Technology Stack

- **Backend:** ASP.NET Core 8, C#, Entity Framework Core
- **Database:** PostgreSQL / Supabase
- **Storage:** Supabase Storage
- **Web:** React, TypeScript, Vite
- **Mobile:** Flutter, Dart, Riverpod, GoRouter
- **AI:** Python, FastAPI, LangChain, LangGraph, Google Gemini
- **Hosting:** Render
- **Authentication:** ASP.NET Identity + JWT


# User Roles

## Citizen
## Driver
## Waste Officer 
## Municipal Manager


## Major Workflow


Citizen submits waste report
        ↓
Backend stores report
        ↓
AI Shared Planner
        ↓
C1 Waste Analysis
        ↓
Waste Officer verifies report
        ↓
C2 Collection Planning
        ↓
Human collection-plan approval
        ↓
Collection tasks created
        ↓
C3 Fleet & Route Planning
        ↓
C4 Validation & Operations
        ↓
Human dispatch approval
        ↓
Driver assignments created
        ↓
Driver executes collection work
```

## Important AI Workflow Details

The AI workflow follows a human-in-the-loop approach.

AI-generated plans are not automatically executed. Important decisions require human approval before operational data is created.

The ASP.NET backend remains authoritative, and clients never call the AI service directly.

```text
Flutter / React
      ↓
ASP.NET Core
      ↓
FastAPI AI Service
      ↓
Google Gemini
```

The AI workflow uses:

- structured outputs
- validation
- bounded retries
- backend-side safety validation
- human approval checkpoints



## Key Functionalities

- JWT authentication
- Role-based authorization
- Waste report submission and tracking
- Image/file attachments
- Report verification
- AI-assisted waste analysis
- AI-assisted collection planning
- Human approval workflows
- Fleet and route planning
- Driver assignment management
- Collection stop completion/failure handling
- Operational issue reporting
- Public waste-bin discovery
- Complaint management
- Background AI workflow processing


## Test Accounts

### Citizen
- Email: `citizen@smartwaste.local`
- Password: `DevPassword123!`

### Driver
- Email: `driver@smartwaste.local`
- Password: `DevPassword123!`

### Waste Officer  
- Email: `officer@smartwaste.local`
- Password: `DevPassword123!`

### Municipal Manager
- Email: `manager@smartwaste.local`
- Password: `DevPassword123!`



## Testing the Deployed System

The backend and AI service are hosted on Render free-tier services.

They may enter sleep mode after inactivity.

Before testing:

1. Open the backend health endpoint and AI service health endpoint

   `https://binit-4pse.onrender.com/health`
   `https://binit-ai.onrender.com/health`
   

3. Wait until it responds successfully.

4. Open the web application or mobile app.

5. If using AI features, allow additional time for the AI service to wake up on the first request.

Cold starts may make the first request noticeably slower. Later requests should be faster.


## Project Status

- Backend deployed
- Web application deployed
- AI service deployed
- PostgreSQL database deployed
- Supabase storage configured
- Flutter application completed
- Android APK built
- Automated tests implemented
```
